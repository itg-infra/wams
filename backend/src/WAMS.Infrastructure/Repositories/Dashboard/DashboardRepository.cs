namespace WAMS.Infrastructure.Repositories.Dashboard;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Dashboard;
using WAMS.Application.Interfaces.Dashboard;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class DashboardRepository(AppDbContext db) : IDashboardRepository
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        IReadOnlyList<long>? warehouseIds,
        IReadOnlyList<string> userRoleNames,
        CancellationToken ct = default
    )
    {
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];
        var warehouseFilterDisabled = warehouseIds is null;
        var roleNamesArray = userRoleNames.ToArray();

        // Single round-trip: CTEs cross-joined for one result row.
        // budget_kpi     - current-month approved plan value vs realized AP value
        // po_qualifying  - Generated POs whose budget plan items have no Generated AP yet
        // po_kpi         - po_qualifying count + count created in the last 7 days (WoW proxy)
        // wo_kpi         - Submitted WOs not soft-deleted + distinct warehouse count
        // pending_docs   - BudgetPlans(InApproval)+RecapWorkOrders(Pending)
        //                  where the current user's roles can approve the pending workflow stage,
        //                  tagged with a "pending since" timestamp
        // approval_kpi   - pending_docs count + count pending > 48h ("overdue")
        const string sql = """
            WITH
            budget_kpi AS (
                SELECT
                    COALESCE(SUM(bpi.total_value), 0)          AS planned_value,
                    COALESCE(SUM(api.budget_realization), 0)   AS actual_value
                FROM budget_plans bp
                JOIN budget_plan_items bpi ON bpi.budget_plan_id = bp."Id"
                LEFT JOIN account_payable_items api ON api.budget_plan_item_id = bpi."Id"
                LEFT JOIN account_payables ap
                    ON ap."Id" = api.account_payable_id
                    AND ap.sap_ap_number IS NOT NULL
                    AND ap.deleted_at IS NULL
                WHERE bp.deleted_at IS NULL
                  AND bp.status = 'Approved'
                  AND date_trunc('month', bp.doc_date AT TIME ZONE 'UTC') =
                      date_trunc('month', CURRENT_DATE::timestamptz AT TIME ZONE 'UTC')
                  AND (@p_wh_disabled OR bp.warehouse_shadow_id = ANY(@p_wh_ids))
            ),
            po_qualifying AS (
                SELECT po."Id", po.created_at
                FROM purchase_orders po
                JOIN purchase_order_items poi ON poi.purchase_order_id = po."Id"
                JOIN budget_plan_items bpi ON bpi."Id" = poi.budget_plan_item_id
                JOIN budget_plans bp ON bp."Id" = bpi.budget_plan_id
                WHERE po.status = 'Generated'
                  AND po.deleted_at IS NULL
                  AND (@p_wh_disabled OR bp.warehouse_shadow_id = ANY(@p_wh_ids))
                  AND NOT EXISTS (
                      SELECT 1
                      FROM purchase_order_items poi2
                      JOIN budget_plan_items bpi2 ON bpi2."Id" = poi2.budget_plan_item_id
                      JOIN account_payable_items api2 ON api2.budget_plan_item_id = bpi2."Id"
                      JOIN account_payables ap2
                          ON ap2."Id" = api2.account_payable_id
                          AND ap2.sap_ap_number IS NOT NULL
                          AND ap2.deleted_at IS NULL
                      WHERE poi2.purchase_order_id = po."Id"
                  )
                GROUP BY po."Id", po.created_at
            ),
            po_kpi AS (
                SELECT
                    COUNT(*)                                                              AS count,
                    COUNT(*) FILTER (WHERE created_at >= NOW() - INTERVAL '7 days')        AS new_last_7_days
                FROM po_qualifying
            ),
            wo_kpi AS (
                SELECT
                    COUNT(*)                                    AS count,
                    COUNT(DISTINCT wo.warehouse_shadow_id)       AS active_warehouse_count
                FROM work_orders wo
                WHERE wo.deleted_at IS NULL
                  AND wo.status = 'Submitted'
                  AND (@p_wh_disabled OR wo.warehouse_shadow_id = ANY(@p_wh_ids))
            ),
            pending_docs AS (
                -- BudgetPlans in InApproval where user's role can approve the current pending stage
                SELECT bp."Id", bp.submitted_at AS pending_since
                FROM budget_plans bp
                JOIN workflow_instances wi ON wi."Id" = bp.workflow_instance_id
                JOIN workflow_instance_stages wis
                    ON wis.workflow_instance_id = wi."Id"
                    AND wis.stage_order = wi.current_stage_order
                    AND wis.status = 'Pending'
                WHERE bp.status = 'InApproval'
                  AND bp.deleted_at IS NULL
                  AND (@p_wh_disabled OR bp.warehouse_shadow_id = ANY(@p_wh_ids))
                  AND EXISTS (
                      SELECT 1 FROM jsonb_array_elements_text(wis.approver_roles) r
                      WHERE r = ANY(@p_role_names)
                  )
                UNION ALL
                -- RecapWorkOrders in Pending (no stage-level role filter - any approver can act)
                SELECT rwo."Id", rwo.created_at AS pending_since
                FROM recap_work_orders rwo
                JOIN budget_plans bp ON bp."Id" = rwo.budget_plan_id
                WHERE rwo.status = 'Pending'
                  AND (@p_wh_disabled OR bp.warehouse_shadow_id = ANY(@p_wh_ids))
            ),
            approval_kpi AS (
                SELECT
                    COUNT(*)                                                                   AS count,
                    COUNT(*) FILTER (WHERE NOW() - pending_since > INTERVAL '48 hours')         AS overdue_count
                FROM pending_docs
            )
            SELECT
                b.planned_value,
                b.actual_value,
                p.count            AS po_count,
                p.new_last_7_days  AS po_new_last_7_days,
                w.count            AS wo_count,
                w.active_warehouse_count,
                a.count            AS approval_count,
                a.overdue_count    AS approval_overdue_count
            FROM budget_kpi b, po_kpi p, wo_kpi w, approval_kpi a;
            """;

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });
        cmd.Parameters.Add(new NpgsqlParameter("p_role_names", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = roleNamesArray });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);

        var planned = reader.GetDecimal(reader.GetOrdinal("planned_value"));
        var actual = reader.GetDecimal(reader.GetOrdinal("actual_value"));
        var percent = planned == 0 ? 0m : Math.Round(actual / planned * 100, 2);

        return new DashboardSummaryResponse(
            percent,
            planned,
            actual,
            reader.GetInt32(reader.GetOrdinal("po_count")),
            reader.GetInt32(reader.GetOrdinal("po_new_last_7_days")),
            reader.GetInt32(reader.GetOrdinal("wo_count")),
            reader.GetInt32(reader.GetOrdinal("active_warehouse_count")),
            reader.GetInt32(reader.GetOrdinal("approval_count")),
            reader.GetInt32(reader.GetOrdinal("approval_overdue_count")));
    }

    public async Task<(List<DashboardActivityResponse> Items, int TotalCount)> GetTodayActivitiesAsync(
        DashboardActivityQuery query,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default
    )
    {
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];
        var warehouseFilterDisabled = warehouseIds is null;
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);
        var offset = (query.Page - 1) * query.Limit;

        // Filters to BudgetPlans created today (UTC date). Vendor names are aggregated across
        // all of a plan's items (a plan can span multiple vendors); AnyRfba is true if any item
        // on the plan is flagged RFBA. Pagination (OFFSET/LIMIT) happens in the "paged" CTE so the
        // per-plan vendor/RFBA subqueries below only run for the page being returned, not for every
        // plan created today.
        const string sql = """
            WITH paged AS (
                SELECT
                    bp."Id"          AS budget_plan_id,
                    bp.code          AS budget_no,
                    bp.remark,
                    ws.location,
                    bp.created_at    AS date,
                    bp.status,
                    COUNT(*) OVER()  AS total_count
                FROM budget_plans bp
                JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
                WHERE bp.deleted_at IS NULL
                  AND bp.created_at >= date_trunc('day', NOW() AT TIME ZONE 'UTC')::timestamptz
                  AND bp.created_at <  (date_trunc('day', NOW() AT TIME ZONE 'UTC') + interval '1 day')::timestamptz
                  AND (@p_wh_disabled OR bp.warehouse_shadow_id = ANY(@p_wh_ids))
                  AND (@p_search IS NULL
                       OR bp.code   ILIKE @p_search_pattern
                       OR ws.code   ILIKE @p_search_pattern)
                ORDER BY bp.created_at DESC
                OFFSET @p_offset LIMIT @p_limit
            )
            SELECT
                paged.budget_plan_id,
                paged.budget_no,
                (
                    SELECT STRING_AGG(DISTINCT v.card_name, ', ' ORDER BY v.card_name)
                    FROM budget_plan_items bpi
                    JOIN vendor_shadows v ON v."Id" = bpi.vendor_shadow_id
                    WHERE bpi.budget_plan_id = paged.budget_plan_id
                ) AS vendor_name,
                paged.remark,
                COALESCE((
                    SELECT BOOL_OR(bpi.is_rfba)
                    FROM budget_plan_items bpi
                    WHERE bpi.budget_plan_id = paged.budget_plan_id
                ), FALSE) AS any_rfba,
                paged.location,
                paged.date,
                paged.status,
                paged.total_count
            FROM paged
            ORDER BY paged.date DESC;
            """;

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = offset });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = query.Limit });

        var items = new List<DashboardActivityResponse>();
        var total = 0;

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("budget_plan_id");
        var colBudgetNo = reader.GetOrdinal("budget_no");
        var colVendorName = reader.GetOrdinal("vendor_name");
        var colRemark = reader.GetOrdinal("remark");
        var colAnyRfba = reader.GetOrdinal("any_rfba");
        var colLocation = reader.GetOrdinal("location");
        var colDate = reader.GetOrdinal("date");
        var colStatus = reader.GetOrdinal("status");
        var colTotal = reader.GetOrdinal("total_count");

        while (await reader.ReadAsync(ct))
        {
            total = reader.GetInt32(colTotal);
            var statusStr = reader.GetString(colStatus);
            var statusDisplay = BudgetPlanStatus.TryFromValue(statusStr, out var bps) ? bps.DisplayName : statusStr;

            items.Add(new DashboardActivityResponse(
                reader.GetInt64(colId),
                reader.GetString(colBudgetNo),
                reader.IsDBNull(colVendorName) ? null : reader.GetString(colVendorName),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.GetBoolean(colAnyRfba),
                reader.IsDBNull(colLocation) ? null : reader.GetString(colLocation),
                reader.GetDateTime(colDate),
                statusStr,
                statusDisplay));
        }

        return (items, total);
    }

    public async Task<DashboardHistoryResponse> GetHistoryAsync(
        int year,
        int month,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default
    )
    {
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];
        var warehouseFilterDisabled = warehouseIds is null;

        // UNION of:
        //   • WorkOrder submissions (submitted_at, EventType = 'Submitted')
        //   • WorkflowInstanceStage approvals/rejections linked to BudgetPlan's workflow instance
        //     (EventType = 'Approved' | 'Rejected')
        // One query returns both calendar aggregates and the raw 20-event feed.
        const string eventsSql = """
            WITH events AS (
                SELECT
                    wo.submitted_at            AS event_ts,
                    'Submitted'                AS event_type,
                    item.item_name             AS activity_type_name,
                    ws.code                    AS warehouse_code
                FROM work_orders wo
                JOIN warehouse_shadows ws  ON ws."Id" = wo.warehouse_shadow_id
                JOIN item_shadows item     ON item."Id" = wo.item_shadow_id
                WHERE wo.submitted_at IS NOT NULL
                  AND wo.deleted_at IS NULL
                  AND EXTRACT(YEAR  FROM wo.submitted_at AT TIME ZONE 'UTC') = @p_year
                  AND EXTRACT(MONTH FROM wo.submitted_at AT TIME ZONE 'UTC') = @p_month
                  AND (@p_wh_disabled OR wo.warehouse_shadow_id = ANY(@p_wh_ids))

                UNION ALL

                SELECT
                    CASE WHEN wis.status = 'Approved'
                         THEN wis.approved_at
                         ELSE wis.rejected_at
                    END                        AS event_ts,
                    wis.status                 AS event_type,
                    item.item_name             AS activity_type_name,
                    ws.code                    AS warehouse_code
                FROM workflow_instance_stages wis
                JOIN workflow_instances wi    ON wi."Id" = wis.workflow_instance_id
                JOIN budget_plans bp          ON bp.workflow_instance_id = wi."Id"
                JOIN work_orders wo           ON wo.budget_plan_id = bp."Id"
                JOIN warehouse_shadows ws     ON ws."Id" = wo.warehouse_shadow_id
                JOIN item_shadows item        ON item."Id" = wo.item_shadow_id
                WHERE wis.status IN ('Approved', 'Rejected')
                  AND (
                      (wis.status = 'Approved' AND
                       EXTRACT(YEAR  FROM wis.approved_at AT TIME ZONE 'UTC') = @p_year AND
                       EXTRACT(MONTH FROM wis.approved_at AT TIME ZONE 'UTC') = @p_month)
                      OR
                      (wis.status = 'Rejected' AND
                       EXTRACT(YEAR  FROM wis.rejected_at AT TIME ZONE 'UTC') = @p_year AND
                       EXTRACT(MONTH FROM wis.rejected_at AT TIME ZONE 'UTC') = @p_month)
                  )
                  AND bp.deleted_at IS NULL
                  AND wo.deleted_at IS NULL
                  AND (@p_wh_disabled OR bp.warehouse_shadow_id = ANY(@p_wh_ids))
            )
            SELECT
                event_ts,
                event_type,
                activity_type_name,
                warehouse_code,
                (event_ts AT TIME ZONE 'UTC')::date AS event_date
            FROM events
            WHERE event_ts IS NOT NULL
            ORDER BY event_ts DESC;
            """;

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(eventsSql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("p_year", NpgsqlDbType.Integer) { Value = year });
        cmd.Parameters.Add(new NpgsqlParameter("p_month", NpgsqlDbType.Integer) { Value = month });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });

        var calendarMap = new Dictionary<DateOnly, int>();
        var recentEvents = new List<DashboardEventEntry>();

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colTs = reader.GetOrdinal("event_ts");
        var colType = reader.GetOrdinal("event_type");
        var colActivity = reader.GetOrdinal("activity_type_name");
        var colWh = reader.GetOrdinal("warehouse_code");
        var colDate = reader.GetOrdinal("event_date");

        while (await reader.ReadAsync(ct))
        {
            var ts = reader.GetDateTime(colTs);
            var type = reader.GetString(colType);
            var activity = reader.GetString(colActivity);
            var wh = reader.GetString(colWh);
            var date = reader.GetFieldValue<DateOnly>(colDate);

            calendarMap[date] = calendarMap.TryGetValue(date, out var c) ? c + 1 : 1;

            if (recentEvents.Count < 20)
                recentEvents.Add(new DashboardEventEntry(ts, type, activity, wh));
        }

        var calendarDays = calendarMap
            .Select(kv => new DashboardCalendarDay(kv.Key, kv.Value))
            .OrderBy(d => d.Date)
            .ToList();

        return new DashboardHistoryResponse(calendarDays, recentEvents);
    }
}
