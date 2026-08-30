namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.FinanceReports;

public static class FinanceReportExportColumns
{
    public static List<ExportColumnDefinition<FinanceReportCostDetailResponse>> Columns =>
    [
        new("Work Order", x => x.WorkOrderId),
        new("BL Number", x => x.BlNumber),
        new("Vessel", x => x.Vessel),
        new("Product", x => x.Product),
        new("PIC", x => x.Pic),
        new("Is RFBA", x => x.IsRfba),
        new("Start Date", x => x.StartDate, Format: "yyyy-MM-dd"),
        new("End Date", x => x.EndDate, Format: "yyyy-MM-dd"),
        new("Total Price", x => x.TotalPrice),
        new("PPN Applied", x => x.IsPpnApplied),
        new("PPN Rate %", x => x.PpnRatePercent),
        new("Total PPN", x => x.TotalPricePpn),
        new("PPH Applied", x => x.IsPphApplied),
        new("PPH Type", x => x.PphType),
        new("Total PPH", x => x.TotalPricePph),
        new("Grand Total", x => x.GrandTotal),
        new("Payment Status", x => x.PaymentStatus),
    ];
}
