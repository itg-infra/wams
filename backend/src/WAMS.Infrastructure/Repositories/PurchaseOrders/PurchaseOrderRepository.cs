namespace WAMS.Infrastructure.Repositories.PurchaseOrders;

using System.Data;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WAMS.Application.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.PurchaseOrders;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class PurchaseOrderRepository(
    AppDbContext db,
    ITenantContext tenantContext) : IPurchaseOrderRepository
{
    private const string DefaultOrderBy = "po.created_at DESC NULLS LAST, po.\"Id\" DESC";
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOpts =
        new() { PropertyNameCaseInsensitive = true };
    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = "po.status",
            ["docdate"] = "po.doc_date",
            ["createdat"] = "po.created_at",
        };

    public async Task<(List<PurchaseOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(
        PurchaseOrderQuery q,
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
                po.""Id"",
                po.code,
                v.card_code,
                v.card_name,
                po.status,
                po.doc_date,
                po.remark,
                po.sap_po_number,
                COALESCE(SUM(poi.total_value), 0) AS grand_total,
                COUNT(poi.""Id"")::int AS item_count,
                po.created_at,
                cu.""Fullname"" AS created_by_name,
                COUNT(*) OVER() AS total_count
            FROM purchase_orders po
            JOIN vendor_shadows v ON v.""Id"" = po.vendor_shadow_id
            LEFT JOIN users cu ON cu.""Id"" = po.created_by_user_id
            LEFT JOIN purchase_order_items poi ON poi.purchase_order_id = po.""Id""
            WHERE po.deleted_at IS NULL
              AND (@p_tenant_filter_disabled OR po.company_id = @p_company_id)
              AND (@p_status IS NULL OR po.status = @p_status)
              AND (@p_vendor_id IS NULL OR po.vendor_shadow_id = @p_vendor_id)
              AND (@p_search IS NULL
                   OR po.code ILIKE @p_search_pattern
                   OR v.card_name ILIKE @p_search_pattern
                   OR COALESCE(po.remark, '') ILIKE @p_search_pattern)
              AND (@p_date_from IS NULL OR po.doc_date >= @p_date_from)
              AND (@p_date_to IS NULL OR po.doc_date < @p_date_to)
            GROUP BY po.""Id"", v.card_code, v.card_name, cu.""Fullname""
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

        var items = new List<PurchaseOrderSummaryResponse>();
        var total = 0;

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colCode = reader.GetOrdinal("code");
        var colVendorCode = reader.GetOrdinal("card_code");
        var colVendorName = reader.GetOrdinal("card_name");
        var colStatus = reader.GetOrdinal("status");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colRemark = reader.GetOrdinal("remark");
        var colSapPoNumber = reader.GetOrdinal("sap_po_number");
        var colGrandTotal = reader.GetOrdinal("grand_total");
        var colItemCount = reader.GetOrdinal("item_count");
        var colCreatedAt = reader.GetOrdinal("created_at");
        var colCreatedByName = reader.GetOrdinal("created_by_name");
        var colTotalCount = reader.GetOrdinal("total_count");

        while (await reader.ReadAsync(ct))
        {
            total = reader.GetInt32(colTotalCount);
            items.Add(new PurchaseOrderSummaryResponse(
                reader.GetInt64(colId),
                reader.GetString(colCode),
                reader.GetString(colVendorCode),
                reader.GetString(colVendorName),
                reader.GetString(colStatus),
                reader.GetDateTime(colDocDate),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.IsDBNull(colSapPoNumber) ? null : reader.GetString(colSapPoNumber),
                reader.GetDecimal(colGrandTotal),
                reader.GetInt32(colItemCount),
                reader.GetDateTime(colCreatedAt),
                reader.IsDBNull(colCreatedByName) ? "" : reader.GetString(colCreatedByName)));
        }

        return (items, total);
    }

    public async IAsyncEnumerable<PurchaseOrderSummaryResponse> StreamAllAsync(
        PurchaseOrderQuery q,
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
                po.""Id"",
                po.code,
                v.card_code,
                v.card_name,
                po.status,
                po.doc_date,
                po.remark,
                po.sap_po_number,
                COALESCE(SUM(poi.total_value), 0) AS grand_total,
                COUNT(poi.""Id"")::int AS item_count,
                po.created_at,
                cu.""Fullname"" AS created_by_name
            FROM purchase_orders po
            JOIN vendor_shadows v ON v.""Id"" = po.vendor_shadow_id
            LEFT JOIN users cu ON cu.""Id"" = po.created_by_user_id
            LEFT JOIN purchase_order_items poi ON poi.purchase_order_id = po.""Id""
            WHERE po.deleted_at IS NULL
              AND (@p_tenant_filter_disabled OR po.company_id = @p_company_id)
              AND (@p_status IS NULL OR po.status = @p_status)
              AND (@p_vendor_id IS NULL OR po.vendor_shadow_id = @p_vendor_id)
              AND (@p_search IS NULL
                   OR po.code ILIKE @p_search_pattern
                   OR v.card_name ILIKE @p_search_pattern
                   OR COALESCE(po.remark, '') ILIKE @p_search_pattern)
              AND (@p_date_from IS NULL OR po.doc_date >= @p_date_from)
              AND (@p_date_to IS NULL OR po.doc_date < @p_date_to)
            GROUP BY po.""Id"", v.card_code, v.card_name, cu.""Fullname""
            ORDER BY {orderBy}
            LIMIT @p_limit;";

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
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
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = limit });

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colCode = reader.GetOrdinal("code");
        var colVendorCode = reader.GetOrdinal("card_code");
        var colVendorName = reader.GetOrdinal("card_name");
        var colStatus = reader.GetOrdinal("status");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colRemark = reader.GetOrdinal("remark");
        var colSapPoNumber = reader.GetOrdinal("sap_po_number");
        var colGrandTotal = reader.GetOrdinal("grand_total");
        var colItemCount = reader.GetOrdinal("item_count");
        var colCreatedAt = reader.GetOrdinal("created_at");
        var colCreatedByName = reader.GetOrdinal("created_by_name");

        while (await reader.ReadAsync(ct))
        {
            yield return new PurchaseOrderSummaryResponse(
                reader.GetInt64(colId),
                reader.GetString(colCode),
                reader.GetString(colVendorCode),
                reader.GetString(colVendorName),
                reader.GetString(colStatus),
                reader.GetDateTime(colDocDate),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.IsDBNull(colSapPoNumber) ? null : reader.GetString(colSapPoNumber),
                reader.GetDecimal(colGrandTotal),
                reader.GetInt32(colItemCount),
                reader.GetDateTime(colCreatedAt),
                reader.IsDBNull(colCreatedByName) ? "" : reader.GetString(colCreatedByName));
        }
    }

    public async Task<Dictionary<long, (int SapDocEntry, int LineIndex, int? SapApdpDocEntry)>> GetGeneratedPoLineRefsAsync(
        List<long> budgetPlanItemIds,
        CancellationToken ct = default
    )
    {
        var rows = await db.PurchaseOrderItems
            .Where(poi =>
                budgetPlanItemIds.Contains(poi.BudgetPlanItemId) &&
                poi.PurchaseOrder.Status == PurchaseOrderStatus.Generated &&
                poi.PurchaseOrder.SapDocEntry != null)
            .Select(poi => new
            {
                poi.BudgetPlanItemId,
                SapDocEntry = poi.PurchaseOrder.SapDocEntry!.Value,
                LineIndex = poi.SortOrder - 1,
                SapApdpDocEntry = poi.PurchaseOrder.SapApdpDocEntry,
            })
            .ToListAsync(ct);

        // A budget item can end up on two Generated POs (creation-time check doesn't cover
        // concurrent Draft POs); keep the most recently generated one instead of crashing.
        return rows
            .GroupBy(x => x.BudgetPlanItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.SapDocEntry)
                    .Select(x => (x.SapDocEntry, x.LineIndex, x.SapApdpDocEntry)).First());
    }

    public async Task<PurchaseOrder?> GetByIdWithItemsAsync(long id, CancellationToken ct = default)
        => await db.PurchaseOrders
            .Include(p => p.Company)
            .Include(p => p.Vendor)
            .Include(p => p.CreatedBy)
            .Include(p => p.GeneratedBy)
            .Include(p => p.Items)
                .ThenInclude(i => i.BudgetPlanItem)
                    .ThenInclude(bpi => bpi.BudgetPlan)
                        .ThenInclude(bp => bp.Warehouse)
            .Include(p => p.Items)
                .ThenInclude(i => i.BudgetPlanItem)
                    .ThenInclude(bpi => bpi.BudgetPlan)
                        .ThenInclude(bp => bp.WorkflowInstance)
                            .ThenInclude(wi => wi!.Stages)
                                .ThenInclude(s => s.ApprovedBy)
            .Include(p => p.Items)
                .ThenInclude(i => i.BudgetPlanItem)
                    .ThenInclude(bpi => bpi.Spk)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<(long BudgetPlanId, long PoId, string PoCode)>> GetPoSummariesByBudgetPlanIdsAsync(
        List<long> budgetPlanIds,
        long excludePoId,
        CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;

        var rows = await db.PurchaseOrderItems
            .Where(poi =>
                budgetPlanIds.Contains(poi.BudgetPlanItem.BudgetPlanId) &&
                poi.PurchaseOrderId != excludePoId &&
                poi.PurchaseOrder.DeletedAt == null &&
                (tenantFilterDisabled || (long?)poi.PurchaseOrder.CompanyId == tenantCompanyId))
            .Select(poi => new
            {
                poi.BudgetPlanItem.BudgetPlanId,
                PoId = poi.PurchaseOrderId,
                PoCode = poi.PurchaseOrder.Code
            })
            .Distinct()
            .ToListAsync(ct);

        return [.. rows.Select(x => (x.BudgetPlanId, x.PoId, x.PoCode))];
    }

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
    // the specific reason (not found / vendor mismatch / plan not approved / already generated /
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
                bpi.BudgetPlan.Status == BudgetPlanStatus.Approved,
                db.PurchaseOrderItems.Any(poi =>
                    poi.BudgetPlanItemId == bpi.Id &&
                    poi.PurchaseOrder.Status == PurchaseOrderStatus.Generated &&
                    poi.PurchaseOrder.DeletedAt == null),
                TakenByAnotherPurchaseOrder(null)
                    .Where(poi => poi.BudgetPlanItemId == bpi.Id)
                    .OrderByDescending(poi => poi.PurchaseOrderId)
                    .Select(poi => poi.PurchaseOrder.Code)
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

    public async Task<(List<AvailablePoItemResponse> Items, int Total)> GetAvailableItemsForPickerAsync(
        IReadOnlyCollection<long> vendorShadowIds,
        long? seedBudgetPlanId,
        DataTableQuery query,
        bool includeGenerated = false,
        long? excludeDocumentId = null,
        List<long>? warehouseIds = null,
        CancellationToken ct = default
    )
    {
        var candidates = db.BudgetPlanItems
            .Where(bpi =>
                vendorShadowIds.Contains(bpi.VendorShadowId) &&
                bpi.BudgetPlan.Status == BudgetPlanStatus.Approved &&
                bpi.BudgetPlan.DeletedAt == null &&
                (warehouseIds == null || warehouseIds.Contains(bpi.BudgetPlan.WarehouseShadowId)));

        if (!includeGenerated)
            candidates = candidates.Where(NotOnAnotherPurchaseOrder(excludeDocumentId));

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        if (search is not null)
        {
            var pattern = LikePatternHelper.ToContainsPattern(search);

            // Production uses PostgreSQL's case-insensitive ILIKE. Repository tests use
            // SQLite, whose LIKE is case-insensitive for ASCII by default, so keep the same
            // user-facing behavior without asking SQLite to translate an Npgsql extension.
            if (db.Database.IsNpgsql())
            {
                candidates = candidates.Where(bpi =>
                    EF.Functions.ILike(bpi.BudgetPlan.Code, pattern, "\\") ||
                    (bpi.BudgetPlan.Remark != null && EF.Functions.ILike(bpi.BudgetPlan.Remark, pattern, "\\")) ||
                    EF.Functions.ILike(bpi.BudgetPlan.Warehouse.Code, pattern, "\\") ||
                    EF.Functions.ILike(bpi.BudgetPlan.Warehouse.Name, pattern, "\\") ||
                    EF.Functions.ILike(bpi.Vendor.CardCode, pattern, "\\") ||
                    EF.Functions.ILike(bpi.Vendor.CardName, pattern, "\\") ||
                    EF.Functions.ILike(bpi.Item.ItemCode, pattern, "\\") ||
                    EF.Functions.ILike(bpi.Item.ItemName, pattern, "\\") ||
                    (bpi.BillOfLading != null && EF.Functions.ILike(bpi.BillOfLading, pattern, "\\")));
            }
            else
            {
                candidates = candidates.Where(bpi =>
                    EF.Functions.Like(bpi.BudgetPlan.Code, pattern, "\\") ||
                    (bpi.BudgetPlan.Remark != null && EF.Functions.Like(bpi.BudgetPlan.Remark, pattern, "\\")) ||
                    EF.Functions.Like(bpi.BudgetPlan.Warehouse.Code, pattern, "\\") ||
                    EF.Functions.Like(bpi.BudgetPlan.Warehouse.Name, pattern, "\\") ||
                    EF.Functions.Like(bpi.Vendor.CardCode, pattern, "\\") ||
                    EF.Functions.Like(bpi.Vendor.CardName, pattern, "\\") ||
                    EF.Functions.Like(bpi.Item.ItemCode, pattern, "\\") ||
                    EF.Functions.Like(bpi.Item.ItemName, pattern, "\\") ||
                    (bpi.BillOfLading != null && EF.Functions.Like(bpi.BillOfLading, pattern, "\\")));
            }
        }

        var total = await candidates.CountAsync(ct);
        var offset = (query.Page - 1) * query.Limit;

        var items = await candidates
            .OrderByDescending(bpi => seedBudgetPlanId.HasValue && bpi.BudgetPlanId == seedBudgetPlanId.Value)
            .ThenBy(bpi => bpi.BudgetPlan.Warehouse.Name)
            .ThenBy(bpi => bpi.BudgetPlan.Code)
            .ThenBy(bpi => bpi.SortOrder)
            .Select(bpi => new
            {
                bpi.Id,
                bpi.BudgetPlanId,
                BudgetPlanCode = bpi.BudgetPlan.Code,
                BudgetPlanRemark = bpi.BudgetPlan.Remark,
                BudgetPlanDocDate = bpi.BudgetPlan.DocDate,
                IsSeedBudgetPlan = seedBudgetPlanId.HasValue && bpi.BudgetPlanId == seedBudgetPlanId.Value,
                WarehouseShadowId = bpi.BudgetPlan.WarehouseShadowId,
                WarehouseCode = bpi.BudgetPlan.Warehouse.Code,
                WarehouseName = bpi.BudgetPlan.Warehouse.Name,
                bpi.VendorShadowId,
                VendorCode = bpi.Vendor.CardCode,
                VendorName = bpi.Vendor.CardName,
                bpi.ItemShadowId,
                ItemCode = bpi.Item.ItemCode,
                ItemName = bpi.Item.ItemName,
                CoaCode = bpi.Item.AcctCode,
                CoaName = bpi.Item.AcctName,
                bpi.IsRfba,
                bpi.BillOfLading,
                bpi.CostValue,
                bpi.Quantity,
                UomCode = bpi.Uom.Code,
                UomName = bpi.Uom.Name,
            })
            .Skip(offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        if (items.Count == 0) return ([], total);

        if (!includeGenerated)
        {
            return ([.. items.Select(x => new AvailablePoItemResponse(
                x.Id,
                x.BudgetPlanId,
                x.BudgetPlanCode,
                x.BudgetPlanRemark,
                x.BudgetPlanDocDate,
                x.IsSeedBudgetPlan,
                x.WarehouseShadowId,
                x.WarehouseCode,
                x.WarehouseName,
                x.VendorShadowId,
                x.VendorCode,
                x.VendorName,
                x.ItemShadowId,
                x.ItemCode,
                x.ItemName,
                x.CoaCode,
                x.CoaName,
                x.IsRfba,
                x.BillOfLading,
                x.CostValue,
                x.Quantity,
                x.UomCode,
                x.UomName,
                false,
                null,
                "Available"))], total);
        }

        var bpiIds = items.Select(x => x.Id).ToList();
        var generatedIds = (await db.PurchaseOrderItems
            .Where(poi =>
                bpiIds.Contains(poi.BudgetPlanItemId) &&
                poi.PurchaseOrder.Status == PurchaseOrderStatus.Generated &&
                poi.PurchaseOrder.DeletedAt == null)
            .Select(poi => poi.BudgetPlanItemId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();

        // Same batch pattern as generatedIds above, reusing the shared TakenByAnotherPurchaseOrder
        // definition so this can never disagree with the picker's !includeGenerated branch or the
        // diagnostics error-message path about who holds an item. excludeDocumentId is honoured by
        // the shared helper, so a Draft PO being edited sees its own attached items as free (null).
        var holderRows = await TakenByAnotherPurchaseOrder(excludeDocumentId)
            .Where(poi => bpiIds.Contains(poi.BudgetPlanItemId))
            .OrderByDescending(poi => poi.PurchaseOrderId)
            .Select(poi => new { poi.BudgetPlanItemId, poi.PurchaseOrder.Code })
            .ToListAsync(ct);

        var takenByCode = new Dictionary<long, string>();
        foreach (var row in holderRows)
            takenByCode.TryAdd(row.BudgetPlanItemId, row.Code);

        return ([.. items.Select(x => new AvailablePoItemResponse(
            x.Id,
            x.BudgetPlanId,
            x.BudgetPlanCode,
            x.BudgetPlanRemark,
            x.BudgetPlanDocDate,
            x.IsSeedBudgetPlan,
            x.WarehouseShadowId,
            x.WarehouseCode,
            x.WarehouseName,
            x.VendorShadowId,
            x.VendorCode,
            x.VendorName,
            x.ItemShadowId,
            x.ItemCode,
            x.ItemName,
            x.CoaCode,
            x.CoaName,
            x.IsRfba,
            x.BillOfLading,
            x.CostValue,
            x.Quantity,
            x.UomCode,
            x.UomName,
            generatedIds.Contains(x.Id),
            takenByCode.GetValueOrDefault(x.Id),
            generatedIds.Contains(x.Id) ? "AlreadyGenerated" :
                (takenByCode.ContainsKey(x.Id) ? "TakenByDraft" : "Available")))], total);
    }

    // THE single definition of "this budget plan item is already spoken for".
    // Used by AvailableItemsBaseQuery (the create/update validation path),
    // GetAvailableItemsForPickerAsync (the /available-items picker), AND
    // GetAvailabilityDiagnosticsAsync (the error-message path) so all three can
    // never drift apart and offer/report an item inconsistently.
    //
    // An item is taken once it appears on ANY non-deleted PO -- Draft included, regardless
    // of quantity. excludeDocumentId lets a Draft PO being edited see its own already-attached
    // items as available. TakenByAnotherPurchaseOrder is the document-level filter (no
    // BudgetPlanItem correlation yet); NotOnAnotherPurchaseOrder adds the correlation and
    // negates it for the boolean "is available" predicate, translating to a correlated
    // NOT EXISTS subquery. Diagnostics reuses the same document-level filter to instead
    // project the offending PO's code.
    private IQueryable<PurchaseOrderItem> TakenByAnotherPurchaseOrder(long? excludeDocumentId)
        => db.PurchaseOrderItems.Where(poi =>
            poi.PurchaseOrder.DeletedAt == null &&
            (excludeDocumentId == null || poi.PurchaseOrderId != excludeDocumentId));

    private Expression<Func<BudgetPlanItem, bool>> NotOnAnotherPurchaseOrder(long? excludeDocumentId)
        => bpi => !TakenByAnotherPurchaseOrder(excludeDocumentId).Any(poi => poi.BudgetPlanItemId == bpi.Id);

    private IQueryable<BudgetPlanItem> AvailableItemsBaseQuery(
        long vendorShadowId, long? excludeDocumentId = null, List<long>? warehouseIds = null)
        => db.BudgetPlanItems
            .Where(bpi =>
                bpi.VendorShadowId == vendorShadowId &&
                bpi.BudgetPlan.Status == BudgetPlanStatus.Approved &&
                bpi.BudgetPlan.DeletedAt == null &&
                (warehouseIds == null || warehouseIds.Contains(bpi.BudgetPlan.WarehouseShadowId)))
            .Where(NotOnAnotherPurchaseOrder(excludeDocumentId))
            .Include(bpi => bpi.Item)
            .Include(bpi => bpi.Vendor)
            .Include(bpi => bpi.Uom)
            .Include(bpi => bpi.BudgetPlan)
            .OrderBy(bpi => bpi.BudgetPlan.Code)
            .ThenBy(bpi => bpi.SortOrder);

    private static readonly IReadOnlyDictionary<string, string> ApprovedBudgetPlanSortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["docdate"] = "doc_date",
            ["budgetplancode"] = "budget_plan_code",
            ["vendorname"] = "vendor_name",
            ["totalbudgetplan"] = "total_budget_plan",
            ["budgetapproved"] = "budget_approved",
            ["budgetvariance"] = "(total_budget_plan - budget_approved)",
            ["ponumber"] = "po_number",
        };

    public async Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetApprovedBudgetPlansWithPoStatusAsync(
        long[]? warehouseIds,
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;
        var warehouseFilterDisabled = warehouseIds is null;
        var warehouseIdsParam = warehouseIds ?? [];
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);

        var orderBy = ApprovedBudgetPlanSortColumns.TryGetValue(query.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : "created_at DESC NULLS LAST, budget_plan_id DESC";

        var sql = $"""
            WITH report AS (
                SELECT
                    COUNT(*) OVER() AS total_count,
                    bp."Id" AS budget_plan_id,
                    bp.code AS budget_plan_code,
                    bp.remark,
                    bp.doc_date,
                    bp.created_at,
                    bp.status AS budget_plan_status,
                    EXISTS (
                        SELECT 1
                        FROM budget_plan_items bpi_rfba
                        WHERE bpi_rfba.budget_plan_id = bp."Id"
                        AND bpi_rfba.is_rfba = TRUE
                    ) AS has_rfba_items,
                    (
                        EXISTS (SELECT 1 FROM budget_plan_items bpi_sub WHERE bpi_sub.budget_plan_id = bp."Id")
                        AND NOT EXISTS (
                            SELECT 1
                            FROM budget_plan_items bpi_sub
                            WHERE bpi_sub.budget_plan_id = bp."Id"
                              AND NOT EXISTS (
                                  SELECT 1
                                  FROM purchase_order_items poi_sub
                                  JOIN purchase_orders po_sub ON po_sub."Id" = poi_sub.purchase_order_id
                                  WHERE poi_sub.budget_plan_item_id = bpi_sub."Id"
                                    AND po_sub.status = 'Generated'
                                    AND po_sub.deleted_at IS NULL
                              )
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM purchase_order_items poi_status
                            JOIN purchase_orders po_status ON po_status."Id" = poi_status.purchase_order_id
                            JOIN budget_plan_items bpi_status ON bpi_status."Id" = poi_status.budget_plan_item_id
                            WHERE bpi_status.budget_plan_id = bp."Id"
                              AND po_status.deleted_at IS NULL
                              AND po_status.status <> 'Generated'
                        )
                    ) AS all_generated,
                    vendors.vendor_name,
                    u_sub."Fullname" AS maker_name,
                    u_approver."Fullname" AS approval_name,
                    COALESCE(
                        (SELECT json_agg(po_sub ORDER BY po_sub.id)
                         FROM (
                             SELECT DISTINCT po."Id" AS id, po.code
                             FROM purchase_order_items poi
                             JOIN purchase_orders po ON po."Id" = poi.purchase_order_id
                             JOIN budget_plan_items bpi2 ON bpi2."Id" = poi.budget_plan_item_id
                             WHERE bpi2.budget_plan_id = bp."Id"
                               AND po.deleted_at IS NULL
                               AND (@p_tenant_filter_disabled OR po.company_id = @p_company_id)
                         ) po_sub),
                        '[]'::json
                    ) AS purchase_orders,
                    (SELECT STRING_AGG(DISTINCT po_num.code, ', ' ORDER BY po_num.code)
                     FROM purchase_order_items poi_num
                     JOIN purchase_orders po_num ON po_num."Id" = poi_num.purchase_order_id
                     JOIN budget_plan_items bpi_num ON bpi_num."Id" = poi_num.budget_plan_item_id
                     WHERE bpi_num.budget_plan_id = bp."Id"
                       AND po_num.deleted_at IS NULL
                       AND (@p_tenant_filter_disabled OR po_num.company_id = @p_company_id)
                    ) AS po_number,
                    ws.location,
                    COALESCE(SUM(bpi_total.cost_value * bpi_total.quantity), 0) AS total_budget_plan,
                    COALESCE((
                        SELECT SUM(poi_s.cost_value * poi_s.quantity)
                        FROM purchase_order_items poi_s
                        JOIN purchase_orders po_s ON po_s."Id" = poi_s.purchase_order_id
                        JOIN budget_plan_items bpi_s ON bpi_s."Id" = poi_s.budget_plan_item_id
                        WHERE bpi_s.budget_plan_id = bp."Id"
                        AND po_s.deleted_at IS NULL
                        AND (@p_tenant_filter_disabled OR po_s.company_id = @p_company_id)
                    ), 0) AS budget_approved
                FROM budget_plans bp
                JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
                LEFT JOIN budget_plan_items bpi_total ON bpi_total.budget_plan_id = bp."Id"
                LEFT JOIN LATERAL (
                    SELECT STRING_AGG(DISTINCT v.card_name, ', ' ORDER BY v.card_name) AS vendor_name
                    FROM budget_plan_items bpi_v
                    JOIN vendor_shadows v ON v."Id" = bpi_v.vendor_shadow_id
                    WHERE bpi_v.budget_plan_id = bp."Id"
                ) vendors ON TRUE
                LEFT JOIN users u_sub ON u_sub."Id" = bp.submitted_by_user_id
                LEFT JOIN workflow_instances wi_ap ON wi_ap.doc_id = bp."Id" AND wi_ap.doc_type = 'BudgetPlanApproval'
                LEFT JOIN LATERAL (
                    SELECT wis.approved_by_user_id
                    FROM workflow_instance_stages wis
                    WHERE wis.workflow_instance_id = wi_ap."Id"
                      AND wis.status = 'Approved'
                    ORDER BY wis.stage_order DESC
                    LIMIT 1
                ) last_stage ON TRUE
                LEFT JOIN users u_approver ON u_approver."Id" = last_stage.approved_by_user_id
                WHERE bp.status = 'Approved'
                AND bp.deleted_at IS NULL
                AND (@p_tenant_filter_disabled OR bp.company_id = @p_company_id)
                AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
                AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern OR EXISTS (
                    SELECT 1
                    FROM budget_plan_items bpi_search
                    JOIN vendor_shadows v_search ON v_search."Id" = bpi_search.vendor_shadow_id
                    WHERE bpi_search.budget_plan_id = bp."Id"
                    AND v_search.card_name ILIKE @p_search_pattern
                ))
                GROUP BY bp."Id", bp.code, bp.remark, bp.doc_date, bp.status, bp.created_at,
                         u_sub."Fullname", u_approver."Fullname",
                         vendors.vendor_name,
                         ws.location
            )
            SELECT * FROM report
            ORDER BY {orderBy}
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
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = query.Limit });
        cmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = (query.Page - 1) * query.Limit });

        var result = new List<ApprovedBudgetPlanPoStatusResponse>();
        var totalCount = 0;
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colTotalCount = reader.GetOrdinal("total_count");
        var colBudgetPlanId = reader.GetOrdinal("budget_plan_id");
        var colBudgetPlanCode = reader.GetOrdinal("budget_plan_code");
        var colRemark = reader.GetOrdinal("remark");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colBudgetPlanStatus = reader.GetOrdinal("budget_plan_status");
        var colHasRfbaItems = reader.GetOrdinal("has_rfba_items");
        var colAllGenerated = reader.GetOrdinal("all_generated");
        var colVendorName = reader.GetOrdinal("vendor_name");
        var colMakerName = reader.GetOrdinal("maker_name");
        var colApprovalName = reader.GetOrdinal("approval_name");
        var colPurchaseOrders = reader.GetOrdinal("purchase_orders");
        var colLocation = reader.GetOrdinal("location");
        var colTotalBudgetPlan = reader.GetOrdinal("total_budget_plan");
        var colBudgetApproved = reader.GetOrdinal("budget_approved");

        while (await reader.ReadAsync(ct))
        {
            totalCount = reader.GetInt32(colTotalCount);
            var bpStatusStr = reader.GetString(colBudgetPlanStatus);
            var bpStatusDisplay = BudgetPlanStatus.TryFromValue(bpStatusStr, out var bps) ? bps.DisplayName : bpStatusStr;
            var totalBudgetPlan = reader.GetDecimal(colTotalBudgetPlan);
            var budgetApproved = reader.GetDecimal(colBudgetApproved);
            var purchaseOrders = JsonSerializer.Deserialize<List<PoLinkInfo>>(
                reader.GetString(colPurchaseOrders), CaseInsensitiveJsonOpts) ?? [];
            result.Add(new ApprovedBudgetPlanPoStatusResponse(
                reader.GetInt64(colBudgetPlanId),
                reader.GetString(colBudgetPlanCode),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.GetDateTime(colDocDate),
                bpStatusStr,
                bpStatusDisplay,
                reader.GetBoolean(colHasRfbaItems),
                null,
                null,
                reader.IsDBNull(colVendorName) ? null : reader.GetString(colVendorName),
                reader.IsDBNull(colMakerName) ? null : reader.GetString(colMakerName),
                reader.IsDBNull(colApprovalName) ? null : reader.GetString(colApprovalName),
                purchaseOrders,
                reader.IsDBNull(colLocation) ? null : reader.GetString(colLocation),
                totalBudgetPlan,
                budgetApproved,
                totalBudgetPlan - budgetApproved,
                reader.GetBoolean(colAllGenerated)));
        }

        return (result, totalCount);
    }

    public async Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetRecapPurchaseOrdersAsync(
        bool isRfba,
        long[]? warehouseIds,
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;
        var warehouseFilterDisabled = warehouseIds is null;
        var warehouseIdsParam = warehouseIds ?? [];
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);

        var orderBy = ApprovedBudgetPlanSortColumns.TryGetValue(query.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : "created_at DESC NULLS LAST, budget_plan_id DESC";

        var sql = $"""
            WITH report AS (
                SELECT
                    COUNT(*) OVER() AS total_count,
                    bp."Id" AS budget_plan_id,
                    bp.code AS budget_plan_code,
                    bp.remark,
                    bp.doc_date,
                    bp.created_at,
                    bp.status AS budget_plan_status,
                    EXISTS (
                        SELECT 1
                        FROM budget_plan_items bpi_rfba
                        WHERE bpi_rfba.budget_plan_id = bp."Id"
                        AND bpi_rfba.is_rfba = TRUE
                    ) AS has_rfba_items,
                    fv.vendor_shadow_id,
                    v.card_code AS vendor_code,
                    v.card_name AS vendor_name,
                    u_sub."Fullname" AS maker_name,
                    u_approver."Fullname" AS approval_name,
                    COALESCE(
                        (SELECT json_agg(po_sub ORDER BY po_sub.id)
                         FROM (
                             SELECT DISTINCT po."Id" AS id, po.code
                             FROM purchase_order_items poi
                             JOIN purchase_orders po ON po."Id" = poi.purchase_order_id
                             JOIN budget_plan_items bpi2 ON bpi2."Id" = poi.budget_plan_item_id
                             WHERE bpi2.budget_plan_id = bp."Id"
                               AND bpi2.is_rfba = @p_is_rfba
                               AND po.deleted_at IS NULL
                               AND (@p_tenant_filter_disabled OR po.company_id = @p_company_id)
                         ) po_sub),
                        '[]'::json
                    ) AS purchase_orders,
                    (SELECT STRING_AGG(DISTINCT po_num.code, ', ' ORDER BY po_num.code)
                     FROM purchase_order_items poi_num
                     JOIN purchase_orders po_num ON po_num."Id" = poi_num.purchase_order_id
                     JOIN budget_plan_items bpi_num ON bpi_num."Id" = poi_num.budget_plan_item_id
                     WHERE bpi_num.budget_plan_id = bp."Id"
                       AND bpi_num.is_rfba = @p_is_rfba
                       AND po_num.deleted_at IS NULL
                       AND (@p_tenant_filter_disabled OR po_num.company_id = @p_company_id)
                    ) AS po_number,
                    ws.location,
                    COALESCE(SUM(bpi_total.cost_value * bpi_total.quantity), 0) AS total_budget_plan,
                    COALESCE((
                        SELECT SUM(poi_s.cost_value * poi_s.quantity)
                        FROM purchase_order_items poi_s
                        JOIN purchase_orders po_s ON po_s."Id" = poi_s.purchase_order_id
                        JOIN budget_plan_items bpi_s ON bpi_s."Id" = poi_s.budget_plan_item_id
                        WHERE bpi_s.budget_plan_id = bp."Id"
                        AND po_s.deleted_at IS NULL
                        AND (@p_tenant_filter_disabled OR po_s.company_id = @p_company_id)
                    ), 0) AS budget_approved
                FROM budget_plans bp
                JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
                LEFT JOIN budget_plan_items bpi_total ON bpi_total.budget_plan_id = bp."Id"
                LEFT JOIN LATERAL (
                    SELECT bpi.vendor_shadow_id
                    FROM budget_plan_items bpi
                    WHERE bpi.budget_plan_id = bp."Id"
                    ORDER BY bpi.sort_order
                    LIMIT 1
                ) fv ON TRUE
                LEFT JOIN vendor_shadows v ON v."Id" = fv.vendor_shadow_id
                LEFT JOIN users u_sub ON u_sub."Id" = bp.submitted_by_user_id
                LEFT JOIN workflow_instances wi_ap ON wi_ap.doc_id = bp."Id" AND wi_ap.doc_type = 'BudgetPlanApproval'
                LEFT JOIN LATERAL (
                    SELECT wis.approved_by_user_id
                    FROM workflow_instance_stages wis
                    WHERE wis.workflow_instance_id = wi_ap."Id"
                      AND wis.status = 'Approved'
                    ORDER BY wis.stage_order DESC
                    LIMIT 1
                ) last_stage ON TRUE
                LEFT JOIN users u_approver ON u_approver."Id" = last_stage.approved_by_user_id
                WHERE bp.status = 'Approved'
                AND bp.deleted_at IS NULL
                AND (@p_tenant_filter_disabled OR bp.company_id = @p_company_id)
                AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
                AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern OR v.card_name ILIKE @p_search_pattern)
                AND EXISTS (
                    SELECT 1
                    FROM purchase_order_items poi_filter
                    JOIN purchase_orders po_filter ON po_filter."Id" = poi_filter.purchase_order_id
                    JOIN budget_plan_items bpi_filter ON bpi_filter."Id" = poi_filter.budget_plan_item_id
                    WHERE bpi_filter.budget_plan_id = bp."Id"
                      AND bpi_filter.is_rfba = @p_is_rfba
                      AND po_filter.deleted_at IS NULL
                      AND (@p_tenant_filter_disabled OR po_filter.company_id = @p_company_id)
                )
                GROUP BY bp."Id", bp.code, bp.remark, bp.doc_date, bp.status, bp.created_at,
                         u_sub."Fullname", u_approver."Fullname",
                         fv.vendor_shadow_id, v.card_code, v.card_name,
                         ws.location
            )
            SELECT * FROM report
            ORDER BY {orderBy}
            LIMIT @p_limit OFFSET @p_offset;
            """;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("p_is_rfba", NpgsqlDbType.Boolean) { Value = isRfba });
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsParam });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = query.Limit });
        cmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = (query.Page - 1) * query.Limit });

        var result = new List<ApprovedBudgetPlanPoStatusResponse>();
        var totalCount = 0;
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colTotalCount = reader.GetOrdinal("total_count");
        var colBudgetPlanId = reader.GetOrdinal("budget_plan_id");
        var colBudgetPlanCode = reader.GetOrdinal("budget_plan_code");
        var colRemark = reader.GetOrdinal("remark");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colBudgetPlanStatus = reader.GetOrdinal("budget_plan_status");
        var colHasRfbaItems = reader.GetOrdinal("has_rfba_items");
        var colVendorShadowId = reader.GetOrdinal("vendor_shadow_id");
        var colVendorCode = reader.GetOrdinal("vendor_code");
        var colVendorName = reader.GetOrdinal("vendor_name");
        var colMakerName = reader.GetOrdinal("maker_name");
        var colApprovalName = reader.GetOrdinal("approval_name");
        var colPurchaseOrders = reader.GetOrdinal("purchase_orders");
        var colLocation = reader.GetOrdinal("location");
        var colTotalBudgetPlan = reader.GetOrdinal("total_budget_plan");
        var colBudgetApproved = reader.GetOrdinal("budget_approved");

        while (await reader.ReadAsync(ct))
        {
            totalCount = reader.GetInt32(colTotalCount);
            var bpStatusStr = reader.GetString(colBudgetPlanStatus);
            var bpStatusDisplay = BudgetPlanStatus.TryFromValue(bpStatusStr, out var bps) ? bps.DisplayName : bpStatusStr;
            var totalBudgetPlan = reader.GetDecimal(colTotalBudgetPlan);
            var budgetApproved = reader.GetDecimal(colBudgetApproved);
            var purchaseOrders = JsonSerializer.Deserialize<List<PoLinkInfo>>(
                reader.GetString(colPurchaseOrders), CaseInsensitiveJsonOpts) ?? [];
            result.Add(new ApprovedBudgetPlanPoStatusResponse(
                reader.GetInt64(colBudgetPlanId),
                reader.GetString(colBudgetPlanCode),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.GetDateTime(colDocDate),
                bpStatusStr,
                bpStatusDisplay,
                reader.GetBoolean(colHasRfbaItems),
                reader.IsDBNull(colVendorShadowId) ? (long?)null : reader.GetInt64(colVendorShadowId),
                reader.IsDBNull(colVendorCode) ? null : reader.GetString(colVendorCode),
                reader.IsDBNull(colVendorName) ? null : reader.GetString(colVendorName),
                reader.IsDBNull(colMakerName) ? null : reader.GetString(colMakerName),
                reader.IsDBNull(colApprovalName) ? null : reader.GetString(colApprovalName),
                purchaseOrders,
                reader.IsDBNull(colLocation) ? null : reader.GetString(colLocation),
                totalBudgetPlan,
                budgetApproved,
                totalBudgetPlan - budgetApproved));
        }

        return (result, totalCount);
    }

    public async IAsyncEnumerable<ApprovedBudgetPlanPoStatusResponse> StreamRecapPurchaseOrdersAsync(
        bool isRfba,
        long[]? warehouseIds,
        DataTableQuery query,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;
        var warehouseFilterDisabled = warehouseIds is null;
        var warehouseIdsParam = warehouseIds ?? [];
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);

        var orderBy = ApprovedBudgetPlanSortColumns.TryGetValue(query.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : "created_at DESC NULLS LAST, budget_plan_id DESC";

        var sql = $"""
            WITH report AS (
                SELECT
                    bp."Id" AS budget_plan_id,
                    bp.code AS budget_plan_code,
                    bp.remark,
                    bp.doc_date,
                    bp.created_at,
                    bp.status AS budget_plan_status,
                    EXISTS (
                        SELECT 1
                        FROM budget_plan_items bpi_rfba
                        WHERE bpi_rfba.budget_plan_id = bp."Id"
                        AND bpi_rfba.is_rfba = TRUE
                    ) AS has_rfba_items,
                    fv.vendor_shadow_id,
                    v.card_code AS vendor_code,
                    v.card_name AS vendor_name,
                    u_sub."Fullname" AS maker_name,
                    u_approver."Fullname" AS approval_name,
                    COALESCE(
                        (SELECT json_agg(po_sub ORDER BY po_sub.id)
                         FROM (
                             SELECT DISTINCT po."Id" AS id, po.code
                             FROM purchase_order_items poi
                             JOIN purchase_orders po ON po."Id" = poi.purchase_order_id
                             JOIN budget_plan_items bpi2 ON bpi2."Id" = poi.budget_plan_item_id
                             WHERE bpi2.budget_plan_id = bp."Id"
                               AND bpi2.is_rfba = @p_is_rfba
                               AND po.deleted_at IS NULL
                               AND (@p_tenant_filter_disabled OR po.company_id = @p_company_id)
                         ) po_sub),
                        '[]'::json
                    ) AS purchase_orders,
                    (SELECT STRING_AGG(DISTINCT po_num.code, ', ' ORDER BY po_num.code)
                     FROM purchase_order_items poi_num
                     JOIN purchase_orders po_num ON po_num."Id" = poi_num.purchase_order_id
                     JOIN budget_plan_items bpi_num ON bpi_num."Id" = poi_num.budget_plan_item_id
                     WHERE bpi_num.budget_plan_id = bp."Id"
                       AND bpi_num.is_rfba = @p_is_rfba
                       AND po_num.deleted_at IS NULL
                       AND (@p_tenant_filter_disabled OR po_num.company_id = @p_company_id)
                    ) AS po_number,
                    ws.location,
                    COALESCE(SUM(bpi_total.cost_value * bpi_total.quantity), 0) AS total_budget_plan,
                    COALESCE((
                        SELECT SUM(poi_s.cost_value * poi_s.quantity)
                        FROM purchase_order_items poi_s
                        JOIN purchase_orders po_s ON po_s."Id" = poi_s.purchase_order_id
                        JOIN budget_plan_items bpi_s ON bpi_s."Id" = poi_s.budget_plan_item_id
                        WHERE bpi_s.budget_plan_id = bp."Id"
                        AND po_s.deleted_at IS NULL
                        AND (@p_tenant_filter_disabled OR po_s.company_id = @p_company_id)
                    ), 0) AS budget_approved
                FROM budget_plans bp
                JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
                LEFT JOIN budget_plan_items bpi_total ON bpi_total.budget_plan_id = bp."Id"
                LEFT JOIN LATERAL (
                    SELECT bpi.vendor_shadow_id
                    FROM budget_plan_items bpi
                    WHERE bpi.budget_plan_id = bp."Id"
                    ORDER BY bpi.sort_order
                    LIMIT 1
                ) fv ON TRUE
                LEFT JOIN vendor_shadows v ON v."Id" = fv.vendor_shadow_id
                LEFT JOIN users u_sub ON u_sub."Id" = bp.submitted_by_user_id
                LEFT JOIN workflow_instances wi_ap ON wi_ap.doc_id = bp."Id" AND wi_ap.doc_type = 'BudgetPlanApproval'
                LEFT JOIN LATERAL (
                    SELECT wis.approved_by_user_id
                    FROM workflow_instance_stages wis
                    WHERE wis.workflow_instance_id = wi_ap."Id"
                      AND wis.status = 'Approved'
                    ORDER BY wis.stage_order DESC
                    LIMIT 1
                ) last_stage ON TRUE
                LEFT JOIN users u_approver ON u_approver."Id" = last_stage.approved_by_user_id
                WHERE bp.status = 'Approved'
                AND bp.deleted_at IS NULL
                AND (@p_tenant_filter_disabled OR bp.company_id = @p_company_id)
                AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
                AND (@p_search IS NULL OR bp.code ILIKE @p_search_pattern OR v.card_name ILIKE @p_search_pattern)
                AND EXISTS (
                    SELECT 1
                    FROM purchase_order_items poi_filter
                    JOIN purchase_orders po_filter ON po_filter."Id" = poi_filter.purchase_order_id
                    JOIN budget_plan_items bpi_filter ON bpi_filter."Id" = poi_filter.budget_plan_item_id
                    WHERE bpi_filter.budget_plan_id = bp."Id"
                      AND bpi_filter.is_rfba = @p_is_rfba
                      AND po_filter.deleted_at IS NULL
                      AND (@p_tenant_filter_disabled OR po_filter.company_id = @p_company_id)
                )
                GROUP BY bp."Id", bp.code, bp.remark, bp.doc_date, bp.status, bp.created_at,
                         u_sub."Fullname", u_approver."Fullname",
                         fv.vendor_shadow_id, v.card_code, v.card_name,
                         ws.location
            )
            SELECT * FROM report
            ORDER BY {orderBy}
            LIMIT @p_limit;
            """;

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("p_is_rfba", NpgsqlDbType.Boolean) { Value = isRfba });
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsParam });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = limit });

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colBudgetPlanId = reader.GetOrdinal("budget_plan_id");
        var colBudgetPlanCode = reader.GetOrdinal("budget_plan_code");
        var colRemark = reader.GetOrdinal("remark");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colBudgetPlanStatus = reader.GetOrdinal("budget_plan_status");
        var colHasRfbaItems = reader.GetOrdinal("has_rfba_items");
        var colVendorShadowId = reader.GetOrdinal("vendor_shadow_id");
        var colVendorCode = reader.GetOrdinal("vendor_code");
        var colVendorName = reader.GetOrdinal("vendor_name");
        var colMakerName = reader.GetOrdinal("maker_name");
        var colApprovalName = reader.GetOrdinal("approval_name");
        var colPurchaseOrders = reader.GetOrdinal("purchase_orders");
        var colLocation = reader.GetOrdinal("location");
        var colTotalBudgetPlan = reader.GetOrdinal("total_budget_plan");
        var colBudgetApproved = reader.GetOrdinal("budget_approved");

        while (await reader.ReadAsync(ct))
        {
            var bpStatusStr = reader.GetString(colBudgetPlanStatus);
            var bpStatusDisplay = BudgetPlanStatus.TryFromValue(bpStatusStr, out var bps) ? bps.DisplayName : bpStatusStr;
            var totalBudgetPlan = reader.GetDecimal(colTotalBudgetPlan);
            var budgetApproved = reader.GetDecimal(colBudgetApproved);
            var purchaseOrders = JsonSerializer.Deserialize<List<PoLinkInfo>>(
                reader.GetString(colPurchaseOrders), CaseInsensitiveJsonOpts) ?? [];
            yield return new ApprovedBudgetPlanPoStatusResponse(
                reader.GetInt64(colBudgetPlanId),
                reader.GetString(colBudgetPlanCode),
                reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                reader.GetDateTime(colDocDate),
                bpStatusStr,
                bpStatusDisplay,
                reader.GetBoolean(colHasRfbaItems),
                reader.IsDBNull(colVendorShadowId) ? (long?)null : reader.GetInt64(colVendorShadowId),
                reader.IsDBNull(colVendorCode) ? null : reader.GetString(colVendorCode),
                reader.IsDBNull(colVendorName) ? null : reader.GetString(colVendorName),
                reader.IsDBNull(colMakerName) ? null : reader.GetString(colMakerName),
                reader.IsDBNull(colApprovalName) ? null : reader.GetString(colApprovalName),
                purchaseOrders,
                reader.IsDBNull(colLocation) ? null : reader.GetString(colLocation),
                totalBudgetPlan,
                budgetApproved,
                totalBudgetPlan - budgetApproved);
        }
    }

    public Task CreateAsync(PurchaseOrder po, CancellationToken ct = default)
    {
        db.PurchaseOrders.Add(po);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PurchaseOrder po, CancellationToken ct = default)
    {
        db.PurchaseOrders.Update(po);
        return Task.CompletedTask;
    }

    public async Task<bool> MarkGeneratedAsync(
        long id,
        string claimToken,
        string sapPoNumber,
        int? sapDocEntry,
        long generatedByUserId,
        CancellationToken ct = default
    )
    {
        var rows = await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE purchase_orders
            SET status                = 'Generated',
                sap_po_number         = {sapPoNumber},
                sap_doc_entry         = {sapDocEntry},
                generated_by_user_id  = {generatedByUserId},
                generated_at          = NOW(),
                generation_claimed_at = NULL,
                generation_claim_token = NULL,
                updated_at            = NOW()
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
            UPDATE purchase_orders
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
            UPDATE purchase_orders
            SET generation_claimed_at = NULL, generation_claim_token = NULL
            WHERE "Id" = {id} AND generation_claim_token = {claimToken}
            """, ct);

    public async Task<bool> TryClaimForApdpGenerationAsync(long id, string claimToken, CancellationToken ct = default)
    {
        var rows = await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE purchase_orders
            SET apdp_generation_claimed_at = NOW(), apdp_generation_claim_token = {claimToken},
                sap_apdp_error = NULL, updated_at = NOW()
            WHERE "Id" = {id}
              AND status = 'Generated'
              AND sap_doc_entry IS NOT NULL
              AND deleted_at IS NULL
              AND (sap_apdp_doc_entry IS NULL)
              AND (apdp_generation_claimed_at IS NULL OR apdp_generation_claimed_at < NOW() - INTERVAL '15 minutes')
            """, ct);
        return rows == 1;
    }

    public async Task<bool> MarkApdpGeneratedAsync(long id, string claimToken, int sapDocEntry, CancellationToken ct = default)
    {
        var rows = await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE purchase_orders
            SET sap_apdp_doc_entry = {sapDocEntry},
                sap_apdp_generated_at = NOW(),
                sap_apdp_error = NULL,
                apdp_generation_claimed_at = NULL,
                apdp_generation_claim_token = NULL,
                updated_at = NOW()
            WHERE "Id" = {id}
              AND apdp_generation_claim_token = {claimToken}
              AND sap_apdp_doc_entry IS NULL
              AND deleted_at IS NULL
            """, ct);
        return rows == 1;
    }

    public async Task RecordApdpFailureAsync(long id, string claimToken, string error, CancellationToken ct = default)
        => await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE purchase_orders
            SET sap_apdp_error = {error}, updated_at = NOW()
            WHERE "Id" = {id} AND apdp_generation_claim_token = {claimToken} AND deleted_at IS NULL
            """, ct);

    public async Task ReleaseApdpGenerationClaimAsync(long id, string claimToken, CancellationToken ct = default)
        => await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE purchase_orders
            SET apdp_generation_claimed_at = NULL, apdp_generation_claim_token = NULL,
                updated_at = NOW()
            WHERE "Id" = {id} AND apdp_generation_claim_token = {claimToken}
            """, ct);

    public async Task<bool> LockForEditAsync(long id, CancellationToken ct = default)
        => (await db.Database.SqlQuery<long>($"""
            SELECT "Id" AS "Value" FROM purchase_orders
            WHERE "Id" = {id} AND deleted_at IS NULL
              AND (generation_claimed_at IS NULL OR generation_claimed_at < NOW() - INTERVAL '15 minutes')
            FOR UPDATE
            """).ToListAsync(ct)).Count == 1;

    public async Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default)
    {
        var rows = await db.PurchaseOrders
            .Where(p => p.Id == id)
            .Where(p => p.GenerationClaimedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, DateTime.UtcNow), ct);
        return rows == 1;
    }
}
