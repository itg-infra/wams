namespace WAMS.Infrastructure.Repositories.RecapWorkOrders;

using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WAMS.Application.Common;
using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Domain.Entities.RecapWorkOrders;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class RecapWorkOrderRepository(
    AppDbContext db,
    ITenantContext tenantContext) : IRecapWorkOrderRepository
{
    private const string DefaultOrderBy = "r.created_at DESC NULLS LAST, r.\"Id\" DESC";
    private static readonly Dictionary<string, string> SortColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = "r.status",
            ["docdate"] = "bp.doc_date",
            ["createdat"] = "r.created_at",
        };

    // Uses ExecuteSqlInterpolatedAsync so it shares the EF connection and participates
    // in the same SaveChanges transaction. ON CONFLICT eliminates the TOCTOU race when
    // two WOs under the same BP are submitted concurrently.
    public async Task UpsertForBudgetPlanAsync(
        long budgetPlanId,
        long companyId,
        CancellationToken ct = default
    )
        => await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO recap_work_orders (budget_plan_id, company_id, status, created_at, updated_at)
            VALUES ({budgetPlanId}, {companyId}, 'Pending', NOW(), NOW())
            ON CONFLICT (budget_plan_id) DO NOTHING
            """, ct);

    public async Task<(List<RecapWorkOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(
        RecapWorkOrderQuery q,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default
    )
    {
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];
        var warehouseFilterDisabled = warehouseIds is null;

        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;

        var orderBy = SortColumns.TryGetValue(q.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(q.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : DefaultOrderBy;

        var search = string.IsNullOrWhiteSpace(q.Search) ? null : q.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);
        var offset = (q.Page - 1) * q.Limit;

        var countSql = @"
            SELECT COUNT(*)
            FROM recap_work_orders r
            JOIN budget_plans bp ON bp.""Id"" = r.budget_plan_id
            WHERE (@p_tenant_filter_disabled OR r.company_id = @p_company_id)
              AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
              AND (@p_status IS NULL OR r.status = @p_status)
              AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern);";

        var sql = $@"
            WITH wo_agg AS (
                SELECT
                    wo.budget_plan_id,
                    STRING_AGG(DISTINCT wo.activity_type_code, ', ' ORDER BY wo.activity_type_code) FILTER (WHERE wo.activity_type_code IS NOT NULL) AS activity_types,
                    STRING_AGG(DISTINCT u.""Fullname"", ', ' ORDER BY u.""Fullname"") FILTER (WHERE u.""Fullname"" IS NOT NULL) AS pic_names
                FROM work_orders wo
                LEFT JOIN users u ON u.""Id"" = wo.pic_user_id
                WHERE wo.deleted_at IS NULL
                GROUP BY wo.budget_plan_id
            ),
            spk_agg AS (
                SELECT
                    bpsi.budget_plan_id,
                    STRING_AGG(DISTINCT spk.bl_no, ', ' ORDER BY spk.bl_no) FILTER (WHERE spk.bl_no IS NOT NULL) AS bl_numbers
                FROM budget_plan_spk_items bpsi
                JOIN spk_shadows spk ON spk.""Id"" = bpsi.spk_shadow_id
                GROUP BY bpsi.budget_plan_id
            ),
            rfba_bps AS (
                SELECT DISTINCT bpi.budget_plan_id
                FROM budget_plan_items bpi
                WHERE bpi.is_rfba = TRUE
            )
            SELECT
                r.""Id"",
                bp.""Id"" AS budget_plan_id,
                bp.code AS budget_plan_code,
                bt.code AS template_code,
                bp.remark,
                ws.code AS warehouse_code,
                ws.name AS warehouse_name,
                spk_agg.bl_numbers,
                wo_agg.activity_types,
                wo_agg.pic_names,
                (rfba_bps.budget_plan_id IS NOT NULL) AS is_rfba,
                bp.doc_date,
                r.status,
                r.created_at
            FROM recap_work_orders r
            JOIN budget_plans bp ON bp.""Id"" = r.budget_plan_id
            JOIN budget_templates bt ON bt.""Id"" = bp.budget_template_id
            JOIN warehouse_shadows ws ON ws.""Id"" = bp.warehouse_shadow_id
            LEFT JOIN spk_agg ON spk_agg.budget_plan_id = bp.""Id""
            LEFT JOIN wo_agg ON wo_agg.budget_plan_id = bp.""Id""
            LEFT JOIN rfba_bps ON rfba_bps.budget_plan_id = bp.""Id""
            WHERE (@p_tenant_filter_disabled OR r.company_id = @p_company_id)
              AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
              AND (@p_status IS NULL OR r.status = @p_status)
              AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern)
            ORDER BY {orderBy}
            OFFSET @p_offset LIMIT @p_limit;";

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = countSql;
        countCmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        countCmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        countCmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        countCmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });
        countCmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)q.Status ?? DBNull.Value });
        countCmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        countCmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct) ?? 0);

        if (offset >= total) return ([], total);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });
        cmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)q.Status ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = offset });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = q.Limit });

        var items = new List<RecapWorkOrderSummaryResponse>();

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colBpId = reader.GetOrdinal("budget_plan_id");
        var colBpCode = reader.GetOrdinal("budget_plan_code");
        var colTemplateCode = reader.GetOrdinal("template_code");
        var colRemark = reader.GetOrdinal("remark");
        var colWarehouseCode = reader.GetOrdinal("warehouse_code");
        var colWarehouseName = reader.GetOrdinal("warehouse_name");
        var colBlNumbers = reader.GetOrdinal("bl_numbers");
        var colActivityTypes = reader.GetOrdinal("activity_types");
        var colPicNames = reader.GetOrdinal("pic_names");
        var colIsRfba = reader.GetOrdinal("is_rfba");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colStatus = reader.GetOrdinal("status");
        var colCreatedAt = reader.GetOrdinal("created_at");

        while (await reader.ReadAsync(ct))
        {
            items.Add(new RecapWorkOrderSummaryResponse(
                reader.GetInt64(colId),
                reader.GetInt64(colBpId),
                reader.GetString(colBpCode),
                reader.GetString(colTemplateCode),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.GetString(colWarehouseCode),
                reader.GetString(colWarehouseName),
                reader.IsDBNull(colBlNumbers) ? null : reader.GetString(colBlNumbers),
                reader.IsDBNull(colActivityTypes) ? null : reader.GetString(colActivityTypes),
                reader.IsDBNull(colPicNames) ? null : reader.GetString(colPicNames),
                reader.GetBoolean(colIsRfba),
                reader.GetDateTime(colDocDate),
                reader.GetString(colStatus),
                reader.GetDateTime(colCreatedAt)));
        }

        return (items, total);
    }

    public async IAsyncEnumerable<RecapWorkOrderSummaryResponse> StreamAllAsync(
        RecapWorkOrderQuery q,
        IReadOnlyList<long>? warehouseIds,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];
        var warehouseFilterDisabled = warehouseIds is null;

        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;

        var orderBy = SortColumns.TryGetValue(q.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(q.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : DefaultOrderBy;

        var search = string.IsNullOrWhiteSpace(q.Search) ? null : q.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);

        var streamSql = $@"
            WITH wo_agg AS (
                SELECT
                    wo.budget_plan_id,
                    STRING_AGG(DISTINCT wo.activity_type_code, ', ' ORDER BY wo.activity_type_code) FILTER (WHERE wo.activity_type_code IS NOT NULL) AS activity_types,
                    STRING_AGG(DISTINCT u.""Fullname"", ', ' ORDER BY u.""Fullname"") FILTER (WHERE u.""Fullname"" IS NOT NULL) AS pic_names
                FROM work_orders wo
                LEFT JOIN users u ON u.""Id"" = wo.pic_user_id
                WHERE wo.deleted_at IS NULL
                GROUP BY wo.budget_plan_id
            ),
            spk_agg AS (
                SELECT
                    bpsi.budget_plan_id,
                    STRING_AGG(DISTINCT spk.bl_no, ', ' ORDER BY spk.bl_no) FILTER (WHERE spk.bl_no IS NOT NULL) AS bl_numbers
                FROM budget_plan_spk_items bpsi
                JOIN spk_shadows spk ON spk.""Id"" = bpsi.spk_shadow_id
                GROUP BY bpsi.budget_plan_id
            ),
            rfba_bps AS (
                SELECT DISTINCT bpi.budget_plan_id
                FROM budget_plan_items bpi
                WHERE bpi.is_rfba = TRUE
            )
            SELECT
                r.""Id"",
                bp.""Id"" AS budget_plan_id,
                bp.code AS budget_plan_code,
                bt.code AS template_code,
                bp.remark,
                ws.code AS warehouse_code,
                ws.name AS warehouse_name,
                spk_agg.bl_numbers,
                wo_agg.activity_types,
                wo_agg.pic_names,
                (rfba_bps.budget_plan_id IS NOT NULL) AS is_rfba,
                bp.doc_date,
                r.status,
                r.created_at
            FROM recap_work_orders r
            JOIN budget_plans bp ON bp.""Id"" = r.budget_plan_id
            JOIN budget_templates bt ON bt.""Id"" = bp.budget_template_id
            JOIN warehouse_shadows ws ON ws.""Id"" = bp.warehouse_shadow_id
            LEFT JOIN spk_agg ON spk_agg.budget_plan_id = bp.""Id""
            LEFT JOIN wo_agg ON wo_agg.budget_plan_id = bp.""Id""
            LEFT JOIN rfba_bps ON rfba_bps.budget_plan_id = bp.""Id""
            WHERE (@p_tenant_filter_disabled OR r.company_id = @p_company_id)
              AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
              AND (@p_status IS NULL OR r.status = @p_status)
              AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern)
            ORDER BY {orderBy}
            LIMIT @p_limit;";

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand { Connection = conn, CommandText = streamSql };
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });
        cmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)q.Status ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = limit });

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colBpId = reader.GetOrdinal("budget_plan_id");
        var colBpCode = reader.GetOrdinal("budget_plan_code");
        var colTemplateCode = reader.GetOrdinal("template_code");
        var colRemark = reader.GetOrdinal("remark");
        var colWarehouseCode = reader.GetOrdinal("warehouse_code");
        var colWarehouseName = reader.GetOrdinal("warehouse_name");
        var colBlNumbers = reader.GetOrdinal("bl_numbers");
        var colActivityTypes = reader.GetOrdinal("activity_types");
        var colPicNames = reader.GetOrdinal("pic_names");
        var colIsRfba = reader.GetOrdinal("is_rfba");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colStatus = reader.GetOrdinal("status");
        var colCreatedAt = reader.GetOrdinal("created_at");

        while (await reader.ReadAsync(ct))
        {
            yield return new RecapWorkOrderSummaryResponse(
                reader.GetInt64(colId),
                reader.GetInt64(colBpId),
                reader.GetString(colBpCode),
                reader.GetString(colTemplateCode),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.GetString(colWarehouseCode),
                reader.GetString(colWarehouseName),
                reader.IsDBNull(colBlNumbers) ? null : reader.GetString(colBlNumbers),
                reader.IsDBNull(colActivityTypes) ? null : reader.GetString(colActivityTypes),
                reader.IsDBNull(colPicNames) ? null : reader.GetString(colPicNames),
                reader.GetBoolean(colIsRfba),
                reader.GetDateTime(colDocDate),
                reader.GetString(colStatus),
                reader.GetDateTime(colCreatedAt));
        }
    }

    public async Task<RecapDetailProjection?> GetDetailProjectionAsync(
        long id,
        string? reviewerNameOverride,
        CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        // Query 1: header (single row) + warehouse_shadow_id for access check 
        const string headerSql = """
            SELECT  r."Id"                          AS recap_id,
                    r.budget_plan_id                AS bp_id,
                    r.company_id                    AS company_id,
                    bp.warehouse_shadow_id          AS warehouse_shadow_id,
                    r.status                        AS recap_status,
                    COALESCE(@p_reviewer_override, rb."Fullname") AS reviewer_name,
                    r.reviewed_at                   AS reviewed_at,
                    r.rejection_reason              AS rejection_reason,
                    bp.code                         AS bp_code,
                    bt.code                         AS template_code,
                    bp.status                       AS bp_status,
                    bp.remark                       AS remark,
                    bp.doc_date                     AS doc_date,
                    ws.code                         AS wh_code,
                    ws.name                         AS wh_name,
                    ws.location                     AS wh_location
            FROM recap_work_orders r
            JOIN budget_plans bp        ON bp."Id" = r.budget_plan_id AND bp.deleted_at IS NULL
            JOIN budget_templates bt    ON bt."Id" = bp.budget_template_id AND bt.deleted_at IS NULL
            JOIN warehouse_shadows ws   ON ws."Id" = bp.warehouse_shadow_id
            LEFT JOIN users rb          ON rb."Id" = r.reviewed_by_user_id AND rb.deleted_at IS NULL
            WHERE r."Id" = @p_id
              AND (@p_tenant_disabled OR r.company_id = @p_company_id)
            """;

        await using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = headerSql;
        cmd1.Parameters.Add(new NpgsqlParameter("p_id", NpgsqlDbType.Bigint) { Value = id });
        cmd1.Parameters.Add(new NpgsqlParameter("p_tenant_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd1.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd1.Parameters.Add(new NpgsqlParameter("p_reviewer_override", NpgsqlDbType.Text) { Value = (object?)reviewerNameOverride ?? DBNull.Value });

        long bpId, companyId, warehouseShadowId;
        string recapStatus, bpStatus;
        string? reviewerName, rejectionReason, remark;
        DateTime? reviewedAt;
        RecapDetailHeader header;

        await using (var reader = await cmd1.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct))
                return null;

            bpId = reader.GetInt64(reader.GetOrdinal("bp_id"));
            companyId = reader.GetInt64(reader.GetOrdinal("company_id"));
            warehouseShadowId = reader.GetInt64(reader.GetOrdinal("warehouse_shadow_id"));
            recapStatus = reader.GetString(reader.GetOrdinal("recap_status"));
            reviewerName = ReadStringOrNull(reader, "reviewer_name");
            reviewedAt = ReadDateTimeOrNull(reader, "reviewed_at");
            rejectionReason = ReadStringOrNull(reader, "rejection_reason");
            bpStatus = reader.GetString(reader.GetOrdinal("bp_status"));
            remark = ReadStringOrNull(reader, "remark");

            header = new RecapDetailHeader(
                BpCode: reader.GetString(reader.GetOrdinal("bp_code")),
                TemplateCode: reader.GetString(reader.GetOrdinal("template_code")),
                BpStatus: bpStatus,
                Remark: remark,
                DocDate: reader.GetDateTime(reader.GetOrdinal("doc_date")),
                WarehouseCode: reader.GetString(reader.GetOrdinal("wh_code")),
                WarehouseName: reader.GetString(reader.GetOrdinal("wh_name")),
                WarehouseLocation: ReadStringOrNull(reader, "wh_location"));
        }

        // Query 2: SPK docs + cost rows for the BP, in one round-trip via two cursors? 
        // Postgres has no multi-resultset over Npgsql. Issue them sequentially.
        var spkRows = new List<RecapDetailSpkRow>();
        var costRows = new List<RecapDetailCostRow>();

        const string spkSql = """
            SELECT  s.type         AS type,
                    s.doc_no       AS doc_no,
                    s.base_doc_no  AS base_doc_no,
                    s.bl_no        AS bl_no,
                    s.item_code    AS item_code,
                    s.item_name    AS item_name,
                    s.quantity     AS quantity,
                    s.delivery_qty AS delivery_qty,
                    s.uom          AS uom,
                    bsi.sort_order AS sort_order
            FROM budget_plan_spk_items bsi
            JOIN spk_shadows s ON s."Id" = bsi.spk_shadow_id
            WHERE bsi.budget_plan_id = @p_bp_id
            ORDER BY bsi.sort_order
            """;

        await using (var cmd2 = conn.CreateCommand())
        {
            cmd2.CommandText = spkSql;
            cmd2.Parameters.Add(new NpgsqlParameter("p_bp_id", NpgsqlDbType.Bigint) { Value = bpId });
            await using var reader = await cmd2.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                spkRows.Add(new RecapDetailSpkRow(
                    Type: reader.GetString(0),
                    DocNo: reader.GetString(1),
                    BaseDocNo: reader.GetString(2),
                    BlNo: reader.IsDBNull(3) ? null : reader.GetString(3),
                    ItemCode: reader.GetString(4),
                    ItemName: reader.GetString(5),
                    Quantity: reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    DeliveryQty: reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    UoM: reader.GetString(8),
                    SortOrder: reader.GetInt32(9)));
            }
        }

        const string costSql = """
            SELECT  bpi."Id"               AS id,
                    bpi.type               AS type,
                    v.card_code            AS vendor_code,
                    v.card_name            AS vendor_name,
                    bpi.is_rfba            AS is_rfba,
                    bpi.doc_external       AS doc_external,
                    i.item_name            AS item_name,
                    i.acct_code            AS acct_code,
                    i.acct_name            AS acct_name,
                    bpi.bill_of_lading     AS bill_of_lading,
                    bpi.cost_value         AS cost_value,
                    bpi.quantity           AS quantity,
                    u.code                 AS uom_code,
                    bpi.description        AS description,
                    bpi.total_value        AS total_value,
                    bpi.item_shadow_id     AS item_shadow_id,
                    bpi.sort_order         AS sort_order
            FROM budget_plan_items bpi
            JOIN vendor_shadows v ON v."Id" = bpi.vendor_shadow_id
            JOIN item_shadows  i  ON i."Id" = bpi.item_shadow_id
            JOIN uom_masters   u  ON u."Id" = bpi.uom_master_id AND u.deleted_at IS NULL
            WHERE bpi.budget_plan_id = @p_bp_id
            ORDER BY bpi.sort_order
            """;

        await using (var cmd3 = conn.CreateCommand())
        {
            cmd3.CommandText = costSql;
            cmd3.Parameters.Add(new NpgsqlParameter("p_bp_id", NpgsqlDbType.Bigint) { Value = bpId });
            await using var reader = await cmd3.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                costRows.Add(new RecapDetailCostRow(
                    Id: reader.GetInt64(0),
                    Type: reader.GetString(1),
                    VendorCode: reader.GetString(2),
                    VendorName: reader.GetString(3),
                    IsRfba: reader.GetBoolean(4),
                    DocExternal: reader.IsDBNull(5) ? null : reader.GetString(5),
                    ItemName: reader.GetString(6),
                    AcctCode: reader.GetString(7),
                    AcctName: reader.GetString(8),
                    BillOfLading: reader.IsDBNull(9) ? null : reader.GetString(9),
                    CostValue: reader.GetDecimal(10),
                    Quantity: reader.GetDecimal(11),
                    UomCode: reader.GetString(12),
                    Description: reader.IsDBNull(13) ? null : reader.GetString(13),
                    TotalValue: reader.GetDecimal(14),
                    ItemShadowId: reader.GetInt64(15),
                    SortOrder: reader.GetInt32(16)));
            }
        }

        // Query 3: work orders + pre-aggregated detail values needed for ComputeActualCost 
        // The aggregates and LEFT JOINs collapse the four 1:1 detail tables and the two 1:N item
        // tables into scalar fields. The cost-computation switch reads them directly - no extra
        // round trips, no in-memory grouping.
        var woRows = new List<RecapDetailWoRow>();

        const string woSql = """
            SELECT  w."Id"                                                              AS id,
                    w.code                                                              AS code,
                    (SELECT spk.bl_no
                       FROM budget_plan_spk_items bsi2
                       JOIN spk_shadows spk ON spk."Id" = bsi2.spk_shadow_id
                       WHERE bsi2.budget_plan_id = @p_bp_id
                       ORDER BY bsi2.sort_order
                       LIMIT 1)                                                         AS bl_number,
                    pic."Fullname"                                                      AS pic_name,
                    w.is_rfba                                                           AS is_rfba,
                    w.start_date                                                        AS start_date,
                    w.end_date                                                          AS end_date,
                    w.status                                                            AS status,
                    act.item_name                                                       AS activity_name,
                    (SELECT tos.vehicle_no
                       FROM work_order_transport_orders wto
                       JOIN transport_order_shadows tos ON tos.id = wto.transport_order_shadow_id
                       WHERE wto.work_order_id = w."Id"
                       ORDER BY wto.transport_order_shadow_id
                       LIMIT 1)                                                         AS vehicle_no,
                    w.activity_type_code                                                AS activity_type_code,
                    w.item_shadow_id                                                    AS item_shadow_id,
                    COALESCE((SELECT SUM(nett_weight) FROM work_order_unloading_items WHERE work_order_id = w."Id"), 0) AS unloading_nett,
                    COALESCE((SELECT SUM(nett_weight) FROM work_order_loading_items   WHERE work_order_id = w."Id"), 0) AS loading_nett,
                    sd.volume_weight                                                    AS storage_volume,
                    he.total_cost                                                       AS heavy_total_cost,
                    ub.total_weight                                                     AS unbagging_total,
                    rb2.total_weight                                                    AS rebagging_total,
                    w.created_at                                                        AS created_at
            FROM work_orders w
            JOIN users pic        ON pic."Id" = w.pic_user_id AND pic.deleted_at IS NULL
            JOIN item_shadows act ON act."Id" = w.item_shadow_id
            LEFT JOIN work_order_storage_details      sd  ON sd.work_order_id  = w."Id"
            LEFT JOIN work_order_heavy_equip_details  he  ON he.work_order_id  = w."Id"
            LEFT JOIN work_order_unbagging_details    ub  ON ub.work_order_id  = w."Id"
            LEFT JOIN work_order_rebagging_details    rb2 ON rb2.work_order_id = w."Id"
            WHERE w.budget_plan_id = @p_bp_id AND w.deleted_at IS NULL
            ORDER BY w.created_at
            """;

        await using (var cmd4 = conn.CreateCommand())
        {
            cmd4.CommandText = woSql;
            cmd4.Parameters.Add(new NpgsqlParameter("p_bp_id", NpgsqlDbType.Bigint) { Value = bpId });
            await using var reader = await cmd4.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                woRows.Add(new RecapDetailWoRow(
                    Id: reader.GetInt64(0),
                    Code: reader.GetString(1),
                    BlNumber: reader.IsDBNull(2) ? null : reader.GetString(2),
                    PicName: reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsRfba: reader.GetBoolean(4),
                    StartDate: reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    EndDate: reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    Status: reader.GetString(7),
                    ActivityName: reader.IsDBNull(8) ? null : reader.GetString(8),
                    VehicleNo: reader.IsDBNull(9) ? null : reader.GetString(9),
                    ActivityTypeCode: reader.GetString(10),
                    ItemShadowId: reader.GetInt64(11),
                    UnloadingNettSum: reader.GetDecimal(12),
                    LoadingNettSum: reader.GetDecimal(13),
                    StorageVolumeWeight: reader.IsDBNull(14) ? null : reader.GetDecimal(14),
                    HeavyEquipTotalCost: reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                    UnbaggingTotalWeight: reader.IsDBNull(16) ? null : reader.GetDecimal(16),
                    RebaggingTotalWeight: reader.IsDBNull(17) ? null : reader.GetDecimal(17),
                    CreatedAt: reader.GetDateTime(18)));
            }
        }

        return new RecapDetailProjection(
            Id: id,
            BudgetPlanId: bpId,
            CompanyId: companyId,
            WarehouseShadowId: warehouseShadowId,
            RecapStatus: recapStatus,
            ReviewerName: reviewerName,
            ReviewedAt: reviewedAt,
            RejectionReason: rejectionReason,
            Header: header,
            SpkRows: spkRows,
            CostRows: costRows,
            WoRows: woRows);
    }

    private static string? ReadStringOrNull(System.Data.Common.DbDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetString(i);
    }

    private static DateTime? ReadDateTimeOrNull(System.Data.Common.DbDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetDateTime(i);
    }

    public async Task<RecapWorkOrder?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default)
        => await db.RecapWorkOrders
            .Include(r => r.ReviewedBy)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.BudgetTemplate)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.Warehouse)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.Items)
                    .ThenInclude(i => i.Vendor)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.Items)
                    .ThenInclude(i => i.Item)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.Items)
                    .ThenInclude(i => i.Uom)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.SpkItems)
                    .ThenInclude(s => s.Spk)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.PicUser)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.Activity)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.TransportOrders)
                        .ThenInclude(t => t.TransportOrderShadow)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.UnloadingItems)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.LoadingItems)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.StorageDetail)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.HeavyEquipDetail)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.UnbaggingDetail)
            .Include(r => r.BudgetPlan)
                .ThenInclude(b => b.WorkOrders.Where(w => w.DeletedAt == null))
                    .ThenInclude(w => w.RebaggingDetail)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<bool> IsApprovedByBudgetPlanIdAsync(long budgetPlanId, CancellationToken ct = default)
        => await db.RecapWorkOrders
            .AnyAsync(r => r.BudgetPlanId == budgetPlanId && r.Status == RecapWorkOrderStatus.Approved, ct);

    // Targets only the 5 columns that change on review - raw SQL to avoid SmartEnum converter bypass in ExecuteUpdateAsync.
    public async Task ReviewAsync(
        long id,
        string status,
        long reviewedByUserId,
        DateTime reviewedAt,
        string? rejectionReason,
        CancellationToken ct = default
    )
        => await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE recap_work_orders
            SET status               = {status},
                reviewed_by_user_id  = {reviewedByUserId},
                reviewed_at          = {reviewedAt},
                rejection_reason     = {rejectionReason},
                updated_at           = NOW()
            WHERE "Id" = {id}
            """, ct);

    public async Task ResetToPendingByBudgetPlanIdAsync(long budgetPlanId, CancellationToken ct = default)
        => await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE recap_work_orders
            SET status               = 'Pending',
                reviewed_by_user_id  = NULL,
                reviewed_at          = NULL,
                rejection_reason     = NULL,
                updated_at           = NOW()
            WHERE budget_plan_id = {budgetPlanId} AND status = 'Rejected'
            """, ct);
}
