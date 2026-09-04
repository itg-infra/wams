namespace WAMS.Infrastructure.Repositories.BudgetPlans;

using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WAMS.Application.Common;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class BudgetPlanRepository(
    AppDbContext db,
    ITenantContext tenantContext) : IBudgetPlanRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new();

    private const string DefaultOrderBy = "bp.created_at DESC, bp.\"Id\" DESC";
    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = "bp.status",
            ["createdat"] = "bp.created_at",
            ["docdate"] = "bp.doc_date",
            ["submittedat"] = "bp.submitted_at",
        };

    private sealed record SummaryQueryContext(
        long? TenantCompanyId,
        bool TenantFilterDisabled,
        long[] WarehouseIds,
        string? Search,
        string? SearchPattern,
        DateTime? DateFrom,
        DateTime? DateTo,
        int Offset,
        int Limit);

    public async Task<(List<BudgetPlanSummaryResponse> Items, int TotalCount)> GetAllSummaryAsync(
        BudgetPlanStatus? status,
        BudgetPlanQuery q,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default
    )
    {
        var orderBy = ResolveOrderBy(q.SortBy, q.SortOrder);
        var sql = BuildSummarySql(orderBy);
        var ctx = BuildSummaryQueryContext(q, warehouseIds);

        // Borrow EF's connection - do NOT dispose it. EF Core owns its lifecycle and closes it
        // when the DbContext scope ends. Disposing here would tear down the pooled connection
        // prematurely (and break any later EF query in the same request scope).
        var conn = db.Database.GetDbConnection();

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = CountSummarySql;
        AddCountParameters(countCmd, status, ctx);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct) ?? 0);

        if (ctx.Offset >= total) return ([], total);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddSummaryParameters(cmd, status, ctx);

        var items = new List<BudgetPlanSummaryResponse>();

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colBudgetNo = reader.GetOrdinal("budget_no");
        var colTemplateCode = reader.GetOrdinal("template_code");
        var colRemark = reader.GetOrdinal("remark");
        var colLocation = reader.GetOrdinal("location");
        var colVendorName = reader.GetOrdinal("vendor_name");
        var colMakerName = reader.GetOrdinal("maker_name");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colStatus = reader.GetOrdinal("status");
        var colIsRfba = reader.GetOrdinal("is_rfba");
        var colTotalStages = reader.GetOrdinal("total_stages");
        var colCurrentStage = reader.GetOrdinal("current_stage_order");
        var colStagesJson = reader.GetOrdinal("stages_json");

        while (await reader.ReadAsync(ct))
        {
            var statusStr = reader.GetString(colStatus);
            var statusDisplay = BudgetPlanStatus.TryFromValue(statusStr, out var bps) ? bps.DisplayName : statusStr;

            var totalStages = reader.IsDBNull(colTotalStages) ? 0 : reader.GetInt32(colTotalStages);
            var currentStage = reader.IsDBNull(colCurrentStage) ? 0 : reader.GetInt32(colCurrentStage);
            var stagesJson = reader.IsDBNull(colStagesJson) ? null : reader.GetString(colStagesJson);
            var stages = stagesJson is null
                ? []
                : JsonSerializer.Deserialize<List<WorkflowStageInfo>>(stagesJson, JsonOpts) ?? [];

            items.Add(new BudgetPlanSummaryResponse(
                reader.GetInt64(colId),
                reader.GetString(colBudgetNo),
                reader.GetString(colTemplateCode),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.IsDBNull(colLocation) ? null : reader.GetString(colLocation),
                reader.IsDBNull(colVendorName) ? null : reader.GetString(colVendorName),
                reader.IsDBNull(colMakerName) ? null : reader.GetString(colMakerName),
                reader.GetDateTime(colDocDate),
                statusStr,
                statusDisplay,
                new BudgetPlanApprovalInfo(totalStages, currentStage, stages),
                reader.GetBoolean(colIsRfba)));
        }

        return (items, total);
    }

    public async IAsyncEnumerable<BudgetPlanSummaryResponse> StreamAllAsync(
        BudgetPlanStatus? status,
        BudgetPlanQuery query,
        IReadOnlyList<long>? warehouseIds,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var orderBy = ResolveOrderBy(query.SortBy, query.SortOrder);
        var sql = BuildStreamSummarySql(orderBy);
        var ctx = BuildSummaryQueryContext(query, warehouseIds);

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand { Connection = conn, CommandText = sql };
        AddStreamSummaryParameters(cmd, status, ctx, limit);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colBudgetNo = reader.GetOrdinal("budget_no");
        var colTemplateCode = reader.GetOrdinal("template_code");
        var colRemark = reader.GetOrdinal("remark");
        var colLocation = reader.GetOrdinal("location");
        var colVendorName = reader.GetOrdinal("vendor_name");
        var colMakerName = reader.GetOrdinal("maker_name");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colStatus = reader.GetOrdinal("status");
        var colIsRfba = reader.GetOrdinal("is_rfba");
        var colTotalStages = reader.GetOrdinal("total_stages");
        var colCurrentStage = reader.GetOrdinal("current_stage_order");
        var colStagesJson = reader.GetOrdinal("stages_json");

        while (await reader.ReadAsync(ct))
        {
            var statusStr = reader.GetString(colStatus);
            var statusDisplay = BudgetPlanStatus.TryFromValue(statusStr, out var bps) ? bps.DisplayName : statusStr;

            var totalStages = reader.IsDBNull(colTotalStages) ? 0 : reader.GetInt32(colTotalStages);
            var currentStage = reader.IsDBNull(colCurrentStage) ? 0 : reader.GetInt32(colCurrentStage);
            var stagesJson = reader.IsDBNull(colStagesJson) ? null : reader.GetString(colStagesJson);
            var stages = stagesJson is null
                ? []
                : JsonSerializer.Deserialize<List<WorkflowStageInfo>>(stagesJson, JsonOpts) ?? [];

            yield return new BudgetPlanSummaryResponse(
                reader.GetInt64(colId),
                reader.GetString(colBudgetNo),
                reader.GetString(colTemplateCode),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.IsDBNull(colLocation) ? null : reader.GetString(colLocation),
                reader.IsDBNull(colVendorName) ? null : reader.GetString(colVendorName),
                reader.IsDBNull(colMakerName) ? null : reader.GetString(colMakerName),
                reader.GetDateTime(colDocDate),
                statusStr,
                statusDisplay,
                new BudgetPlanApprovalInfo(totalStages, currentStage, stages),
                reader.GetBoolean(colIsRfba));
        }
    }

    private static string BuildStreamSummarySql(string orderBy) => $@"
        SELECT
            bp.""Id"",
            bp.code AS budget_no,
            bt.code AS template_code,
            bp.remark,
            ws.location,
            (
                SELECT STRING_AGG(DISTINCT v.card_name, ', ' ORDER BY v.card_name)
                FROM budget_plan_items bpi
                JOIN vendor_shadows v ON v.""Id"" = bpi.vendor_shadow_id
                WHERE bpi.budget_plan_id = bp.""Id""
            ) AS vendor_name,
            cu.""Fullname"" AS maker_name,
            bp.doc_date,
            bp.status,
            EXISTS (
                SELECT 1
                FROM budget_plan_items bpi_rfba
                WHERE bpi_rfba.budget_plan_id = bp.""Id""
                  AND bpi_rfba.is_rfba = TRUE
            ) AS is_rfba,
            wi.total_stages,
            wi.current_stage_order,
            wi.stages_json
        FROM budget_plans bp
        JOIN budget_templates bt ON bt.""Id"" = bp.budget_template_id
        JOIN warehouse_shadows ws ON ws.""Id"" = bp.warehouse_shadow_id
        LEFT JOIN users cu ON cu.""Id"" = bp.created_by_user_id
        LEFT JOIN LATERAL (
            SELECT
                COUNT(wis.""Id"")::int AS total_stages,
                wii.current_stage_order,
                JSON_AGG(
                    JSON_BUILD_OBJECT(
                        'StageOrder', wis.stage_order,
                        'StageName', wis.stage_name,
                        'ApproverRoles', wis.approver_roles,
                        'Status', wis.status,
                        'ApprovedAt', wis.approved_at,
                        'ApprovedByName', NULL,
                        'RejectedAt', wis.rejected_at,
                        'RejectedByName', NULL,
                        'RejectionReason', wis.rejection_reason
                    ) ORDER BY wis.stage_order
                )::text AS stages_json
            FROM workflow_instances wii
            JOIN workflow_instance_stages wis ON wis.workflow_instance_id = wii.""Id""
            WHERE wii.""Id"" = bp.workflow_instance_id
            GROUP BY wii.current_stage_order
        ) wi ON TRUE
        WHERE bp.deleted_at IS NULL
        AND (@p_status IS NULL OR bp.status = @p_status)
        AND (@p_tenant_filter_disabled OR bp.company_id = @p_company_id)
        AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
        AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern OR COALESCE(bp.remark, '') ILIKE @p_search_pattern OR ws.name ILIKE @p_search_pattern)
        AND (@p_date_from IS NULL OR bp.doc_date >= @p_date_from)
        AND (@p_date_to IS NULL OR bp.doc_date < @p_date_to)
        ORDER BY {orderBy}
        LIMIT @p_limit;
    ";

    private static void AddStreamSummaryParameters(DbCommand cmd, BudgetPlanStatus? status, SummaryQueryContext ctx, int limit)
    {
        cmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)status?.Value ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = ctx.TenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)ctx.TenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = ctx.WarehouseIds.Length == 0 });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = ctx.WarehouseIds });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)ctx.Search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)ctx.SearchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = (object?)ctx.DateFrom ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = (object?)ctx.DateTo ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = limit });
    }

    private static string ResolveOrderBy(string? sortBy, string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy) || !SortColumns.TryGetValue(sortBy, out var column))
            return DefaultOrderBy;

        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return $"{column} {(desc ? "DESC" : "ASC")}";
    }

    private SummaryQueryContext BuildSummaryQueryContext(BudgetPlanQuery q, IReadOnlyList<long>? warehouseIds)
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;
        var search = string.IsNullOrWhiteSpace(q.Search) ? null : q.Search.Trim();
        return new SummaryQueryContext(
            tenantCompanyId,
            tenantFilterDisabled,
            warehouseIds?.ToArray() ?? [],
            search,
            search is null ? null : LikePatternHelper.ToContainsPattern(search),
            q.DateFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            q.DateTo?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1),
            (q.Page - 1) * q.Limit,
            q.Limit);
    }

    // The @p_*_filter_disabled pattern passes a boolean that short-circuits the filter when
    // no value is provided. This keeps the SQL shape stable (no dynamic string building) while
    // letting PG's optimizer use the right index when the filter IS active.
    private const string CountSummarySql = @"
        SELECT COUNT(*)
        FROM budget_plans bp
        JOIN warehouse_shadows ws ON ws.""Id"" = bp.warehouse_shadow_id
        WHERE bp.deleted_at IS NULL
        AND (@p_status IS NULL OR bp.status = @p_status)
        AND (@p_tenant_filter_disabled OR bp.company_id = @p_company_id)
        AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
        AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern OR COALESCE(bp.remark, '') ILIKE @p_search_pattern OR ws.name ILIKE @p_search_pattern)
        AND (@p_date_from IS NULL OR bp.doc_date >= @p_date_from)
        AND (@p_date_to IS NULL OR bp.doc_date < @p_date_to);";

    private static string BuildSummarySql(string orderBy) => $@"
        SELECT
            bp.""Id"",
            bp.code AS budget_no,
            bt.code AS template_code,
            bp.remark,
            ws.location,
            (
                SELECT STRING_AGG(DISTINCT v.card_name, ', ' ORDER BY v.card_name)
                FROM budget_plan_items bpi
                JOIN vendor_shadows v ON v.""Id"" = bpi.vendor_shadow_id
                WHERE bpi.budget_plan_id = bp.""Id""
            ) AS vendor_name,
            cu.""Fullname"" AS maker_name,
            bp.doc_date,
            bp.status,
            EXISTS (
                SELECT 1
                FROM budget_plan_items bpi_rfba
                WHERE bpi_rfba.budget_plan_id = bp.""Id""
                  AND bpi_rfba.is_rfba = TRUE
            ) AS is_rfba,
            wi.total_stages,
            wi.current_stage_order,
            wi.stages_json
        FROM budget_plans bp
        JOIN budget_templates bt ON bt.""Id"" = bp.budget_template_id
        JOIN warehouse_shadows ws ON ws.""Id"" = bp.warehouse_shadow_id
        LEFT JOIN users cu ON cu.""Id"" = bp.created_by_user_id
        -- Aggregate workflow stage info into a single JSON blob per plan for efficient list rendering.
        LEFT JOIN LATERAL (
            SELECT
                COUNT(wis.""Id"")::int AS total_stages,
                wii.current_stage_order,
                JSON_AGG(
                    JSON_BUILD_OBJECT(
                        'StageOrder', wis.stage_order,
                        'StageName', wis.stage_name,
                        'ApproverRoles', wis.approver_roles,
                        'Status', wis.status,
                        'ApprovedAt', wis.approved_at,
                        'ApprovedByName', NULL,
                        'RejectedAt', wis.rejected_at,
                        'RejectedByName', NULL,
                        'RejectionReason', wis.rejection_reason
                    ) ORDER BY wis.stage_order
                )::text AS stages_json
            FROM workflow_instances wii
            JOIN workflow_instance_stages wis ON wis.workflow_instance_id = wii.""Id""
            WHERE wii.""Id"" = bp.workflow_instance_id
            GROUP BY wii.current_stage_order
        ) wi ON TRUE
        WHERE bp.deleted_at IS NULL
        AND (@p_status IS NULL OR bp.status = @p_status)
        AND (@p_tenant_filter_disabled OR bp.company_id = @p_company_id)
        AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
        AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern OR COALESCE(bp.remark, '') ILIKE @p_search_pattern OR ws.name ILIKE @p_search_pattern)
        AND (@p_date_from IS NULL OR bp.doc_date >= @p_date_from)
        AND (@p_date_to IS NULL OR bp.doc_date < @p_date_to)
        ORDER BY {orderBy}
        OFFSET @p_offset
        LIMIT @p_limit;
    ";

    private static void AddCountParameters(DbCommand cmd, BudgetPlanStatus? status, SummaryQueryContext ctx)
    {
        cmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)status?.Value ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = ctx.TenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)ctx.TenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = ctx.WarehouseIds.Length == 0 });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = ctx.WarehouseIds });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)ctx.Search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)ctx.SearchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = (object?)ctx.DateFrom ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = (object?)ctx.DateTo ?? DBNull.Value });
    }

    private static void AddSummaryParameters(DbCommand cmd, BudgetPlanStatus? status, SummaryQueryContext ctx)
    {
        cmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)status?.Value ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = ctx.TenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)ctx.TenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = ctx.WarehouseIds.Length == 0 });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = ctx.WarehouseIds });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)ctx.Search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)ctx.SearchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = (object?)ctx.DateFrom ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = (object?)ctx.DateTo ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = ctx.Offset });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = ctx.Limit });
    }

    // Tracked load for write paths (update, spk add/remove).
    // For read-only GET detail use GetByIdDetailReadAsync (AsNoTracking + AsSplitQuery).
    // For submit use GetByIdForSubmitAsync (lighter includes).
    public async Task<BudgetPlan?> GetByIdWithItemsAsync(long id, CancellationToken ct = default)
        => await db.BudgetPlans
            .Where(b => b.DeletedAt == null)
            .Include(b => b.BudgetTemplate)
            .Include(b => b.Warehouse)
            .Include(b => b.Items)
                .ThenInclude(i => i.Item)
            .Include(b => b.Items)
                .ThenInclude(i => i.Vendor)
            .Include(b => b.Items)
                .ThenInclude(i => i.Uom)
            .Include(b => b.Items)
                .ThenInclude(i => i.ActivityType)
            .Include(b => b.CreatedBy)
            .Include(b => b.SubmittedBy)
            .Include(b => b.RejectedBy)
            .Include(b => b.SpkItems)
                .ThenInclude(s => s.Spk)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<BudgetPlan?> GetByIdWithItemsAndWorkOrdersAsync(long id, CancellationToken ct = default)
    {
        var plan = await db.BudgetPlans
            .Where(b => b.DeletedAt == null)
            .Include(b => b.BudgetTemplate)
            .Include(b => b.Warehouse)
            .Include(b => b.Items)
                .ThenInclude(i => i.Item)
            .Include(b => b.Items)
                .ThenInclude(i => i.Vendor)
            .Include(b => b.Items)
                .ThenInclude(i => i.Uom)
            .Include(b => b.Items)
                .ThenInclude(i => i.ActivityType)
            .Include(b => b.CreatedBy)
            .Include(b => b.SubmittedBy)
            .Include(b => b.RejectedBy)
            .Include(b => b.SpkItems)
                .ThenInclude(s => s.Spk)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (plan is null) return null;

        // Loaded as a separate query with IgnoreQueryFilters() scoped to WorkOrder only - a plain
        // .Include(b => b.WorkOrders) above would silently apply WorkOrder's global DeletedAt filter.
        // This method exists solely to support the FK-safety check in BudgetPlanService.UpdateAsync,
        // which must see every WorkOrder that still holds a live (Restrict) FK to a BudgetPlanItem,
        // including soft-deleted ones. Tracked entities with a matching FK are auto-wired into
        // plan.WorkOrders by EF's relationship fixup, so no explicit assignment is needed.
        await db.WorkOrders
            .IgnoreQueryFilters()
            .Where(w => w.BudgetPlanId == id)
            .ToListAsync(ct);

        return plan;
    }

    // Tracked lightweight load for submit path - only needs BudgetPlan.WarehouseShadowId and Items.Any().
    public async Task<BudgetPlan?> GetByIdForSubmitAsync(long id, CancellationToken ct = default)
        => await db.BudgetPlans
            .Where(b => b.DeletedAt == null)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    // Scalar projection for the WO create path. One SQL round-trip, no entity hydration.
    public async Task<BpForWoCreateProjection?> GetForWoCreateAsync(long id, CancellationToken ct = default)
    {
        var header = await db.BudgetPlans
            .AsNoTracking()
            .Where(b => b.Id == id && b.DeletedAt == null)
            .Select(b => new
            {
                b.Id,
                StatusValue = b.Status.Value,
                b.CompanyId,
                b.WarehouseShadowId,
                TemplateCode = b.BudgetTemplate.Code,
            })
            .FirstOrDefaultAsync(ct);

        if (header == null) return null;

        var items = await db.BudgetPlanItems
            .AsNoTracking()
            .Where(i => i.BudgetPlanId == id)
            .Select(i => new { i.Id, i.ItemShadowId, AtCode = i.ActivityType!.Code, i.IsRfba })
            .ToListAsync(ct);

        return new BpForWoCreateProjection(
            header.Id,
            header.StatusValue,
            header.CompanyId,
            header.WarehouseShadowId,
            header.TemplateCode,
            items.Select(i => new BpItemForWo(i.Id, i.ItemShadowId, i.AtCode, i.IsRfba)).ToList());
    }

    public async Task<BudgetPlanResponse?> GetByIdProjectionAsync(long id, CancellationToken ct = default)
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        // Query 1: header (1 row) 
        const string headerSql = """
            SELECT  bp."Id"                          AS bp_id,
                    bp.code                          AS bp_code,
                    bp.remark                        AS remark,
                    bp.doc_date                      AS doc_date,
                    bp.status                        AS bp_status,
                    bp.created_at                    AS created_at,
                    bp.submitted_at                  AS submitted_at,
                    bp.rejected_at                   AS rejected_at,
                    bp.rejection_reason              AS rejection_reason,
                    bp.workflow_instance_id          AS workflow_instance_id,
                    bt."Id"                          AS bt_id,
                    bt.code                          AS bt_code,
                    bt.province_id                   AS bt_province_id,
                    btp.name                         AS bt_province_name,
                    btp.display                      AS bt_province_display,
                    ws.code                          AS wh_code,
                    ws.name                          AS wh_name,
                    cu."Fullname"                    AS creator_name,
                    sb."Fullname"                    AS submitter_name,
                    rb."Fullname"                    AS rejector_name
            FROM budget_plans bp
            JOIN budget_templates bt        ON bt."Id" = bp.budget_template_id AND bt.deleted_at IS NULL
            LEFT JOIN provinces btp         ON btp."Id" = bt.province_id
            JOIN warehouse_shadows ws       ON ws."Id" = bp.warehouse_shadow_id
            LEFT JOIN users cu              ON cu."Id" = bp.created_by_user_id AND cu.deleted_at IS NULL
            LEFT JOIN users sb              ON sb."Id" = bp.submitted_by_user_id AND sb.deleted_at IS NULL
            LEFT JOIN users rb              ON rb."Id" = bp.rejected_by_user_id  AND rb.deleted_at IS NULL
            WHERE bp."Id" = @p_id
              AND bp.deleted_at IS NULL
              AND (@p_tenant_disabled OR bp.company_id = @p_company_id)
            """;

        long btId;
        long? workflowInstanceId, btProvinceId;
        string bpCode, bpStatusStr, btCode, whCode, whName;
        string? remark, btProvinceName, btProvinceDisplay, creatorName, submitterName, rejectorName, rejectionReason;
        DateTime docDate, createdAt;
        DateTime? submittedAt, rejectedAt;

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = headerSql;
            cmd.Parameters.Add(new NpgsqlParameter("p_id", NpgsqlDbType.Bigint) { Value = id });
            cmd.Parameters.Add(new NpgsqlParameter("p_tenant_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
            cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            bpCode             = reader.GetString(reader.GetOrdinal("bp_code"));
            remark             = ReadStringOrNull(reader, "remark");
            docDate            = reader.GetDateTime(reader.GetOrdinal("doc_date"));
            bpStatusStr        = reader.GetString(reader.GetOrdinal("bp_status"));
            createdAt          = reader.GetDateTime(reader.GetOrdinal("created_at"));
            submittedAt        = ReadDateTimeOrNull(reader, "submitted_at");
            rejectedAt         = ReadDateTimeOrNull(reader, "rejected_at");
            rejectionReason    = ReadStringOrNull(reader, "rejection_reason");
            workflowInstanceId = ReadInt64OrNull(reader, "workflow_instance_id");
            btId               = reader.GetInt64(reader.GetOrdinal("bt_id"));
            btCode             = reader.GetString(reader.GetOrdinal("bt_code"));
            btProvinceId       = ReadInt64OrNull(reader, "bt_province_id");
            btProvinceName     = ReadStringOrNull(reader, "bt_province_name");
            btProvinceDisplay  = ReadStringOrNull(reader, "bt_province_display");
            whCode             = reader.GetString(reader.GetOrdinal("wh_code"));
            whName             = reader.GetString(reader.GetOrdinal("wh_name"));
            creatorName        = ReadStringOrNull(reader, "creator_name");
            submitterName      = ReadStringOrNull(reader, "submitter_name");
            rejectorName       = ReadStringOrNull(reader, "rejector_name");
        }

        // Query 2: items 
        var items = new List<BudgetPlanItemResponse>();
        const string itemsSql = """
            SELECT  bpi."Id"            AS id,
                    bpi.item_shadow_id  AS item_shadow_id,
                    i.item_code         AS item_code,
                    i.item_name         AS item_name,
                    i.acct_code         AS acct_code,
                    i.acct_name         AS acct_name,
                    bpi.vendor_shadow_id AS vendor_shadow_id,
                    v.card_code         AS vendor_code,
                    v.card_name         AS vendor_name,
                    bpi.uom_master_id   AS uom_id,
                    u.code              AS uom_code,
                    u.name              AS uom_name,
                    bpi.cost_value      AS cost_value,
                    bpi.quantity        AS quantity,
                    bpi.total_value     AS total_value,
                    bpi.sort_order      AS sort_order,
                    bpi.type            AS type,
                    bpi.is_rfba         AS is_rfba,
                    bpi.doc_external    AS doc_external,
                    bpi.bill_of_lading  AS bill_of_lading,
                    bpi.description     AS description,
                    bpi.activity_type_id AS activity_type_id,
                    bpi_at.code         AS activity_type_code,
                    bpi_at.name         AS activity_type_name,
                    bpi.spk_shadow_id   AS spk_shadow_id,
                    bpi.ppn_tax_type_code AS ppn_tax_type_code,
                    bpi.ppn_rate        AS ppn_rate,
                    bpi.pph_tax_type_code AS pph_tax_type_code,
                    bpi.pph_rate        AS pph_rate,
                    bpi.ppn_amount      AS ppn_amount,
                    bpi.pph_amount      AS pph_amount,
                    bpi.grand_total     AS grand_total,
                    bpi.cost_treatment  AS cost_treatment
            FROM budget_plan_items bpi
            JOIN item_shadows i   ON i."Id"  = bpi.item_shadow_id
            JOIN vendor_shadows v ON v."Id"  = bpi.vendor_shadow_id
            JOIN uom_masters u    ON u."Id"  = bpi.uom_master_id AND u.deleted_at IS NULL
            LEFT JOIN activity_types bpi_at ON bpi_at."Id" = bpi.activity_type_id AND bpi_at.deleted_at IS NULL
            WHERE bpi.budget_plan_id = @p_bp_id
            ORDER BY bpi.sort_order
            """;

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = itemsSql;
            cmd.Parameters.Add(new NpgsqlParameter("p_bp_id", NpgsqlDbType.Bigint) { Value = id });
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new BudgetPlanItemResponse(
                    Id:               reader.GetInt64(0),
                    ItemShadowId:     reader.GetInt64(1),
                    CostDetail:       reader.GetString(2),
                    CostName:         reader.GetString(3),
                    Coa:              reader.GetString(4),
                    CoaName:          reader.GetString(5),
                    VendorShadowId:   reader.GetInt64(6),
                    VendorCode:       reader.GetString(7),
                    VendorName:       reader.GetString(8),
                    UomMasterId:      reader.GetInt64(9),
                    UomCode:          reader.GetString(10),
                    UomName:          reader.GetString(11),
                    CostValue:        reader.GetDecimal(12),
                    Quantity:         reader.GetDecimal(13),
                    TotalValue:       reader.GetDecimal(14),
                    SortOrder:        reader.GetInt32(15),
                    Type:             reader.GetString(16),
                    IsRfba:           reader.GetBoolean(17),
                    DocExternal:      reader.IsDBNull(18) ? null : reader.GetString(18),
                    BillOfLading:     reader.IsDBNull(19) ? null : reader.GetString(19),
                    Description:      reader.IsDBNull(20) ? null : reader.GetString(20),
                    ActivityTypeId:   reader.GetInt64(21),
                    ActivityTypeCode: reader.IsDBNull(22) ? null : reader.GetString(22),
                    ActivityTypeName: reader.IsDBNull(23) ? null : reader.GetString(23),
                    SpkShadowId:      reader.IsDBNull(24) ? null : reader.GetInt64(24),
                    PpnTaxTypeCode:   reader.IsDBNull(25) ? null : reader.GetString(25),
                    PpnRate:          reader.GetDecimal(26),
                    PphTaxTypeCode:   reader.IsDBNull(27) ? null : reader.GetString(27),
                    PphRate:          reader.GetDecimal(28),
                    PpnAmount:        reader.GetDecimal(29),
                    PphAmount:        reader.GetDecimal(30),
                    GrandTotal:       reader.GetDecimal(31),
                    CostTreatment:    reader.IsDBNull(32) ? null : reader.GetString(32)));
            }
        }

        // Query 3: SPK items 
        var spkItems = new List<BudgetPlanSpkItemResponse>();
        const string spkSql = """
            SELECT  bsi."Id"         AS id,
                    bsi.spk_shadow_id AS spk_shadow_id,
                    s.type, s.doc_no, s.base_doc, s.base_doc_no, s.card_code, s.card_name,
                    s.item_code, s.item_name, s.quantity, s.delivery_qty, s.uom, s.pack_type,
                    s.whs_code, s.whs_name, s.doc_status, s.bl_no,
                    bsi.sort_order,
                    i."Id" AS item_shadow_id
            FROM budget_plan_spk_items bsi
            JOIN spk_shadows s ON s."Id" = bsi.spk_shadow_id
            -- Resolve item_shadow_id by item_code+company so the frontend can populate the cost detail dropdown.
            LEFT JOIN item_shadows i ON i.item_code = s.item_code AND i.company_id = s.company_id
            WHERE bsi.budget_plan_id = @p_bp_id
            ORDER BY bsi.sort_order
            """;

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = spkSql;
            cmd.Parameters.Add(new NpgsqlParameter("p_bp_id", NpgsqlDbType.Bigint) { Value = id });
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                spkItems.Add(new BudgetPlanSpkItemResponse(
                    Id:           reader.GetInt64(0),
                    SpkShadowId:  reader.GetInt64(1),
                    Type:         reader.GetString(2),
                    DocNo:        reader.GetString(3),
                    BaseDoc:      reader.GetString(4),
                    BaseDocNo:    reader.GetString(5),
                    CardCode:     reader.GetString(6),
                    CardName:     reader.GetString(7),
                    ItemCode:     reader.GetString(8),
                    ItemName:     reader.GetString(9),
                    Quantity:     reader.IsDBNull(10) ? null : reader.GetDecimal(10),
                    DeliveryQty:  reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                    UoM:          reader.GetString(12),
                    PackType:     reader.GetString(13),
                    WhsCode:      reader.GetString(14),
                    WhsName:      reader.GetString(15),
                    DocStatus:    reader.GetString(16),
                    BlNo:         reader.IsDBNull(17) ? null : reader.GetString(17),
                    SortOrder:    reader.GetInt32(18),
                    ItemShadowId: reader.IsDBNull(19) ? null : reader.GetInt64(19)));
            }
        }

        // Query 4: workflow stages (only if BP has an instance) 
        var stages = new List<WorkflowStageInfo>();
        var currentStageOrder = 0;
        if (workflowInstanceId.HasValue)
        {
            const string wfSql = """
                SELECT  wi.current_stage_order,
                        wis.stage_order, wis.stage_name, wis.approver_roles, wis.status,
                        wis.approved_at, ap."Fullname"  AS approved_by_name,
                        wis.rejected_at, rp."Fullname"  AS rejected_by_name,
                        wis.rejection_reason
                FROM workflow_instances wi
                JOIN workflow_instance_stages wis ON wis.workflow_instance_id = wi."Id"
                LEFT JOIN users ap ON ap."Id" = wis.approved_by_user_id AND ap.deleted_at IS NULL
                LEFT JOIN users rp ON rp."Id" = wis.rejected_by_user_id AND rp.deleted_at IS NULL
                WHERE wi."Id" = @p_wi_id
                ORDER BY wis.stage_order
                """;

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = wfSql;
            cmd.Parameters.Add(new NpgsqlParameter("p_wi_id", NpgsqlDbType.Bigint) { Value = workflowInstanceId.Value });
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                currentStageOrder = reader.GetInt32(0);
                var approverRoles = JsonSerializer.Deserialize<string[]>(reader.GetString(3)) ?? [];
                stages.Add(new WorkflowStageInfo(
                    StageOrder:      reader.GetInt32(1),
                    StageName:       reader.GetString(2),
                    ApproverRoles:   approverRoles,
                    Status:          reader.GetString(4),
                    ApprovedAt:      reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    ApprovedByName:  reader.IsDBNull(6) ? null : reader.GetString(6),
                    RejectedAt:      reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    RejectedByName:  reader.IsDBNull(8) ? null : reader.GetString(8),
                    RejectionReason: reader.IsDBNull(9) ? null : reader.GetString(9)));
            }
        }

        var statusDisplay = BudgetPlanStatus.TryFromValue(bpStatusStr, out var bps) ? bps.DisplayName : bpStatusStr;
        var approval = new BudgetPlanApprovalInfo(stages.Count, currentStageOrder, stages);
        var grandTotal = items.Sum(i => i.TotalValue);
        var totalPpnAmount = items.Sum(i => i.PpnAmount);
        var totalPphAmount = items.Sum(i => i.PphAmount);
        var taxInclusiveGrandTotal = items.Sum(i => i.GrandTotal);

        return new BudgetPlanResponse(
            Id:               id,
            BudgetNo:         bpCode,
            Template:         new BudgetTemplateSummaryInfo(btId, btCode, btProvinceId, btProvinceName, btProvinceDisplay),
            WarehouseCode:    whCode,
            WarehouseName:    whName,
            Remark:           remark,
            DocDate:          docDate,
            Status:           bpStatusStr,
            StatusDisplay:    statusDisplay,
            SpkItems:         spkItems,
            Items:            items,
            GrandTotal:       grandTotal,
            TotalPpnAmount:   totalPpnAmount,
            TotalPphAmount:   totalPphAmount,
            TaxInclusiveGrandTotal: taxInclusiveGrandTotal,
            CreatedAt:        createdAt,
            CreatedByName:    creatorName ?? string.Empty,
            SubmittedAt:      submittedAt,
            SubmittedByName:  submitterName,
            Approval:         approval,
            RejectedAt:       rejectedAt,
            RejectedByName:   rejectorName,
            RejectionReason:  rejectionReason);
    }

    private static string? ReadStringOrNull(DbDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetString(i);
    }

    private static DateTime? ReadDateTimeOrNull(DbDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetDateTime(i);
    }

    private static long? ReadInt64OrNull(DbDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? null : reader.GetInt64(i);
    }

    public async Task<BudgetPlan?> GetByIdDetailReadAsync(long id, CancellationToken ct = default)
        => await db.BudgetPlans
            .Where(b => b.DeletedAt == null)
            .Include(b => b.BudgetTemplate)
            .Include(b => b.Warehouse)
            .Include(b => b.Items)
                .ThenInclude(i => i.Item)
            .Include(b => b.Items)
                .ThenInclude(i => i.Vendor)
            .Include(b => b.Items)
                .ThenInclude(i => i.Uom)
            .Include(b => b.Items)
                .ThenInclude(i => i.ActivityType)
            .Include(b => b.CreatedBy)
            .Include(b => b.SubmittedBy)
            .Include(b => b.RejectedBy)
            .Include(b => b.WorkflowInstance)
                .ThenInclude(wi => wi!.Stages.OrderBy(s => s.StageOrder))
                    .ThenInclude(s => s.ApprovedBy)
            .Include(b => b.WorkflowInstance)
                .ThenInclude(wi => wi!.Stages.OrderBy(s => s.StageOrder))
                    .ThenInclude(s => s.RejectedBy)
            .Include(b => b.SpkItems)
                .ThenInclude(s => s.Spk)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    // Tracked load for approve/reject paths - includes WorkflowInstance + Stages without item details.
    public async Task<BudgetPlan?> GetByIdForApprovalAsync(long id, CancellationToken ct = default)
        => await db.BudgetPlans
            .Where(b => b.DeletedAt == null)
            .Include(b => b.Warehouse)
            .Include(b => b.WorkflowInstance)
                .ThenInclude(wi => wi!.Stages)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<long?> GetWarehouseShadowIdAsync(long id, CancellationToken ct = default)
    {
        // Two-step access check (warehouse_shadow_id → access) avoids loading the entity tree
        // when the user has no access at all. Used as the pre-flight for GET /budget-plans/{id}.
        return await db.BudgetPlans
            .Where(b => b.Id == id && b.DeletedAt == null)
            .Select(b => (long?)b.WarehouseShadowId)
            .FirstOrDefaultAsync(ct);
    }

    public Task<BudgetPlan?> GetSummaryAsync(long id, CancellationToken ct = default)
        => db.BudgetPlans
            .Where(b => b.DeletedAt == null && b.Id == id)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

    public Task CreateAsync(BudgetPlan plan, CancellationToken ct = default)
    {
        db.BudgetPlans.Add(plan);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BudgetPlan plan, CancellationToken ct = default)
    {
        db.BudgetPlans.Update(plan);
        return Task.CompletedTask;
    }

    // Pushes the cutoff filter to the DB - avoids loading all pending plans into memory.
    // Step 1: raw SQL returns IDs of overdue plans. Step 2: EF loads them with required includes.
    public async Task<List<BudgetPlan>> GetOverdueForReminderAsync(DateTime cutoff, CancellationToken ct = default)
    {
        var overdueIds = await db.Database
            .SqlQuery<long>($"""
                SELECT DISTINCT bp."Id" AS "Value"
                FROM budget_plans bp
                JOIN workflow_instances wi ON wi."Id" = bp.workflow_instance_id
                JOIN workflow_instance_stages pending
                    ON pending.workflow_instance_id = wi."Id" AND pending.status = 'Pending'
                LEFT JOIN workflow_instance_stages prev
                    ON prev.workflow_instance_id = wi."Id"
                    AND prev.stage_order = pending.stage_order - 1
                    AND prev.status = 'Approved'
                WHERE bp.deleted_at IS NULL
                  AND bp.status IN ('Submitted', 'InApproval')
                  AND (
                      (pending.stage_order = 1 AND bp.submitted_at < {cutoff})
                      OR (pending.stage_order > 1 AND prev.approved_at < {cutoff})
                  )
                """)
            .ToListAsync(ct);

        if (overdueIds.Count == 0) return [];

        return await db.BudgetPlans
            .Where(b => overdueIds.Contains(b.Id))
            .Include(b => b.WorkflowInstance)
                .ThenInclude(wi => wi!.Stages)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
        => await db.BudgetPlans
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.DeletedAt, DateTime.UtcNow), ct);

    public async Task SetItemsDocExternalAsync(
        List<long> itemIds,
        string docExternal,
        CancellationToken ct = default
    )
    {
        if (itemIds.Count == 0) return;
        var ids = itemIds.ToArray();
        await db.Database.ExecuteSqlAsync(
            $"UPDATE budget_plan_items SET doc_external = {docExternal} WHERE \"Id\" = ANY({ids})",
            ct);
    }

    public async Task RejectViaRecapAsync(
        long budgetPlanId,
        long userId,
        DateTime rejectedAt,
        string? reason,
        CancellationToken ct = default
    )
        => await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE budget_plans
            SET status              = 'Rejected',
                rejected_by_user_id = {userId},
                rejected_at         = {rejectedAt},
                rejection_reason    = {reason},
                updated_at          = NOW()
            WHERE "Id" = {budgetPlanId}
            """, ct);
}
