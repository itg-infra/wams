namespace WAMS.Infrastructure.Repositories.FinanceReports;

using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using WAMS.Application.DTOs.FinanceReports;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.FinanceReports;
using WAMS.Infrastructure.Data;

public class FinanceReportRepository(AppDbContext db, ITenantContext tenantContext) : IFinanceReportRepository
{
    public async Task<FinanceReportDetailResponse?> GetDetailAsync(
        long budgetPlanId,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default
    )
    {
        var tenantCompanyId = tenantContext.IsSet ? tenantContext.CompanyId : null;
        var tenantFilterDisabled = !tenantContext.IsSet || !tenantCompanyId.HasValue;
        var warehouseIdsArray = warehouseIds?.ToArray() ?? [];
        var warehouseFilterDisabled = warehouseIds is null;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        const string headerSql = """
            SELECT  bp."Id"                AS budget_plan_id,
                    bp.code                AS budget_no,
                    bt.code                AS template_id,
                    bp.status               AS status,
                    bp.remark              AS remark,
                    bp.doc_date            AS doc_date,
                    ws.code                AS warehouse_code,
                    ws.name                AS warehouse_name,
                    ws.location            AS location,
                    bp.company_id          AS company_id
            FROM budget_plans bp
            JOIN budget_templates bt ON bt."Id" = bp.budget_template_id
            JOIN warehouse_shadows ws ON ws."Id" = bp.warehouse_shadow_id
            WHERE bp."Id" = @p_id
              AND bp.deleted_at IS NULL
              AND (@p_tenant_disabled OR bp.company_id = @p_company_id)
              AND (@p_warehouse_disabled OR bp.warehouse_shadow_id = ANY(@p_warehouse_ids))
            """;

        await using var headerCmd = conn.CreateCommand();
        headerCmd.CommandText = headerSql;
        headerCmd.Parameters.Add(new NpgsqlParameter("p_id", NpgsqlDbType.Bigint) { Value = budgetPlanId });
        headerCmd.Parameters.Add(new NpgsqlParameter("p_tenant_disabled", NpgsqlDbType.Boolean) { Value = tenantFilterDisabled });
        headerCmd.Parameters.Add(new NpgsqlParameter("p_company_id", NpgsqlDbType.Bigint) { Value = (object?)tenantCompanyId ?? DBNull.Value });
        headerCmd.Parameters.Add(new NpgsqlParameter("p_warehouse_disabled", NpgsqlDbType.Boolean) { Value = warehouseFilterDisabled });
        headerCmd.Parameters.Add(new NpgsqlParameter("p_warehouse_ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = warehouseIdsArray });

        FinanceReportHeaderResponse header;

        await using (var reader = await headerCmd.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct))
                return null;

            header = new FinanceReportHeaderResponse(
                BudgetPlanId: reader.GetInt64(reader.GetOrdinal("budget_plan_id")),
                BudgetNo: reader.GetString(reader.GetOrdinal("budget_no")),
                TemplateId: reader.GetString(reader.GetOrdinal("template_id")),
                Status: reader.GetString(reader.GetOrdinal("status")),
                Remark: reader.IsDBNull(reader.GetOrdinal("remark")) ? null : reader.GetString(reader.GetOrdinal("remark")),
                DocDate: reader.GetDateTime(reader.GetOrdinal("doc_date")),
                WarehouseCode: reader.GetString(reader.GetOrdinal("warehouse_code")),
                WarehouseName: reader.GetString(reader.GetOrdinal("warehouse_name")),
                Location: reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")));
        }

        const string costSql = """
            SELECT  poi."Id"               AS poi_id,
                    wo.code                AS wo_code,
                    poi.bill_of_lading     AS bl_number,
                    spk.card_name          AS vessel,
                    poi.item_name          AS product,
                    pic."Fullname"         AS pic,
                    poi.is_rfba            AS is_rfba,
                    wo.start_date          AS start_date,
                    wo.end_date            AS end_date,
                    poi.total_value        AS total_price,
                    poi.ppn_tax_type_code  AS ppn_tax_type_code,
                    poi.ppn_rate           AS ppn_rate,
                    poi.ppn_amount         AS ppn_amount,
                    poi.pph_tax_type_code  AS pph_tax_type_code,
                    pph_tt.name            AS pph_type_name,
                    poi.pph_amount         AS pph_amount,
                    poi.grand_total        AS grand_total,
                    poi.payment_status     AS payment_status
            FROM purchase_order_items poi
            JOIN purchase_orders po        ON po."Id" = poi.purchase_order_id AND po.deleted_at IS NULL
            JOIN budget_plan_items bpi     ON bpi."Id" = poi.budget_plan_item_id
            LEFT JOIN work_orders wo       ON wo.budget_plan_item_id = poi.budget_plan_item_id AND wo.deleted_at IS NULL
            LEFT JOIN users pic            ON pic."Id" = wo.pic_user_id
            LEFT JOIN spk_shadows spk      ON spk."Id" = bpi.spk_shadow_id
            LEFT JOIN tax_types pph_tt     ON pph_tt.company_id = po.company_id
                                           AND pph_tt.category = 'Pph'
                                           AND pph_tt.code = poi.pph_tax_type_code
            WHERE bpi.budget_plan_id = @p_id
            ORDER BY poi."Id"
            """;

        var costDetails = new List<FinanceReportCostDetailResponse>();
        decimal dpp = 0, totalPpn = 0, totalPph = 0, grandTotal = 0;

        await using (var costCmd = conn.CreateCommand())
        {
            costCmd.CommandText = costSql;
            costCmd.Parameters.Add(new NpgsqlParameter("p_id", NpgsqlDbType.Bigint) { Value = budgetPlanId });
            await using var reader = await costCmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var totalPrice = reader.GetDecimal(reader.GetOrdinal("total_price"));
                var ppnTaxTypeCode = reader.IsDBNull(reader.GetOrdinal("ppn_tax_type_code")) ? null : reader.GetString(reader.GetOrdinal("ppn_tax_type_code"));
                var ppnRate = reader.GetDecimal(reader.GetOrdinal("ppn_rate"));
                var ppnAmount = reader.GetDecimal(reader.GetOrdinal("ppn_amount"));
                var pphTaxTypeCode = reader.IsDBNull(reader.GetOrdinal("pph_tax_type_code")) ? null : reader.GetString(reader.GetOrdinal("pph_tax_type_code"));
                var pphTypeName = reader.IsDBNull(reader.GetOrdinal("pph_type_name")) ? null : reader.GetString(reader.GetOrdinal("pph_type_name"));
                var pphAmount = reader.GetDecimal(reader.GetOrdinal("pph_amount"));
                var rowGrandTotal = reader.GetDecimal(reader.GetOrdinal("grand_total"));

                costDetails.Add(new FinanceReportCostDetailResponse(
                    PurchaseOrderItemId: reader.GetInt64(reader.GetOrdinal("poi_id")),
                    WorkOrderId: reader.IsDBNull(reader.GetOrdinal("wo_code")) ? null : reader.GetString(reader.GetOrdinal("wo_code")),
                    BlNumber: reader.IsDBNull(reader.GetOrdinal("bl_number")) ? null : reader.GetString(reader.GetOrdinal("bl_number")),
                    Vessel: reader.IsDBNull(reader.GetOrdinal("vessel")) ? null : reader.GetString(reader.GetOrdinal("vessel")),
                    Product: reader.GetString(reader.GetOrdinal("product")),
                    Pic: reader.IsDBNull(reader.GetOrdinal("pic")) ? null : reader.GetString(reader.GetOrdinal("pic")),
                    IsRfba: reader.GetBoolean(reader.GetOrdinal("is_rfba")),
                    StartDate: reader.IsDBNull(reader.GetOrdinal("start_date")) ? null : reader.GetDateTime(reader.GetOrdinal("start_date")),
                    EndDate: reader.IsDBNull(reader.GetOrdinal("end_date")) ? null : reader.GetDateTime(reader.GetOrdinal("end_date")),
                    TotalPrice: totalPrice,
                    IsPpnApplied: ppnTaxTypeCode is not null,
                    PpnRatePercent: ppnRate,
                    TotalPricePpn: ppnAmount,
                    IsPphApplied: pphTaxTypeCode is not null,
                    PphType: pphTypeName,
                    TotalPricePph: pphAmount,
                    GrandTotal: rowGrandTotal,
                    PaymentStatus: reader.GetString(reader.GetOrdinal("payment_status"))));

                dpp += totalPrice;
                totalPpn += ppnAmount;
                totalPph += pphAmount;
                grandTotal += rowGrandTotal;
            }
        }

        const string budgetSql = """
            SELECT
                COALESCE(SUM(bpi.cost_value * bpi.quantity), 0) AS budget_plan_total,
                COALESCE((
                    SELECT SUM(poi_s.cost_value * poi_s.quantity)
                    FROM purchase_order_items poi_s
                    JOIN purchase_orders po_s ON po_s."Id" = poi_s.purchase_order_id
                    JOIN budget_plan_items bpi_s ON bpi_s."Id" = poi_s.budget_plan_item_id
                    WHERE bpi_s.budget_plan_id = @p_id
                    AND po_s.deleted_at IS NULL
                ), 0) AS budget_realization
            FROM budget_plan_items bpi
            WHERE bpi.budget_plan_id = @p_id
            """;

        decimal budgetPlanTotal, budgetRealization;
        await using (var budgetCmd = conn.CreateCommand())
        {
            budgetCmd.CommandText = budgetSql;
            budgetCmd.Parameters.Add(new NpgsqlParameter("p_id", NpgsqlDbType.Bigint) { Value = budgetPlanId });
            await using var reader = await budgetCmd.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            budgetPlanTotal = reader.GetDecimal(reader.GetOrdinal("budget_plan_total"));
            budgetRealization = reader.GetDecimal(reader.GetOrdinal("budget_realization"));
        }

        var budgetRecap = new FinanceReportBudgetRecapResponse(
            BudgetPlan: budgetPlanTotal,
            BudgetRealization: budgetRealization,
            BudgetVariance: budgetPlanTotal - budgetRealization);

        return new FinanceReportDetailResponse(
            Header: header,
            CostDetails: costDetails,
            Dpp: dpp,
            TotalPpn: totalPpn,
            TotalPph: totalPph,
            GrandTotal: grandTotal,
            BudgetRecap: budgetRecap);
    }
}
