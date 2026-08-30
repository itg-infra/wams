namespace WAMS.Infrastructure.Repositories.Rca;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WAMS.Application.DTOs.Rca;
using WAMS.Application.Interfaces.Rca;
using WAMS.Application.Interfaces.Users;
using WAMS.Infrastructure.Data;

public class RcaRepository(
    AppDbContext db,
    IUserRepository userRepo) : IRcaRepository
{
    public async Task<RcaRepoData> GetDataAsync(
        string warehouseCode,
        DateTime dateFrom,
        DateTime dateTo,
        IReadOnlyList<long>? warehouseIds,
        long? companyId,
        CancellationToken ct = default
    )
    {
        var whIds = warehouseIds?.ToArray() ?? [];
        var whFilterDisabled = warehouseIds is null;
        var companyFilterDisabled = companyId is null;

        await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        var lines = await GetLinesAsync(
            conn,
            warehouseCode,
            dateFrom,
            dateTo,
            whIds,
            whFilterDisabled,
            companyId,
            companyFilterDisabled,
            ct
        );
        var posTotals = await GetPosTotalsAsync(
            conn,
            warehouseCode,
            dateFrom,
            dateTo,
            whIds,
            whFilterDisabled,
            companyId,
            companyFilterDisabled,
            ct
        );
        var signatures = await GetSignatureNamesAsync(
            conn,
            userRepo,
            warehouseCode,
            dateFrom,
            dateTo,
            whIds,
            whFilterDisabled,
            companyId,
            companyFilterDisabled,
            ct
        );
        var location = await GetWarehouseLocationAsync(conn, warehouseCode, companyId, companyFilterDisabled, ct);

        return new RcaRepoData(lines, posTotals, signatures, location);
    }

    private static async Task<List<RcaLineItem>> GetLinesAsync(
        NpgsqlConnection conn,
        string warehouseCode,
        DateTime dateFrom,
        DateTime dateTo,
        long[] whIds,
        bool whFilterDisabled,
        long? companyId,
        bool companyFilterDisabled,
        CancellationToken ct
    )
    {
        const string sql = """
            SELECT
                wo.start_date::date                 AS activity_date,
                item.acct_code                      AS coa_code,
                bpi.bill_of_lading,
                item.item_code                      AS pos_biaya_code,
                COALESCE(at.name, wo.activity_type_code, '') AS tipe_operasional,
                COALESCE(wli.product_name, '')      AS product_name,
                COALESCE(wli.gross_weight, bpi.quantity, 0) AS quantity,
                COALESCE(wli.uom_code, uom.code, '') AS uom_code,
                item.item_name                      AS keterangan_pos_biaya,
                bpi.description                     AS notes,
                bpi.total_value                     AS amount_rupiah
            FROM budget_plan_items bpi
            JOIN budget_plans bp   ON bp."Id"  = bpi.budget_plan_id
            JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
            JOIN item_shadows item ON item."Id" = bpi.item_shadow_id
            JOIN uom_masters uom  ON uom."Id"  = bpi.uom_master_id
            LEFT JOIN activity_types at ON at."Id" = bpi.activity_type_id AND at.deleted_at IS NULL
            LEFT JOIN work_orders wo
                ON  wo.budget_plan_item_id = bpi."Id"
                AND wo.deleted_at IS NULL
                AND wo.status = 'Submitted'
            LEFT JOIN LATERAL (
                SELECT product_name, gross_weight, uom_code
                FROM work_order_loading_items
                WHERE work_order_id = wo."Id"
                ORDER BY sort_order
                LIMIT 1
            ) wli ON wo."Id" IS NOT NULL
            WHERE ws.code = @p_wh_code
              AND (@p_wh_disabled OR ws."Id" = ANY(@p_wh_ids))
              AND (@p_company_disabled OR ws."CompanyId" = @p_company_id)
              AND wo.start_date >= @p_date_from
              AND wo.start_date <= @p_date_to
              AND bp.deleted_at IS NULL
            ORDER BY wo.start_date, item.item_code
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_code", NpgsqlDbType.Text) { Value = warehouseCode });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_disabled", NpgsqlDbType.Boolean) { Value = whFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = whIds });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_disabled", NpgsqlDbType.Boolean) { Value = companyFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)companyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = dateFrom });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = dateTo });

        var lines = new List<RcaLineItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colDate = reader.GetOrdinal("activity_date");
        var colCoa = reader.GetOrdinal("coa_code");
        var colBl = reader.GetOrdinal("bill_of_lading");
        var colPos = reader.GetOrdinal("pos_biaya_code");
        var colTipe = reader.GetOrdinal("tipe_operasional");
        var colProduct = reader.GetOrdinal("product_name");
        var colQty = reader.GetOrdinal("quantity");
        var colUom = reader.GetOrdinal("uom_code");
        var colKet = reader.GetOrdinal("keterangan_pos_biaya");
        var colNotes = reader.GetOrdinal("notes");
        var colAmount = reader.GetOrdinal("amount_rupiah");

        while (await reader.ReadAsync(ct))
        {
            lines.Add(new RcaLineItem(
                ActivityDate: DateOnly.FromDateTime(reader.GetDateTime(colDate)),
                CoaCode: reader.GetString(colCoa),
                BillOfLading: reader.IsDBNull(colBl) ? null : reader.GetString(colBl),
                PosBiayaCode: reader.GetString(colPos),
                TipeOperasional: reader.GetString(colTipe),
                ProductName: reader.GetString(colProduct),
                Quantity: reader.GetDecimal(colQty),
                UomCode: reader.GetString(colUom),
                KeteranganPosBiaya: reader.GetString(colKet),
                Notes: reader.IsDBNull(colNotes) ? null : reader.GetString(colNotes),
                AmountRupiah: reader.GetDecimal(colAmount)));
        }

        return lines;
    }

    private static async Task<List<PosBiayaTotal>> GetPosTotalsAsync(
        NpgsqlConnection conn,
        string warehouseCode,
        DateTime dateFrom,
        DateTime dateTo,
        long[] whIds,
        bool whFilterDisabled,
        long? companyId,
        bool companyFilterDisabled,
        CancellationToken ct
    )
    {
        // The pos-biaya catalog is NOT "all active item_shadows" - item_shadows is a full ERP mirror
        // (~200 rows) and is_active only means "still present in the last ERP sync", not "is a cost bucket".
        // A pos biaya is simply an item that gets used in budgeting, so the catalog is restricted to
        // item_shadows actually referenced by a budget_plan_item (company-scoped via item.company_id).
        // The amounts (agg) stay warehouse + date scoped; catalog rows with no spend this period show 0.
        const string sql = """
            SELECT
                item.item_code  AS pos_biaya_code,
                item.item_name  AS pos_biaya_name,
                COALESCE(agg.total, 0) AS total
            FROM item_shadows item
            JOIN (
                SELECT DISTINCT bpi.item_shadow_id
                FROM budget_plan_items bpi
                JOIN budget_plans bp ON bp."Id" = bpi.budget_plan_id
                WHERE bp.deleted_at IS NULL
            ) used ON used.item_shadow_id = item."Id"
            LEFT JOIN (
                SELECT bpi.item_shadow_id, SUM(bpi.total_value) AS total
                FROM budget_plan_items bpi
                JOIN budget_plans bp   ON bp."Id"  = bpi.budget_plan_id
                JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
                LEFT JOIN work_orders wo
                    ON  wo.budget_plan_item_id = bpi."Id"
                    AND wo.deleted_at IS NULL
                    AND wo.status = 'Submitted'
                WHERE ws.code = @p_wh_code
                  AND (@p_wh_disabled OR ws."Id" = ANY(@p_wh_ids))
                  AND (@p_company_disabled OR ws."CompanyId" = @p_company_id)
                  AND wo.start_date >= @p_date_from
                  AND wo.start_date <= @p_date_to
                  AND bp.deleted_at IS NULL
                GROUP BY bpi.item_shadow_id
            ) agg ON agg.item_shadow_id = item."Id"
            WHERE (@p_company_disabled OR item.company_id = @p_company_id)
            ORDER BY item.item_code
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_code", NpgsqlDbType.Text) { Value = warehouseCode });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_disabled", NpgsqlDbType.Boolean) { Value = whFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = whIds });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_disabled", NpgsqlDbType.Boolean) { Value = companyFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)companyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = dateFrom });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = dateTo });

        var results = new List<PosBiayaTotal>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colCode = reader.GetOrdinal("pos_biaya_code");
        var colName = reader.GetOrdinal("pos_biaya_name");
        var colTotal = reader.GetOrdinal("total");

        while (await reader.ReadAsync(ct))
        {
            results.Add(new PosBiayaTotal(
                reader.GetString(colCode),
                reader.GetString(colName),
                reader.GetDecimal(colTotal)));
        }

        return results;
    }

    private static async Task<RcaSignatures> GetSignatureNamesAsync(
        NpgsqlConnection conn,
        IUserRepository userRepo,
        string warehouseCode,
        DateTime dateFrom,
        DateTime dateTo,
        long[] whIds,
        bool whFilterDisabled,
        long? companyId,
        bool companyFilterDisabled,
        CancellationToken ct
    )
    {
        // Pull approval stage names from the most recently submitted budget plan
        // whose work orders fall in the date range for this warehouse.
        const string sql = """
            WITH target_bp AS (
                SELECT bp."Id", bp.workflow_instance_id, bp.submitted_by_user_id
                FROM budget_plans bp
                JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
                WHERE ws.code = @p_wh_code
                  AND (@p_wh_disabled OR ws."Id" = ANY(@p_wh_ids))
                  AND (@p_company_disabled OR ws."CompanyId" = @p_company_id)
                  AND bp.deleted_at IS NULL
                  AND EXISTS (
                      SELECT 1 FROM work_orders wo2
                      WHERE wo2.budget_plan_id = bp."Id"
                        AND wo2.deleted_at IS NULL
                        AND wo2.status = 'Submitted'
                        AND wo2.start_date >= @p_date_from
                        AND wo2.start_date <= @p_date_to
                  )
                ORDER BY bp.submitted_at DESC NULLS LAST
                LIMIT 1
            )
            SELECT wis.stage_order, u."Fullname" AS approved_by_name, bp.submitted_by_user_id
            FROM workflow_instance_stages wis
            JOIN workflow_instances wi ON wi."Id" = wis.workflow_instance_id
            JOIN target_bp bp ON bp.workflow_instance_id = wi."Id"
            LEFT JOIN users u ON u."Id" = wis.approved_by_user_id
            ORDER BY wis.stage_order ASC
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_code", NpgsqlDbType.Text) { Value = warehouseCode });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_disabled", NpgsqlDbType.Boolean) { Value = whFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_wh_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = whIds });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_disabled", NpgsqlDbType.Boolean) { Value = companyFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)companyId ?? DBNull.Value });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_from", NpgsqlDbType.TimestampTz) { Value = dateFrom });
        cmd.Parameters.Add(new NpgsqlParameter("p_date_to", NpgsqlDbType.TimestampTz) { Value = dateTo });

        var stageNames = new List<string?>();
        long? submittedByUserId = null;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var colName = reader.GetOrdinal("approved_by_name");
        var colSubmittedBy = reader.GetOrdinal("submitted_by_user_id");

        while (await reader.ReadAsync(ct))
        {
            stageNames.Add(reader.IsDBNull(colName) ? null : reader.GetString(colName));
            if (!submittedByUserId.HasValue && !reader.IsDBNull(colSubmittedBy))
                submittedByUserId = reader.GetInt64(colSubmittedBy);
        }

        // Maker ("Dibuat oleh") + one approver ("Disetujui oleh") per workflow
        // stage, in StageOrder. The number of approvers is dynamic per company:
        // the SQL above already returns every stage of the budget plan's workflow
        // instance, so a 1-stage workflow yields one approver and a 2-stage
        // workflow yields two. The fixed "Diketahui oleh" block is rendered by
        // RcaPdfRenderer and is not resolved here.
        string? maker = null;
        if (submittedByUserId.HasValue)
        {
            var bpMaker = await userRepo.GetByIdAsync(submittedByUserId.Value, ct);
            maker = bpMaker?.Fullname;
        }

        return new RcaSignatures(maker, stageNames);
    }

    private static async Task<string?> GetWarehouseLocationAsync(
        NpgsqlConnection conn,
        string warehouseCode,
        long? companyId,
        bool companyFilterDisabled,
        CancellationToken ct
    )
    {
        const string sql = """
            SELECT location FROM warehouse_shadows
            WHERE code = @p_code
              AND (@p_company_disabled OR "CompanyId" = @p_company_id)
            LIMIT 1
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.Add(new NpgsqlParameter("p_code", NpgsqlDbType.Text) { Value = warehouseCode });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_disabled", NpgsqlDbType.Boolean) { Value = companyFilterDisabled });
        cmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)companyId ?? DBNull.Value });

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is DBNull or null ? null : result.ToString();
    }
}
