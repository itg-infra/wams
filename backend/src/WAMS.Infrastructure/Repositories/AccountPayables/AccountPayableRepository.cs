namespace WAMS.Infrastructure.Repositories.AccountPayables;

using System.Data;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AccountPayables;
using WAMS.Application.Interfaces.AccountPayables;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.AccountPayables;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class AccountPayableRepository(
    AppDbContext db,
    ITenantContext tenantContext) : IAccountPayableRepository
{
    private const string DefaultOrderBy = "ap.created_at DESC NULLS LAST, ap.\"Id\" DESC";
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = "ap.status",
            ["docdate"] = "ap.doc_date",
            ["createdat"] = "ap.created_at",
        };

    public async Task<(List<AccountPayableSummaryResponse> Items, int TotalCount)> GetAllAsync(
        AccountPayableQuery q,
        CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;

        var orderBy = SortColumns.TryGetValue(q.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(q.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : DefaultOrderBy;

        var search = string.IsNullOrWhiteSpace(q.Search) ? null : q.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);
        var dateFrom = q.DateFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dateTo = q.DateTo?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);
        var offset = (q.Page - 1) * q.Limit;

        var sql = $@"
            SELECT
                ap.""Id"",
                ap.code,
                v.card_code,
                v.card_name,
                ap.status,
                ap.doc_date,
                ap.remark,
                ap.sap_ap_number,
                COALESCE(SUM(api.budget_plan_total), 0) AS grand_total,
                COUNT(api.""Id"")::int AS item_count,
                ap.created_at,
                cu.""Fullname"" AS created_by_name,
                COUNT(*) OVER() AS total_count
            FROM account_payables ap
            JOIN vendor_shadows v ON v.""Id"" = ap.vendor_shadow_id
            LEFT JOIN users cu ON cu.""Id"" = ap.created_by_user_id
            LEFT JOIN account_payable_items api ON api.account_payable_id = ap.""Id""
            WHERE ap.deleted_at IS NULL
              AND (@p_tenant_filter_disabled OR ap.company_id = @p_company_id)
              AND (@p_status IS NULL OR ap.status = @p_status)
              AND (@p_vendor_id IS NULL OR ap.vendor_shadow_id = @p_vendor_id)
              AND (@p_search IS NULL
                   OR ap.code ILIKE @p_search_pattern
                   OR v.card_name ILIKE @p_search_pattern
                   OR COALESCE(ap.remark, '') ILIKE @p_search_pattern)
              AND (@p_date_from IS NULL OR ap.doc_date >= @p_date_from)
              AND (@p_date_to IS NULL OR ap.doc_date < @p_date_to)
            GROUP BY ap.""Id"", v.card_code, v.card_name, cu.""Fullname""
            ORDER BY {orderBy}
            OFFSET @p_offset LIMIT @p_limit;";

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)q.Status ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_vendor_id", NpgsqlDbType.Bigint) { Value = (object?)q.VendorShadowId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = (object?)dateFrom ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = (object?)dateTo ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = offset });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = q.Limit });

        var items = new List<AccountPayableSummaryResponse>();
        var total = 0;

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colCode = reader.GetOrdinal("code");
        var colVendorCode = reader.GetOrdinal("card_code");
        var colVendorName = reader.GetOrdinal("card_name");
        var colStatus = reader.GetOrdinal("status");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colRemark = reader.GetOrdinal("remark");
        var colSapApNumber = reader.GetOrdinal("sap_ap_number");
        var colGrandTotal = reader.GetOrdinal("grand_total");
        var colItemCount = reader.GetOrdinal("item_count");
        var colCreatedAt = reader.GetOrdinal("created_at");
        var colCreatedByName = reader.GetOrdinal("created_by_name");
        var colTotalCount = reader.GetOrdinal("total_count");

        while (await reader.ReadAsync(ct))
        {
            total = reader.GetInt32(colTotalCount);
            items.Add(new AccountPayableSummaryResponse(
                reader.GetInt64(colId),
                reader.GetString(colCode),
                reader.GetString(colVendorCode),
                reader.GetString(colVendorName),
                reader.GetString(colStatus),
                reader.GetDateTime(colDocDate),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.IsDBNull(colSapApNumber) ? null : reader.GetString(colSapApNumber),
                reader.GetDecimal(colGrandTotal),
                reader.GetInt32(colItemCount),
                reader.GetDateTime(colCreatedAt),
                reader.IsDBNull(colCreatedByName) ? "" : reader.GetString(colCreatedByName)));
        }

        return (items, total);
    }

    public async IAsyncEnumerable<AccountPayableSummaryResponse> StreamAllAsync(
        AccountPayableQuery q,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;

        var orderBy = SortColumns.TryGetValue(q.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(q.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : DefaultOrderBy;

        var search = string.IsNullOrWhiteSpace(q.Search) ? null : q.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);
        var dateFrom = q.DateFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dateTo = q.DateTo?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var sql = $@"
            SELECT
                ap.""Id"",
                ap.code,
                v.card_code,
                v.card_name,
                ap.status,
                ap.doc_date,
                ap.remark,
                ap.sap_ap_number,
                COALESCE(SUM(api.budget_plan_total), 0) AS grand_total,
                COUNT(api.""Id"")::int AS item_count,
                ap.created_at,
                cu.""Fullname"" AS created_by_name
            FROM account_payables ap
            JOIN vendor_shadows v ON v.""Id"" = ap.vendor_shadow_id
            LEFT JOIN users cu ON cu.""Id"" = ap.created_by_user_id
            LEFT JOIN account_payable_items api ON api.account_payable_id = ap.""Id""
            WHERE ap.deleted_at IS NULL
              AND (@p_tenant_filter_disabled OR ap.company_id = @p_company_id)
              AND (@p_status IS NULL OR ap.status = @p_status)
              AND (@p_vendor_id IS NULL OR ap.vendor_shadow_id = @p_vendor_id)
              AND (@p_search IS NULL
                   OR ap.code ILIKE @p_search_pattern
                   OR v.card_name ILIKE @p_search_pattern
                   OR COALESCE(ap.remark, '') ILIKE @p_search_pattern)
              AND (@p_date_from IS NULL OR ap.doc_date >= @p_date_from)
              AND (@p_date_to IS NULL OR ap.doc_date < @p_date_to)
            GROUP BY ap.""Id"", v.card_code, v.card_name, cu.""Fullname""
            ORDER BY {orderBy}
            LIMIT @p_limit;";

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand { Connection = conn, CommandText = sql };
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)q.Status ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_vendor_id", NpgsqlDbType.Bigint) { Value = (object?)q.VendorShadowId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = (object?)dateFrom ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = (object?)dateTo ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = limit });

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colCode = reader.GetOrdinal("code");
        var colVendorCode = reader.GetOrdinal("card_code");
        var colVendorName = reader.GetOrdinal("card_name");
        var colStatus = reader.GetOrdinal("status");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colRemark = reader.GetOrdinal("remark");
        var colSapApNumber = reader.GetOrdinal("sap_ap_number");
        var colGrandTotal = reader.GetOrdinal("grand_total");
        var colItemCount = reader.GetOrdinal("item_count");
        var colCreatedAt = reader.GetOrdinal("created_at");
        var colCreatedByName = reader.GetOrdinal("created_by_name");

        while (await reader.ReadAsync(ct))
        {
            yield return new AccountPayableSummaryResponse(
                reader.GetInt64(colId),
                reader.GetString(colCode),
                reader.GetString(colVendorCode),
                reader.GetString(colVendorName),
                reader.GetString(colStatus),
                reader.GetDateTime(colDocDate),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.IsDBNull(colSapApNumber) ? null : reader.GetString(colSapApNumber),
                reader.GetDecimal(colGrandTotal),
                reader.GetInt32(colItemCount),
                reader.GetDateTime(colCreatedAt),
                reader.IsDBNull(colCreatedByName) ? "" : reader.GetString(colCreatedByName));
        }
    }

    public async Task<AccountPayable?> GetByIdWithItemsAsync(long id, CancellationToken ct = default)
        // AsSingleQuery: AP has at most ~10 items × 1 BudgetPlan each → fan-out is tiny, one round-trip
        // beats split-query's 5 SELECTs.
        => await db.AccountPayables
            .Include(a => a.Vendor)
            .Include(a => a.CreatedBy)
            .Include(a => a.GeneratedBy)
            .Include(a => a.Items)
                .ThenInclude(i => i.BudgetPlanItem)
                    .ThenInclude(bpi => bpi.BudgetPlan)
                        .ThenInclude(bp => bp.Warehouse)
            .Include(a => a.Items)
                .ThenInclude(i => i.BudgetPlanItem)
                    .ThenInclude(bpi => bpi.Spk)
            .AsSingleQuery()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<List<BudgetPlanItem>> GetAvailableItemsAsync(
        long vendorShadowId,
        List<long> budgetPlanItemIds,
        long? excludeDocumentId = null,
        List<long>? warehouseIds = null,
        CancellationToken ct = default
    )
        => await AvailableItemsBaseQuery(vendorShadowId, excludeDocumentId, warehouseIds)
            .Where(bpi => budgetPlanItemIds.Count == 0 || budgetPlanItemIds.Contains(bpi.Id))
            .ToListAsync(ct);

    // Breaks down AvailableItemsBaseQuery's combined predicate per item so callers can report
    // the specific reason (not found / vendor mismatch / recap not approved / already generated /
    // already taken by another Draft+ document) instead of one merged "unavailable" message.
    //
    // No excludeDocumentId here: this is only ever called with ids that already failed the
    // (exclusion-aware) availability query, so anything reaching this method is genuinely
    // taken by ANOTHER document.
    public async Task<List<BudgetPlanItemAvailability>> GetAvailabilityDiagnosticsAsync(
        long vendorShadowId,
        List<long> itemIds,
        List<long>? warehouseIds = null,
        CancellationToken ct = default
    )
    {
        var found = await db.BudgetPlanItems
            .Where(bpi => itemIds.Contains(bpi.Id) && bpi.BudgetPlan.DeletedAt == null)
            .Select(bpi => new BudgetPlanItemAvailability(
                bpi.Id,
                true,
                bpi.VendorShadowId == vendorShadowId,
                warehouseIds == null || warehouseIds.Contains(bpi.BudgetPlan.WarehouseShadowId),
                db.RecapWorkOrders.Any(r =>
                    r.BudgetPlanId == bpi.BudgetPlanId &&
                    r.Status == RecapWorkOrderStatus.Approved),
                db.AccountPayableItems.Any(api =>
                    api.BudgetPlanItemId == bpi.Id &&
                    api.AccountPayable.Status == AccountPayableStatus.Generated &&
                    api.AccountPayable.DeletedAt == null),
                TakenByAnotherAccountPayable(null)
                    .Where(api => api.BudgetPlanItemId == bpi.Id)
                    .OrderByDescending(api => api.AccountPayableId)
                    .Select(api => api.AccountPayable.Code)
                    .FirstOrDefault(),
                bpi.VendorShadowId))
            .ToListAsync(ct);

        var foundIds = found.Select(f => f.Id).ToHashSet();
        var notFound = itemIds
            .Where(id => !foundIds.Contains(id))
            .Select(id => new BudgetPlanItemAvailability(id, false, false, false, false, false, null));

        return [.. found, .. notFound];
    }

    public async Task LockBudgetPlanItemsAsync(List<long> budgetPlanItemIds, CancellationToken ct = default)
    {
        if (budgetPlanItemIds.Count == 0) return;

        await db.Database.SqlQuery<long>(
            $"""
            SELECT "Id" FROM budget_plan_items
            WHERE "Id" = ANY({budgetPlanItemIds.ToArray()})
            ORDER BY "Id"
            FOR UPDATE
            """).ToListAsync(ct);
    }

    public async Task<List<AvailableApItemResponse>> GetAvailableItemsByBudgetPlansAsync(
        long vendorShadowId,
        List<long> budgetPlanIds,
        bool includeGenerated = false,
        long? excludeDocumentId = null,
        List<long>? warehouseIds = null,
        CancellationToken ct = default
    )
    {
        var query = db.BudgetPlanItems
            .Where(bpi =>
                bpi.VendorShadowId == vendorShadowId &&
                bpi.BudgetPlan.DeletedAt == null &&
                (warehouseIds == null || warehouseIds.Contains(bpi.BudgetPlan.WarehouseShadowId)) &&
                db.RecapWorkOrders.Any(r =>
                    r.BudgetPlanId == bpi.BudgetPlanId &&
                    r.Status == RecapWorkOrderStatus.Approved))
            .Where(bpi => budgetPlanIds.Count == 0 || budgetPlanIds.Contains(bpi.BudgetPlanId));

        if (!includeGenerated)
        {
            query = query.Where(NotOnAnotherAccountPayable(excludeDocumentId));

            return await query
                .OrderBy(bpi => bpi.BudgetPlan.Code)
                .ThenBy(bpi => bpi.SortOrder)
                .Select(bpi => new AvailableApItemResponse(
                    bpi.Id,
                    bpi.BudgetPlanId,
                    bpi.BudgetPlan.Code,
                    bpi.BudgetPlan.Remark,
                    bpi.VendorShadowId,
                    bpi.Vendor.CardCode,
                    bpi.Vendor.CardName,
                    bpi.Item.ItemCode,
                    bpi.Item.ItemName,
                    bpi.Item.AcctCode,
                    bpi.Item.AcctName,
                    bpi.Uom.Code,
                    bpi.Uom.Name,
                    bpi.IsRfba,
                    bpi.BillOfLading,
                    bpi.CostValue,
                    bpi.Quantity,
                    bpi.TotalValue,
                    false,
                    // Always null here: NotOnAnotherAccountPayable already filtered out every
                    // taken item, so every row left is genuinely free. Project the constant
                    // instead of paying for a correlated subquery per row.
                    null,
                    "Available"))
                .ToListAsync(ct);
        }

        // includeGenerated=true: fetch all items (no exclusion filter), then resolve
        // isGenerated/takenByCode in batch lookups instead of N correlated subqueries per row.
        var items = await query
            .OrderBy(bpi => bpi.BudgetPlan.Code)
            .ThenBy(bpi => bpi.SortOrder)
            .Select(bpi => new
            {
                bpi.Id,
                bpi.BudgetPlanId,
                BudgetPlanCode = bpi.BudgetPlan.Code,
                BudgetPlanRemark = bpi.BudgetPlan.Remark,
                bpi.VendorShadowId,
                VendorCode = bpi.Vendor.CardCode,
                VendorName = bpi.Vendor.CardName,
                ItemCode = bpi.Item.ItemCode,
                ItemName = bpi.Item.ItemName,
                AcctCode = bpi.Item.AcctCode,
                AcctName = bpi.Item.AcctName,
                UomCode = bpi.Uom.Code,
                UomName = bpi.Uom.Name,
                bpi.IsRfba,
                bpi.BillOfLading,
                bpi.CostValue,
                bpi.Quantity,
                bpi.TotalValue,
            })
            .ToListAsync(ct);

        if (items.Count == 0) return [];

        var bpiIds = items.Select(x => x.Id).ToList();
        var generatedIds = (await db.AccountPayableItems
            .Where(api =>
                bpiIds.Contains(api.BudgetPlanItemId) &&
                api.AccountPayable.Status == AccountPayableStatus.Generated &&
                api.AccountPayable.DeletedAt == null)
            .Select(api => api.BudgetPlanItemId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();

        // Same batch pattern as generatedIds above, reusing the shared TakenByAnotherAccountPayable
        // definition so this can never disagree with the picker's !includeGenerated branch or the
        // diagnostics error-message path about who holds an item. excludeDocumentId is honoured by
        // the shared helper, so a Draft AP being edited sees its own attached items as free (null).
        var holderRows = await TakenByAnotherAccountPayable(excludeDocumentId)
            .Where(api => bpiIds.Contains(api.BudgetPlanItemId))
            .OrderByDescending(api => api.AccountPayableId)
            .Select(api => new { api.BudgetPlanItemId, api.AccountPayable.Code })
            .ToListAsync(ct);

        var takenByCode = new Dictionary<long, string>();
        foreach (var row in holderRows)
            takenByCode.TryAdd(row.BudgetPlanItemId, row.Code);

        return [.. items.Select(x => new AvailableApItemResponse(
            x.Id, x.BudgetPlanId, x.BudgetPlanCode, x.BudgetPlanRemark,
            x.VendorShadowId, x.VendorCode, x.VendorName, x.ItemCode, x.ItemName,
            x.AcctCode, x.AcctName, x.UomCode, x.UomName, x.IsRfba, x.BillOfLading,
            x.CostValue, x.Quantity, x.TotalValue, generatedIds.Contains(x.Id),
            takenByCode.GetValueOrDefault(x.Id),
            generatedIds.Contains(x.Id) ? "AlreadyGenerated" :
                (takenByCode.ContainsKey(x.Id) ? "TakenByDraft" : "Available")
        ))];
    }

    // THE single definition of "this budget plan item is already spoken for".
    // Used by AvailableItemsBaseQuery (the create/update validation path),
    // GetAvailableItemsByBudgetPlansAsync (the /available-items picker), AND
    // GetAvailabilityDiagnosticsAsync (the error-message path) so all three can
    // never drift apart and offer/report an item inconsistently.
    //
    // An item is taken once it appears on ANY non-deleted AP -- Draft included, regardless
    // of quantity. excludeDocumentId lets a Draft AP being edited see its own already-attached
    // items as available. TakenByAnotherAccountPayable is the document-level filter (no
    // BudgetPlanItem correlation yet); NotOnAnotherAccountPayable adds the correlation and
    // negates it for the boolean "is available" predicate, translating to a correlated
    // NOT EXISTS subquery. Diagnostics reuses the same document-level filter to instead
    // project the offending AP's code.
    private IQueryable<AccountPayableItem> TakenByAnotherAccountPayable(long? excludeDocumentId)
        => db.AccountPayableItems.Where(api =>
            api.AccountPayable.DeletedAt == null &&
            (excludeDocumentId == null || api.AccountPayableId != excludeDocumentId));

    private Expression<Func<BudgetPlanItem, bool>> NotOnAnotherAccountPayable(long? excludeDocumentId)
        => bpi => !TakenByAnotherAccountPayable(excludeDocumentId).Any(api => api.BudgetPlanItemId == bpi.Id);

    private IQueryable<BudgetPlanItem> AvailableItemsBaseQuery(
        long vendorShadowId, long? excludeDocumentId = null, List<long>? warehouseIds = null)
        => db.BudgetPlanItems
            .Where(bpi =>
                bpi.VendorShadowId == vendorShadowId &&
                bpi.BudgetPlan.DeletedAt == null &&
                (warehouseIds == null || warehouseIds.Contains(bpi.BudgetPlan.WarehouseShadowId)) &&
                db.RecapWorkOrders.Any(r =>
                    r.BudgetPlanId == bpi.BudgetPlanId &&
                    r.Status == RecapWorkOrderStatus.Approved))
            .Where(NotOnAnotherAccountPayable(excludeDocumentId))
            .Include(bpi => bpi.Item)
            .Include(bpi => bpi.Vendor)
            .Include(bpi => bpi.Uom)
            .Include(bpi => bpi.BudgetPlan)
                .ThenInclude(bp => bp.Warehouse)
            .Include(bpi => bpi.Spk)
            .OrderBy(bpi => bpi.BudgetPlan.Code)
            .ThenBy(bpi => bpi.SortOrder);

    public async Task<(List<ApprovedRecapApStatusResponse> Items, int Total)> GetApprovedRecapsWithApStatusAsync(
        long[]? warehouseIds,
        int page,
        int limit,
        CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;
        var warehouseFilterDisabled = warehouseIds is null;
        var warehouseIdsParam = warehouseIds ?? [];

        const string sql = """
            SELECT
                COUNT(*) OVER() AS total_count,
                r."Id"                  AS recap_id,
                bp."Id"                 AS budget_plan_id,
                bp.code                 AS budget_plan_code,
                bp.remark,
                bp.doc_date,
                EXISTS (
                    SELECT 1 FROM budget_plan_items bpi_rfba
                    WHERE bpi_rfba.budget_plan_id = bp."Id" AND bpi_rfba.is_rfba = TRUE
                ) AS has_rfba_items,
                vendors.vendor_name,
                ROUND(COALESCE(SUM(bpi.cost_value * bpi.quantity), 0::numeric), 2) AS budget_plan_total,
                COALESCE(
                    (SELECT json_agg(ap_link ORDER BY ap_link.id)
                     FROM (
                         SELECT DISTINCT ap."Id" AS id, ap.code, ap.status,
                                ap.sap_ap_number AS "sapApNumber", v_ap.card_code AS "vendorCode"
                         FROM account_payable_items api
                         JOIN account_payables ap ON ap."Id" = api.account_payable_id
                         JOIN budget_plan_items bpi4 ON bpi4."Id" = api.budget_plan_item_id
                         JOIN vendor_shadows v_ap ON v_ap."Id" = ap.vendor_shadow_id
                         WHERE bpi4.budget_plan_id = bp."Id"
                         AND ap.deleted_at IS NULL
                     ) ap_link),
                    '[]'::json
                ) AS account_payables,
                -- "all generated" means every budget plan item (any vendor) is covered by a
                -- Generated AP - NOT "every AP that happens to exist is Generated" (an item
                -- with zero AP at all must count as not-done, not vacuously done).
                NOT EXISTS (
                    SELECT 1
                    FROM budget_plan_items bpi_ungen
                    WHERE bpi_ungen.budget_plan_id = bp."Id"
                    AND NOT EXISTS (
                        SELECT 1
                        FROM account_payable_items api_gen
                        JOIN account_payables ap_gen ON ap_gen."Id" = api_gen.account_payable_id
                        WHERE api_gen.budget_plan_item_id = bpi_ungen."Id"
                        AND ap_gen.status = 'Generated'
                        AND ap_gen.deleted_at IS NULL
                    )
                ) AS is_all_generated,
                ws.location,
                ROUND(COALESCE((
                    SELECT SUM(api_s.budget_plan_total)
                    FROM account_payable_items api_s
                    JOIN account_payables ap_s ON ap_s."Id" = api_s.account_payable_id
                    JOIN budget_plan_items bpi_s ON bpi_s."Id" = api_s.budget_plan_item_id
                    WHERE bpi_s.budget_plan_id = bp."Id"
                    AND ap_s.deleted_at IS NULL
                ), 0::numeric), 2) AS budget_approved
            FROM recap_work_orders r
            JOIN budget_plans bp ON bp."Id" = r.budget_plan_id
            JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
            LEFT JOIN budget_plan_items bpi ON bpi.budget_plan_id = bp."Id"
            LEFT JOIN LATERAL (
                SELECT STRING_AGG(DISTINCT v.card_name, ', ' ORDER BY v.card_name) AS vendor_name
                FROM budget_plan_items bpi2
                JOIN vendor_shadows v ON v."Id" = bpi2.vendor_shadow_id
                WHERE bpi2.budget_plan_id = bp."Id"
            ) vendors ON TRUE
            WHERE r.status = 'Approved'
              AND bp.deleted_at IS NULL
              AND (@p_tenant_filter_disabled OR r.company_id = @p_company_id)
              AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
            GROUP BY r."Id", bp."Id", bp.code, bp.remark, bp.doc_date,
                     vendors.vendor_name,
                     ws.location
            ORDER BY r.created_at DESC NULLS LAST, r."Id" DESC
            LIMIT @p_limit OFFSET @p_offset;
            """;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsParam });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = limit });
        cmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = (page - 1) * limit });

        var result = new List<ApprovedRecapApStatusResponse>();
        var totalCount = 0;
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colTotalCount = reader.GetOrdinal("total_count");
        var colRecapId = reader.GetOrdinal("recap_id");
        var colBpId = reader.GetOrdinal("budget_plan_id");
        var colBpCode = reader.GetOrdinal("budget_plan_code");
        var colRemark = reader.GetOrdinal("remark");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colHasRfba = reader.GetOrdinal("has_rfba_items");
        var colVendorName = reader.GetOrdinal("vendor_name");
        var colBudgetPlanTotal = reader.GetOrdinal("budget_plan_total");
        var colAccountPayables = reader.GetOrdinal("account_payables");
        var colIsAllGenerated = reader.GetOrdinal("is_all_generated");
        var colLocation = reader.GetOrdinal("location");
        var colBudgetApproved = reader.GetOrdinal("budget_approved");

        while (await reader.ReadAsync(ct))
        {
            totalCount = reader.GetInt32(colTotalCount);
            var budgetPlanTotal = reader.GetDecimal(colBudgetPlanTotal);
            var budgetApproved = reader.GetDecimal(colBudgetApproved);
            var accountPayables = JsonSerializer.Deserialize<List<ApLinkInfo>>(
                reader.GetString(colAccountPayables), CaseInsensitiveJsonOpts) ?? [];
            result.Add(new ApprovedRecapApStatusResponse(
                reader.GetInt64(colRecapId),
                reader.GetInt64(colBpId),
                reader.GetString(colBpCode),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.GetDateTime(colDocDate),
                reader.GetBoolean(colHasRfba),
                null,
                null,
                reader.IsDBNull(colVendorName) ? null : reader.GetString(colVendorName),
                budgetPlanTotal,
                accountPayables,
                reader.GetBoolean(colIsAllGenerated),
                reader.IsDBNull(colLocation) ? null : reader.GetString(colLocation),
                budgetApproved,
                budgetPlanTotal - budgetApproved));
        }

        return (result, totalCount);
    }

    public Task CreateAsync(AccountPayable ap, CancellationToken ct = default)
    {
        db.AccountPayables.Add(ap);
        return Task.CompletedTask;
    }

    public async Task<bool> MarkGeneratedAsync(
        long id,
        string claimToken,
        string sapApNumber,
        int? sapDocEntry,
        int? sapApdpDocEntry,
        long generatedByUserId,
        CancellationToken ct = default)
    {
        var rows = await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE account_payables
            SET status = 'Generated',
                sap_ap_number = {sapApNumber},
                sap_doc_entry = {sapDocEntry},
                sap_apdp_doc_entry = {sapApdpDocEntry},
                generated_by_user_id = {generatedByUserId},
                generated_at = NOW(),
                generation_claimed_at = NULL,
                generation_claim_token = NULL,
                updated_at = NOW()
            WHERE "Id" = {id}
              AND status = 'Draft'
              AND generation_claim_token = {claimToken}
              AND deleted_at IS NULL
            """, ct);
        return rows == 1;
    }

    public async Task<bool> TryClaimForGenerationAsync(long id, string claimToken, CancellationToken ct = default)
    {
        var rows = await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE account_payables
            SET generation_claimed_at = NOW(), generation_claim_token = {claimToken}
            WHERE "Id" = {id}
              AND status = 'Draft'
              AND deleted_at IS NULL
              AND (generation_claimed_at IS NULL OR generation_claimed_at < NOW() - INTERVAL '15 minutes')
            """, ct);
        return rows == 1;
    }

    public async Task ReleaseGenerationClaimAsync(long id, string claimToken, CancellationToken ct = default)
        => await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE account_payables
            SET generation_claimed_at = NULL, generation_claim_token = NULL
            WHERE "Id" = {id} AND generation_claim_token = {claimToken}
            """, ct);

    public async Task<bool> LockForEditAsync(long id, CancellationToken ct = default)
        => (await db.Database.SqlQuery<long>($"""
            SELECT "Id" AS "Value" FROM account_payables
            WHERE "Id" = {id} AND deleted_at IS NULL
              AND (generation_claimed_at IS NULL OR generation_claimed_at < NOW() - INTERVAL '15 minutes')
            FOR UPDATE
            """).ToListAsync(ct)).Count == 1;

    public async Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default)
    {
        var rows = await db.AccountPayables
            .Where(a => a.Id == id)
            .Where(a => a.GenerationClaimedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, DateTime.UtcNow), ct);
        return rows == 1;
    }
}
