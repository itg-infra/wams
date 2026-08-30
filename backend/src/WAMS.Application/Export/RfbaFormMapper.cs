namespace WAMS.Application.Export;

using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.DTOs.Rfba;
using WAMS.Domain.Constants;
using WAMS.Domain.Enums;

/// <summary>
/// Budget plan to printable RFBA forms. This is the only anchor-aware code in the
/// RFBA export: if the form is later re-anchored to PO or AP, add a sibling method
/// here and the renderer does not change.
/// </summary>
public static class RfbaFormMapper
{
    public static RfbaFormDocument FromRecapPurchaseOrder(RecapPurchaseOrderDetailResponse po)
    {
        var pages = po.Items
            .Where(i => i.IsRfba)
            .GroupBy(i => string.IsNullOrWhiteSpace(i.BillOfLading) ? null : i.BillOfLading.Trim())
            .OrderBy(g => g.Key is null)
            .ThenBy(g => g.Min(i => i.Id))
            .Select(g => new RfbaFormPage(
                po.Code,
                null,
                g.Key,
                null,
                null,
                po.DocDate,
                [.. g.OrderBy(i => i.SortOrder)
                    .Select(i => new RfbaFormRow(
                        i.ItemName,
                        i.Quantity,
                        i.UomName,
                        i.CostValue,
                        i.TotalValue
                    ))
                ],
                g.Sum(i => i.TotalValue),
                po.VendorName,
                null,
                null))
            .ToList();

        return new RfbaFormDocument(pages, false, po.CreatedByName, po.CreatedAt, []);
    }

    public static RfbaFormDocument FromBudgetPlan(BudgetPlanResponse bp)
    {
        var pages = bp.Items
            .Where(i => i.IsRfba)
            .GroupBy(i => string.IsNullOrWhiteSpace(i.BillOfLading) ? null : i.BillOfLading.Trim())
            // Items with no BL still carry money, so they get their own page rather
            // than being dropped - last, after every real BL.
            .OrderBy(g => g.Key is null)
            .ThenBy(g => g.Min(i => i.SortOrder))
            .Select(g => new RfbaFormPage(
                RfbaId: bp.BudgetNo,
                Produk: bp.SpkItems.FirstOrDefault(s => s.BlNo == g.Key)?.ItemName,
                BillOfLading: g.Key,
                Vessel: null,
                AreaGudang: bp.WarehouseName,
                DocDate: bp.DocDate,
                Rows:
                [
                    .. g.OrderBy(i => i.SortOrder)
                        .Select(i => new RfbaFormRow(i.CostName, i.Quantity, i.UomName, i.CostValue, i.TotalValue))
                ],
                // The form has no PPN/PPh/discount rows - the printed total is the
                // plain sum of the line totals. Verified against the client sample.
                Total: g.Sum(i => i.TotalValue),
                PayeeName: null,
                PayeeAccountNumber: null,
                PayeeBank: null))
            .ToList();

        // Every stage, in order - the form prints one "Disetujui Oleh" column each.
        // A stage that is Pending or Rejected carries no name, so a plan mid-workflow
        // prints the columns it has earned and leaves the rest blank.
        IReadOnlyList<RfbaApprover> approvers =
        [
            .. bp.Approval.Stages
                .OrderBy(s => s.StageOrder)
                .Select(s => s.Status == WorkflowStageStatus.Approved
                    ? new RfbaApprover(s.ApprovedByName, s.ApprovedAt)
                    : new RfbaApprover(null, null))
        ];

        return new RfbaFormDocument(
            pages,
            bp.Status != BudgetPlanStatus.Approved.Value,
            bp.CreatedByName,
            bp.CreatedAt,
            approvers);
    }
}
