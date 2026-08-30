namespace WAMS.Infrastructure.Repositories.WorkOrders;

using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WAMS.Application.Common;
using WAMS.Application.DTOs.WorkOrders;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class WorkOrderRepository(
    AppDbContext db,
    ITenantContext tenantContext) : IWorkOrderRepository
{
    private const string DefaultOrderBy = "wo.created_at DESC NULLS LAST, wo.\"Id\" DESC";
    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = "wo.status",
            ["startdate"] = "wo.start_date",
            ["createdat"] = "wo.created_at",
        };

    public async Task<(List<WorkOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(
        WorkOrderQuery q,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default
    )
    {
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];
        var warehouseFilterDisabled = warehouseIds is null;

        var orderBy = SortColumns.TryGetValue(q.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(q.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : DefaultOrderBy;

        var search = string.IsNullOrWhiteSpace(q.Search) ? null : q.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);
        var dateFrom = q.DateFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dateTo = q.DateTo?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var offset = (q.Page - 1) * q.Limit;

        // Query 1: COUNT only - no lateral, no ORDER BY, no OFFSET/LIMIT.
        // The spk_first lateral is not needed for counting; search on wo.code/bp.code is
        // already in the base tables so no correlated rewrite is required.
        const string countSql = @"
            SELECT COUNT(*)
            FROM work_orders wo
            WHERE wo.deleted_at IS NULL
              AND (@p_warehouse_filter_disabled OR wo.warehouse_shadow_id = ANY(@p_warehouse_ids))
              AND (@p_status IS NULL OR wo.status = @p_status)
              AND (@p_budget_plan_id IS NULL OR wo.budget_plan_id = @p_budget_plan_id)
              AND (@p_budget_plan_item_id IS NULL OR wo.budget_plan_item_id = @p_budget_plan_item_id)
              AND (@p_search IS NULL
                   OR wo.code ILIKE @p_search_pattern
                   OR wo.budget_plan_id IN (SELECT bp.""Id"" FROM budget_plans bp WHERE bp.code ILIKE @p_search_pattern))
              AND (@p_date_from IS NULL OR wo.start_date >= @p_date_from)
              AND (@p_date_to IS NULL OR wo.start_date < @p_date_to);";

        var dataSql = $@"
            SELECT
                wo.""Id"",
                wo.code,
                wo.budget_plan_id,
                bp.code AS budget_plan_code,
                wo.activity_type_code,
                COALESCE(at.name, wo.activity_type_code) AS activity_type_display,
                wo.item_shadow_id,
                item.item_name AS activity_name,
                ws.code AS warehouse_code,
                ws.name AS warehouse_name,
                pic.""Fullname"" AS pic_name,
                wo.is_rfba,
                wo.start_date,
                wo.end_date,
                wo.status,
                wo.created_at,
                cu.""Fullname"" AS created_by_name,
                spk_first.bl_no,
                spk_first.item_name AS spk_item_name,
                spk_first.card_name AS spk_card_name
            FROM work_orders wo
            JOIN budget_plans bp ON bp.""Id"" = wo.budget_plan_id
            JOIN warehouse_shadows ws ON ws.""Id"" = bp.warehouse_shadow_id
            JOIN item_shadows item ON item.""Id"" = wo.item_shadow_id
            LEFT JOIN activity_types at ON at.code = wo.activity_type_code AND at.deleted_at IS NULL
            LEFT JOIN users pic ON pic.""Id"" = wo.pic_user_id
            LEFT JOIN users cu ON cu.""Id"" = wo.created_by_user_id
            LEFT JOIN LATERAL (
                SELECT spk.bl_no, spk.item_name, spk.card_name
                FROM budget_plan_spk_items bpsi
                JOIN spk_shadows spk ON spk.""Id"" = bpsi.spk_shadow_id
                WHERE bpsi.budget_plan_id = bp.""Id""
                ORDER BY bpsi.sort_order
                LIMIT 1
            ) spk_first ON TRUE
            WHERE wo.deleted_at IS NULL
              AND (@p_warehouse_filter_disabled OR wo.warehouse_shadow_id = ANY(@p_warehouse_ids))
              AND (@p_status IS NULL OR wo.status = @p_status)
              AND (@p_budget_plan_id IS NULL OR wo.budget_plan_id = @p_budget_plan_id)
              AND (@p_budget_plan_item_id IS NULL OR wo.budget_plan_item_id = @p_budget_plan_item_id)
              AND (@p_search IS NULL
                   OR wo.code ILIKE @p_search_pattern
                   OR bp.code ILIKE @p_search_pattern)
              AND (@p_date_from IS NULL OR wo.start_date >= @p_date_from)
              AND (@p_date_to IS NULL OR wo.start_date < @p_date_to)
            ORDER BY {orderBy}
            OFFSET @p_offset LIMIT @p_limit;";

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        // Shared parameter factory - NpgsqlParameter instances cannot be reused across commands,
        // so MakeFilterParams() creates fresh instances with the same values for each command.
        NpgsqlParameter[] MakeFilterParams() =>
        [
            new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled },
            new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray },
            new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)q.Status ?? DBNull.Value },
            new NpgsqlParameter("p_budget_plan_id", NpgsqlDbType.Bigint) { Value = (object?)q.BudgetPlanId ?? DBNull.Value },
            new NpgsqlParameter("p_budget_plan_item_id", NpgsqlDbType.Bigint) { Value = (object?)q.BudgetPlanItemId ?? DBNull.Value },
            new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value },
            new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value },
            new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = (object?)dateFrom ?? DBNull.Value },
            new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = (object?)dateTo ?? DBNull.Value },
        ];

        // COUNT (no lateral - cheap scan)
        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = countSql;
        foreach (var p in MakeFilterParams()) countCmd.Parameters.Add(p);
        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        // short-circuit - if the requested offset is beyond the total, skip the data
        if (offset >= total) return ([], total);

        // DATA (lateral runs only for the LIMIT rows, not the full table)
        await using var dataCmd = conn.CreateCommand();
        dataCmd.CommandText = dataSql;
        foreach (var p in MakeFilterParams()) dataCmd.Parameters.Add(p);
        dataCmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = offset });
        dataCmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = q.Limit });

        var items = new List<WorkOrderSummaryResponse>();

        await using var reader = await dataCmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colCode = reader.GetOrdinal("code");
        var colBudgetPlanId = reader.GetOrdinal("budget_plan_id");
        var colBudgetPlanCode = reader.GetOrdinal("budget_plan_code");
        var colActivityTypeCode = reader.GetOrdinal("activity_type_code");
        var colActivityTypeDisplay = reader.GetOrdinal("activity_type_display");
        var colItemShadowId = reader.GetOrdinal("item_shadow_id");
        var colActivityName = reader.GetOrdinal("activity_name");
        var colWarehouseCode = reader.GetOrdinal("warehouse_code");
        var colWarehouseName = reader.GetOrdinal("warehouse_name");
        var colPicName = reader.GetOrdinal("pic_name");
        var colIsRfba = reader.GetOrdinal("is_rfba");
        var colStartDate = reader.GetOrdinal("start_date");
        var colEndDate = reader.GetOrdinal("end_date");
        var colStatus = reader.GetOrdinal("status");
        var colCreatedAt = reader.GetOrdinal("created_at");
        var colCreatedByName = reader.GetOrdinal("created_by_name");
        var colBlNo = reader.GetOrdinal("bl_no");
        var colSpkItemName = reader.GetOrdinal("spk_item_name");
        var colSpkCardName = reader.GetOrdinal("spk_card_name");

        while (await reader.ReadAsync(ct))
        {
            items.Add(new WorkOrderSummaryResponse(
                reader.GetInt64(colId),
                reader.GetString(colCode),
                reader.GetInt64(colBudgetPlanId),
                reader.GetString(colBudgetPlanCode),
                reader.GetString(colActivityTypeCode),
                reader.GetString(colActivityTypeDisplay),
                reader.GetInt64(colItemShadowId),
                reader.GetString(colActivityName),
                reader.GetString(colWarehouseCode),
                reader.GetString(colWarehouseName),
                reader.IsDBNull(colPicName) ? null : reader.GetString(colPicName),
                reader.GetBoolean(colIsRfba),
                reader.IsDBNull(colStartDate) ? null : reader.GetDateTime(colStartDate),
                reader.IsDBNull(colEndDate) ? null : reader.GetDateTime(colEndDate),
                reader.GetString(colStatus),
                reader.GetDateTime(colCreatedAt),
                reader.IsDBNull(colCreatedByName) ? "" : reader.GetString(colCreatedByName),
                reader.IsDBNull(colBlNo) ? null : reader.GetString(colBlNo),
                reader.IsDBNull(colSpkItemName) ? null : reader.GetString(colSpkItemName),
                reader.IsDBNull(colSpkCardName) ? null : reader.GetString(colSpkCardName)));
        }

        return (items, total);
    }

    public async IAsyncEnumerable<WorkOrderSummaryResponse> StreamAllAsync(
        WorkOrderQuery q,
        IReadOnlyList<long>? warehouseIds,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];
        var warehouseFilterDisabled = warehouseIds is null;
        var search = string.IsNullOrWhiteSpace(q.Search) ? null : q.Search.Trim();
        var searchPattern = search is null ? null : LikePatternHelper.ToContainsPattern(search);
        var dateFrom = q.DateFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dateTo = q.DateTo?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var orderBy = SortColumns.TryGetValue(q.SortBy ?? "", out var col)
            ? $"{col} {(string.Equals(q.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC")}"
            : DefaultOrderBy;

        var sql = $@"
            SELECT
                wo.""Id"",
                wo.code,
                wo.budget_plan_id,
                bp.code AS budget_plan_code,
                wo.activity_type_code,
                COALESCE(at.name, wo.activity_type_code) AS activity_type_display,
                wo.item_shadow_id,
                item.item_name AS activity_name,
                ws.code AS warehouse_code,
                ws.name AS warehouse_name,
                pic.""Fullname"" AS pic_name,
                wo.is_rfba,
                wo.start_date,
                wo.end_date,
                wo.status,
                wo.created_at,
                cu.""Fullname"" AS created_by_name,
                spk_first.bl_no,
                spk_first.item_name AS spk_item_name,
                spk_first.card_name AS spk_card_name
            FROM work_orders wo
            JOIN budget_plans bp ON bp.""Id"" = wo.budget_plan_id
            JOIN warehouse_shadows ws ON ws.""Id"" = bp.warehouse_shadow_id
            JOIN item_shadows item ON item.""Id"" = wo.item_shadow_id
            LEFT JOIN activity_types at ON at.code = wo.activity_type_code AND at.deleted_at IS NULL
            LEFT JOIN users pic ON pic.""Id"" = wo.pic_user_id
            LEFT JOIN users cu ON cu.""Id"" = wo.created_by_user_id
            LEFT JOIN LATERAL (
                SELECT spk.bl_no, spk.item_name, spk.card_name
                FROM budget_plan_spk_items bpsi
                JOIN spk_shadows spk ON spk.""Id"" = bpsi.spk_shadow_id
                WHERE bpsi.budget_plan_id = bp.""Id""
                ORDER BY bpsi.sort_order
                LIMIT 1
            ) spk_first ON TRUE
            WHERE wo.deleted_at IS NULL
              AND (@p_warehouse_filter_disabled OR wo.warehouse_shadow_id = ANY(@p_warehouse_ids))
              AND (@p_status IS NULL OR wo.status = @p_status)
              AND (@p_budget_plan_id IS NULL OR wo.budget_plan_id = @p_budget_plan_id)
              AND (@p_budget_plan_item_id IS NULL OR wo.budget_plan_item_id = @p_budget_plan_item_id)
              AND (@p_search IS NULL
                   OR wo.code ILIKE @p_search_pattern
                   OR bp.code ILIKE @p_search_pattern)
              AND (@p_date_from IS NULL OR wo.start_date >= @p_date_from)
              AND (@p_date_to IS NULL OR wo.start_date < @p_date_to)
            ORDER BY {orderBy}
            LIMIT @p_limit;";

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });
        cmd.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Text) { Value = (object?)q.Status ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_budget_plan_id", NpgsqlDbType.Bigint) { Value = (object?)q.BudgetPlanId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_budget_plan_item_id", NpgsqlDbType.Bigint) { Value = (object?)q.BudgetPlanItemId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search", NpgsqlDbType.Text) { Value = (object?)search ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_search_pattern", NpgsqlDbType.Text) { Value = (object?)searchPattern ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = (object?)dateFrom ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = (object?)dateTo ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = limit });

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colId = reader.GetOrdinal("Id");
        var colCode = reader.GetOrdinal("code");
        var colBudgetPlanId = reader.GetOrdinal("budget_plan_id");
        var colBudgetPlanCode = reader.GetOrdinal("budget_plan_code");
        var colActivityTypeCode = reader.GetOrdinal("activity_type_code");
        var colActivityTypeDisplay = reader.GetOrdinal("activity_type_display");
        var colItemShadowId = reader.GetOrdinal("item_shadow_id");
        var colActivityName = reader.GetOrdinal("activity_name");
        var colWarehouseCode = reader.GetOrdinal("warehouse_code");
        var colWarehouseName = reader.GetOrdinal("warehouse_name");
        var colPicName = reader.GetOrdinal("pic_name");
        var colIsRfba = reader.GetOrdinal("is_rfba");
        var colStartDate = reader.GetOrdinal("start_date");
        var colEndDate = reader.GetOrdinal("end_date");
        var colStatus = reader.GetOrdinal("status");
        var colCreatedAt = reader.GetOrdinal("created_at");
        var colCreatedByName = reader.GetOrdinal("created_by_name");
        var colBlNo = reader.GetOrdinal("bl_no");
        var colSpkItemName = reader.GetOrdinal("spk_item_name");
        var colSpkCardName = reader.GetOrdinal("spk_card_name");

        while (await reader.ReadAsync(ct))
        {
            yield return new WorkOrderSummaryResponse(
                reader.GetInt64(colId),
                reader.GetString(colCode),
                reader.GetInt64(colBudgetPlanId),
                reader.GetString(colBudgetPlanCode),
                reader.GetString(colActivityTypeCode),
                reader.GetString(colActivityTypeDisplay),
                reader.GetInt64(colItemShadowId),
                reader.GetString(colActivityName),
                reader.GetString(colWarehouseCode),
                reader.GetString(colWarehouseName),
                reader.IsDBNull(colPicName) ? null : reader.GetString(colPicName),
                reader.GetBoolean(colIsRfba),
                reader.IsDBNull(colStartDate) ? null : reader.GetDateTime(colStartDate),
                reader.IsDBNull(colEndDate) ? null : reader.GetDateTime(colEndDate),
                reader.GetString(colStatus),
                reader.GetDateTime(colCreatedAt),
                reader.IsDBNull(colCreatedByName) ? "" : reader.GetString(colCreatedByName),
                reader.IsDBNull(colBlNo) ? null : reader.GetString(colBlNo),
                reader.IsDBNull(colSpkItemName) ? null : reader.GetString(colSpkItemName),
                reader.IsDBNull(colSpkCardName) ? null : reader.GetString(colSpkCardName));
        }
    }

    public async Task<WorkOrderResponse?> GetByIdProjectionAsync(long id, CancellationToken ct = default)
    {
        // EF Core ≥8 compiles nested collection projections into a single SQL using PostgreSQL
        // JSON aggregation (json_agg), so this entire response shape comes back in ONE round-trip.
        // Conditional details (FumigationDetail, StorageDetail, etc.) compile to a CASE that
        // skips the LEFT JOIN body when the activity type doesn't match.
        var row = await db.WorkOrders
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new
            {
                w.Id,
                w.Code,
                w.BudgetPlanId,
                BudgetPlanCode = w.BudgetPlan.Code,
                w.ActivityTypeCode,
                ActivityTypeDisplay = db.ActivityTypes
                    .Where(at => at.Code == w.ActivityTypeCode && at.DeletedAt == null)
                    .Select(at => at.Name)
                    .FirstOrDefault() ?? w.ActivityTypeCode,
                w.ItemShadowId,
                ActivityName = w.Activity.ItemName,
                w.WarehouseShadowId,
                WarehouseCode = w.BudgetPlan.Warehouse.Code,
                WarehouseName = w.BudgetPlan.Warehouse.Name,
                w.TemplateCode,
                VendorNames = w.BudgetPlan.Items
                    .Select(i => i.Vendor.CardName)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList(),
                w.CodeBlock,
                w.PicUserId,
                PicName = w.PicUser != null ? w.PicUser.Fullname : null,
                w.StartDate,
                w.EndDate,
                w.IsRfba,
                Status = w.Status.Value,
                w.Notes,
                Gps = w.GpsLocation,
                Spk = w.BudgetPlan.SpkItems
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new
                    {
                        s.Spk.ItemName,
                        s.Spk.Quantity,
                        s.Spk.UoM,
                        s.Spk.BlNo,
                        s.Spk.CardName,
                    })
                    .FirstOrDefault(),
                TransportOrders = w.TransportOrders
                    .Select(t => new TransportOrderRef(
                        t.TransportOrderShadowId,
                        t.TransportOrderShadow.DocNo,
                        t.TransportOrderShadow.Type,
                        t.TransportOrderShadow.VehicleNo,
                        t.TransportOrderShadow.CardName))
                    .ToList(),
                UnloadingItems = w.ActivityTypeCode == ActivityTypeCodes.Bongkar && w.UnloadingItems.Count > 0
                    ? w.UnloadingItems.OrderBy(i => i.SortOrder).Select(i => new WorkOrderUnloadingItemResponse(
                        i.Id, i.BlNumber, i.ProductName, i.Quantity, i.UomCode,
                        i.NoVehicle, i.NoContainer, i.NoSeal,
                        i.GrossWeight, i.FinalWeight, i.NettWeight,
                        i.TotalBag, i.UnitWeight, i.IsChecked, i.SortOrder)).ToList()
                    : null,
                LoadingItems = w.ActivityTypeCode == ActivityTypeCodes.Muat && w.LoadingItems.Count > 0
                    ? w.LoadingItems.OrderBy(i => i.SortOrder).Select(i => new WorkOrderLoadingItemResponse(
                        i.Id, i.BlNumber, i.ProductName, i.Quantity, i.UomCode,
                        i.NoVehicle, i.NoContainer, i.NoSeal,
                        i.GrossWeight, i.FinalWeight, i.NettWeight,
                        i.TotalBag, i.UnitWeight, i.IsChecked, i.SortOrder)).ToList()
                    : null,
                Fumigation = w.ActivityTypeCode == ActivityTypeCodes.Fumigasi && w.FumigationDetail != null
                    ? new WorkOrderFumigationDetailResponse(
                        w.FumigationDetail.FumiId, w.FumigationDetail.TotalDuration,
                        w.FumigationDetail.BlNumber, w.FumigationDetail.MvName,
                        w.FumigationDetail.InitialTemperature, w.FumigationDetail.FinalTemperature,
                        w.FumigationDetail.FumigationType,
                        w.FumigationDetail.MethylBromideDosage, w.FumigationDetail.SulphurFluorideDosage,
                        w.FumigationDetail.PhosphineDosage, w.FumigationDetail.Result)
                    : null,
                Storage = (w.ActivityTypeCode == ActivityTypeCodes.Gudang
                           || w.ActivityTypeCode == ActivityTypeCodes.Opname
                           || w.ActivityTypeCode == ActivityTypeCodes.Others) && w.StorageDetail != null
                    ? new WorkOrderStorageDetailResponse(
                        w.StorageDetail.HasPindahStapel, w.StorageDetail.HasPembersihan, w.StorageDetail.HasPerapihan,
                        w.StorageDetail.VolumeWeight, w.StorageDetail.WorkerOnDuty,
                        w.StorageDetail.HasMask, w.StorageDetail.HasSafetyGlasses, w.StorageDetail.HasHandGloves,
                        w.StorageDetail.HasHelmet, w.StorageDetail.HasSafetyShoes, w.StorageDetail.HasSafetyVest)
                    : null,
                Qc = w.ActivityTypeCode == ActivityTypeCodes.Qc && w.QcDetail != null
                    ? new WorkOrderQcDetailResponse(
                        w.QcDetail.MoisturePercent, w.QcDetail.JamurPercent,
                        w.QcDetail.BauPercent, w.QcDetail.QualityStatus)
                    : null,
                HeavyEquip = w.ActivityTypeCode == ActivityTypeCodes.AlatBerat && w.HeavyEquipDetail != null
                    ? new WorkOrderHeavyEquipDetailResponse(
                        w.HeavyEquipDetail.BlNumber, w.HeavyEquipDetail.StartTime, w.HeavyEquipDetail.EndTime,
                        w.HeavyEquipDetail.StandbyDuration1, w.HeavyEquipDetail.StandbyDuration2,
                        w.HeavyEquipDetail.MinimumDuration,
                        w.HeavyEquipDetail.CostPerHour, w.HeavyEquipDetail.TotalCost)
                    : null,
                Unbagging = w.ActivityTypeCode == ActivityTypeCodes.Unbagging && w.UnbaggingDetail != null
                    ? new WorkOrderUnbaggingDetailResponse(
                        w.UnbaggingDetail.NoVehicle, w.UnbaggingDetail.NoContainer, w.UnbaggingDetail.NoSeal,
                        w.UnbaggingDetail.InitialWeight, w.UnbaggingDetail.FinalWeight,
                        w.UnbaggingDetail.UnitWeight, w.UnbaggingDetail.TotalWeight, w.UnbaggingDetail.TotalBag)
                    : null,
                Rebagging = w.ActivityTypeCode == ActivityTypeCodes.Rebagging && w.RebaggingDetail != null
                    ? new WorkOrderRebaggingDetailResponse(
                        w.RebaggingDetail.Receiver,
                        w.RebaggingDetail.NoVehicle, w.RebaggingDetail.NoContainer, w.RebaggingDetail.NoSeal,
                        w.RebaggingDetail.InitialWeight, w.RebaggingDetail.FinalWeight,
                        w.RebaggingDetail.TotalWeight)
                    : null,
                w.CreatedAt,
                CreatedByName = w.CreatedBy.Fullname,
                w.SubmittedAt,
                SubmittedByName = w.SubmittedBy != null ? w.SubmittedBy.Fullname : null,
            })
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;

        var vendorName = row.VendorNames.Count > 0 ? string.Join(", ", row.VendorNames) : null;
        var transportOrders = row.TransportOrders.Count > 0
            ? row.TransportOrders.DistinctBy(t => t.ShadowId).ToList()
            : null;

        return new WorkOrderResponse(
            row.Id,
            row.Code,
            row.BudgetPlanId,
            row.BudgetPlanCode,
            row.ActivityTypeCode,
            row.ActivityTypeDisplay,
            row.ItemShadowId,
            row.ActivityName,
            row.WarehouseShadowId,
            row.WarehouseCode,
            row.WarehouseName,
            row.TemplateCode,
            vendorName,
            row.CodeBlock,
            row.PicUserId,
            row.PicName,
            row.StartDate,
            row.EndDate,
            row.IsRfba,
            row.Status,
            row.Notes,
            row.Gps is null ? null : new GpsLocationResponse(
                row.Gps.Latitude, row.Gps.Longitude, row.Gps.Accuracy, row.Gps.RecordedAt),
            row.Spk?.ItemName,
            row.Spk?.Quantity,
            row.Spk?.UoM,
            row.Spk?.BlNo,
            row.Spk?.CardName,
            transportOrders,
            row.UnloadingItems,
            row.LoadingItems,
            row.Fumigation,
            row.Storage,
            row.Qc,
            row.HeavyEquip,
            row.Unbagging,
            row.Rebagging,
            row.CreatedAt,
            row.CreatedByName,
            row.SubmittedAt,
            row.SubmittedByName);
    }

    public Task<long?> GetWarehouseShadowIdAsync(long id, CancellationToken ct = default)
        => db.WorkOrders
            .Where(w => w.Id == id && w.DeletedAt == null)
            .Select(w => (long?)w.WarehouseShadowId)
            .FirstOrDefaultAsync(ct);

    public async Task<WorkOrder?> GetByIdForUpdateAsync(long id, CancellationToken ct = default)
    {
        // Tracked load. Only collections the UPDATE path actually mutates are eager-loaded.
        // 1:1 detail navs are conditionally loaded after the root based on ActivityTypeCode -
        // we never need more than one of them per WorkOrder.
        var wo = await db.WorkOrders
            .Include(w => w.UnloadingItems)
            .Include(w => w.LoadingItems)
            .Include(w => w.TransportOrders)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (wo is null) return null;

        var entry = db.Entry(wo);
        switch (wo.ActivityTypeCode)
        {
            case ActivityTypeCodes.Fumigasi:
                await entry.Reference(w => w.FumigationDetail).LoadAsync(ct);
                break;
            case ActivityTypeCodes.Gudang:
            case ActivityTypeCodes.Opname:
            case ActivityTypeCodes.Others:
                await entry.Reference(w => w.StorageDetail).LoadAsync(ct);
                break;
            case ActivityTypeCodes.Qc:
                await entry.Reference(w => w.QcDetail).LoadAsync(ct);
                break;
            case ActivityTypeCodes.AlatBerat:
                await entry.Reference(w => w.HeavyEquipDetail).LoadAsync(ct);
                break;
            case ActivityTypeCodes.Unbagging:
                await entry.Reference(w => w.UnbaggingDetail).LoadAsync(ct);
                break;
            case ActivityTypeCodes.Rebagging:
                await entry.Reference(w => w.RebaggingDetail).LoadAsync(ct);
                break;
        }

        return wo;
    }

    // SQL returns one row per (budget_plan × item_shadow). Client-side grouping by BudgetPlanId
    // assembles the flat rows into the nested Activities list on each response object.
    public async Task<(List<ApprovedBpForWoResponse> Items, int Total)> GetApprovedBpListAsync(
        IReadOnlyList<long>? warehouseIds,
        int page,
        int limit,
        CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];

        const string sql = """
            WITH paged_bps AS (
                SELECT bp."Id",
                       COUNT(*) OVER() AS total_count
                FROM budget_plans bp
                WHERE bp.status = 'Approved'
                AND bp.deleted_at IS NULL
                AND (@p_tenant_filter_disabled OR bp.company_id = @p_company_id)
                AND (@p_warehouse_filter_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
                ORDER BY bp.created_at DESC NULLS LAST, bp."Id" DESC
                LIMIT @p_limit OFFSET @p_offset
            )
            SELECT
                bp."Id" AS budget_plan_id,
                bp.code AS budget_plan_code,
                bt.code AS template_code,
                ws."Id" AS warehouse_shadow_id,
                ws.code AS warehouse_code,
                ws.name AS warehouse_name,
                bp.remark,
                EXISTS (
                    SELECT 1
                    FROM budget_plan_items bpi_rfba
                    WHERE bpi_rfba.budget_plan_id = bp."Id"
                    AND bpi_rfba.is_rfba = TRUE
                ) AS is_rfba,
                bp.doc_date,
                maker."Fullname" AS maker_name,
                spk_first.card_name AS vendor_name,
                EXISTS (
                    SELECT 1
                    FROM recap_work_orders rwo
                    WHERE rwo.budget_plan_id = bp."Id"
                    AND rwo.status = 'Approved'
                ) AS is_locked,
                bpi."Id" AS budget_plan_item_id,
                item."Id" AS item_shadow_id,
                item.item_code,
                item.item_name AS activity_name,
                at_item.code AS item_activity_type_code,
                at_item.name AS item_activity_type_display,
                wo_ref.wo_id AS work_order_id,
                wo_ref.wo_code AS work_order_code,
                wo_ref.wo_status AS work_order_status,
                pb.total_count
            FROM paged_bps pb
            JOIN budget_plans bp ON bp."Id" = pb."Id"
            JOIN budget_templates bt ON bt."Id" = bp.budget_template_id
            JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
            JOIN users maker ON maker."Id" = bp.created_by_user_id
            JOIN budget_plan_items bpi ON bpi.budget_plan_id = bp."Id"
            JOIN item_shadows item ON item."Id" = bpi.item_shadow_id
            LEFT JOIN activity_types at_item ON at_item."Id" = bpi.activity_type_id
            LEFT JOIN LATERAL (
                SELECT STRING_AGG(DISTINCT v.card_name, ', ' ORDER BY v.card_name) AS card_name
                FROM budget_plan_items bpi_v
                JOIN vendor_shadows v ON v."Id" = bpi_v.vendor_shadow_id
                WHERE bpi_v.budget_plan_id = bp."Id"
            ) spk_first ON TRUE
            LEFT JOIN LATERAL (
                SELECT wo."Id" AS wo_id, wo.code AS wo_code, wo.status AS wo_status
                FROM work_orders wo
                WHERE wo.budget_plan_id = bp."Id"
                AND wo.budget_plan_item_id = bpi."Id"
                AND wo.deleted_at IS NULL
                ORDER BY wo."Id" DESC
                LIMIT 1
            ) wo_ref ON TRUE
            ORDER BY bp.created_at DESC NULLS LAST, bp."Id" DESC, bpi.sort_order, item.item_code;
            """;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("p_tenant_filter_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_filter_disabled", NpgsqlDbType.Boolean) { Value = warehouseIdsArray.Length == 0 });
        cmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });
        cmd.Parameters.Add(new NpgsqlParameter("p_limit", NpgsqlDbType.Integer) { Value = limit });
        cmd.Parameters.Add(new NpgsqlParameter("p_offset", NpgsqlDbType.Integer) { Value = (page - 1) * limit });

        var grouped = new Dictionary<long, (ApprovedBpForWoResponse Header, List<BpActivityWoStatus> Activities)>();
        var totalCount = 0;

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colBpId = reader.GetOrdinal("budget_plan_id");
        var colBpCode = reader.GetOrdinal("budget_plan_code");
        var colTemplateCode = reader.GetOrdinal("template_code");
        var colWarehouseShadowId = reader.GetOrdinal("warehouse_shadow_id");
        var colWarehouseCode = reader.GetOrdinal("warehouse_code");
        var colWarehouseName = reader.GetOrdinal("warehouse_name");
        var colRemark = reader.GetOrdinal("remark");
        var colIsRfba = reader.GetOrdinal("is_rfba");
        var colDocDate = reader.GetOrdinal("doc_date");
        var colMakerName = reader.GetOrdinal("maker_name");
        var colVendorName = reader.GetOrdinal("vendor_name");
        var colIsLocked = reader.GetOrdinal("is_locked");
        var colBudgetPlanItemId = reader.GetOrdinal("budget_plan_item_id");
        var colItemShadowId = reader.GetOrdinal("item_shadow_id");
        var colItemCode = reader.GetOrdinal("item_code");
        var colActivityName = reader.GetOrdinal("activity_name");
        var colItemActivityTypeCode = reader.GetOrdinal("item_activity_type_code");
        var colItemActivityTypeDisplay = reader.GetOrdinal("item_activity_type_display");
        var colWorkOrderId = reader.GetOrdinal("work_order_id");
        var colWorkOrderCode = reader.GetOrdinal("work_order_code");
        var colWorkOrderStatus = reader.GetOrdinal("work_order_status");
        var colTotalCount = reader.GetOrdinal("total_count");

        while (await reader.ReadAsync(ct))
        {
            totalCount = reader.GetInt32(colTotalCount);
            var bpId = reader.GetInt64(colBpId);

            if (!grouped.TryGetValue(bpId, out var entry))
            {
                entry = (
                    new ApprovedBpForWoResponse(
                        bpId,
                        reader.GetString(colBpCode),
                        reader.GetString(colTemplateCode),
                        reader.GetInt64(colWarehouseShadowId),
                        reader.GetString(colWarehouseCode),
                        reader.GetString(colWarehouseName),
                        reader.IsDBNull(colRemark) ? null : reader.GetString(colRemark),
                        reader.GetBoolean(colIsRfba),
                        reader.GetDateTime(colDocDate),
                        reader.GetString(colMakerName),
                        reader.IsDBNull(colVendorName) ? null : reader.GetString(colVendorName),
                        reader.GetBoolean(colIsLocked),
                        AllSubmitted: false, // computed below once all activities are read
                        []),
                    []);
                grouped[bpId] = entry;
            }

            entry.Activities.Add(new BpActivityWoStatus(
                reader.GetInt64(colBudgetPlanItemId),
                reader.GetInt64(colItemShadowId),
                reader.GetString(colItemCode),
                reader.GetString(colActivityName),
                reader.IsDBNull(colItemActivityTypeCode) ? null : reader.GetString(colItemActivityTypeCode),
                reader.IsDBNull(colItemActivityTypeDisplay) ? null : reader.GetString(colItemActivityTypeDisplay),
                reader.IsDBNull(colWorkOrderId) ? null : reader.GetInt64(colWorkOrderId),
                reader.IsDBNull(colWorkOrderCode) ? null : reader.GetString(colWorkOrderCode),
                reader.IsDBNull(colWorkOrderStatus) ? null : reader.GetString(colWorkOrderStatus)));
        }

        return (grouped.Values
            .Select(x => x.Header with
            {
                Activities = x.Activities,
                AllSubmitted = x.Activities.All(a => a.WorkOrderStatus == WorkOrderStatus.Submitted.Value),
            })
            .ToList(), totalCount);
    }

    public Task BulkInsertAsync(IReadOnlyList<WorkOrder> workOrders, CancellationToken ct = default)
    {
        db.WorkOrders.AddRange(workOrders);
        return Task.CompletedTask;
    }

    // No UpdateAsync. The service uses GetByIdForUpdateAsync (tracked) and mutates fields directly;
    // SaveChanges emits only the actually-changed UPDATE/INSERT/DELETE statements.

    // Targeted update - avoids tracking conflict when BP items share the same vendor shadow.
    public async Task SubmitAsync(
        long id,
        long submittedByUserId,
        DateTime submittedAt,
        CancellationToken ct = default
    )
        => await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE work_orders
            SET status               = 'Submitted',
                submitted_by_user_id = {submittedByUserId},
                submitted_at         = {submittedAt},
                updated_at           = NOW()
            WHERE "Id" = {id}
            """, ct);

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
        => await db.WorkOrders
            .Where(w => w.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.DeletedAt, DateTime.UtcNow), ct);

    public Task<bool> HasActiveWorkOrderForItemAsync(long budgetPlanItemId, CancellationToken ct = default)
        => db.WorkOrders.AnyAsync(w => w.BudgetPlanItemId == budgetPlanItemId && w.DeletedAt == null, ct);

    public async Task<WorkOrderAttachmentContext?> GetForAttachmentAsync(long id, CancellationToken ct = default)
    {
        // Status is a SmartEnum; .CanBeEdited can't be translated. Project the raw status value
        // and compute editability client-side.
        var row = await db.WorkOrders
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new { w.Id, w.CompanyId, w.CreatedByUserId, Status = w.Status.Value, w.WarehouseShadowId })
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;
        var status = Domain.Enums.WorkOrderStatus.FromValue(row.Status);
        return new WorkOrderAttachmentContext(row.Id, row.CompanyId, row.CreatedByUserId, status.CanBeEdited, row.WarehouseShadowId);
    }

    public Task<WorkOrderPicContext?> GetPicContextAsync(long id, CancellationToken ct = default)
        => db.WorkOrders
            .AsNoTracking()
            .Where(w => w.Id == id && w.DeletedAt == null)
            .Select(w => new WorkOrderPicContext(w.CompanyId, w.WarehouseShadowId))
            .Cast<WorkOrderPicContext?>()
            .FirstOrDefaultAsync(ct);
}
