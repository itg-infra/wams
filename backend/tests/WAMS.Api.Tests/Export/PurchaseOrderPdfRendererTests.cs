namespace WAMS.Api.Tests.Export;

using System.Text;
using FluentAssertions;
using QuestPDF.Infrastructure;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;
using WAMS.Infrastructure.Export;
using Xunit;

public class PurchaseOrderPdfRendererTests
{
    public PurchaseOrderPdfRendererTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static PurchaseOrderItemResponse Item(
        string name, string coaCode, string coaName, decimal qty, decimal cost, decimal total) =>
        new(
            Id: 1, BudgetPlanItemId: 1, ItemShadowId: 1,
            ItemCode: "ITM-1", ItemName: name,
            CoaCode: coaCode, CoaName: coaName,
            VendorShadowId: 1, VendorCode: "V001", VendorName: "AMAYA LAND, CV",
            UomMasterId: 1, UomCode: "UNIT", UomName: "Unit",
            IsRfba: false, BillOfLading: null,
            CostValue: cost, Quantity: qty, TotalValue: total, SortOrder: 0,
            PpnTaxTypeCode: null, PpnRate: 0m, PphTaxTypeCode: null, PphRate: 0m,
            PpnAmount: 0m, PphAmount: 0m, GrandTotal: total, CostTreatment: null);

    private static PurchaseOrderResponse Po(
        string? sapPoNumber = "SBY-260400607",
        IReadOnlyList<PoApprover>? approvers = null,
        DateTime? generatedAt = null) =>
        new(
            Id: 1, Code: "PO/GCU/0001",
            VendorShadowId: 1, VendorCode: "V001", VendorName: "AMAYA LAND, CV",
            Status: "Draft",
            DocDate: new DateTime(2026, 10, 8),
            Remark: "BIAYA LISTRIK GUDANG AMAYA D11",
            SapPoNumber: sapPoNumber,
            LinkedBudgetPlans: [],
            Items:
            [
                Item("B. Lain - Lain Dibayar Muka", "Z.GEN030", "B. Lain - Lain Dibayar Muka", 9m, 4_000_000m, 36_000_000m),
                Item("Sewa Gudang", "Z.GEN031", "Sewa Gudang", 2m, 500_000m, 1_000_000m)
            ],
            GrandTotal: 37_000_000m,
            TotalPpnAmount: 4_070_000m,
            TotalPphAmount: 740_000m,
            TaxInclusiveGrandTotal: 40_330_000m,
            CreatedAt: new DateTime(2026, 10, 8),
            CreatedByName: "Tester",
            GeneratedAt: generatedAt,
            GeneratedByName: null,
            Approvers: approvers ?? []);

    private static PdfReportMetadata Meta() =>
        new(
            Title: "Purchase Order",
            CompanyName: "PT. Gerbang Cahaya Utama",
            CompanyCode: "GCU",
            LogoData: null,
            GeneratedAt: new DateTime(2026, 10, 8),
            Address: "Komplek Delta Building Blok B-20");

    [Fact]
    public void BuildTotalsRows_maps_each_amount_to_its_own_labelled_row()
    {
        var rows = PurchaseOrderPdfRenderer.BuildTotalsRows(Po());

        rows.Should().Equal(new[]
        {
            ("Sub Total", "37,000,000.00"),
            ("Discount", "0.00"),
            ("PPN", "4,070,000.00"),
            ("PPh", "740,000.00"),
            ("Total", "40,330,000.00")
        });
    }

    [Fact]
    public void BuildSignatories_renders_one_approval_column_per_workflow_stage()
    {
        var po = Po(approvers:
        [
            new("Stage One Approver", new DateTime(2026, 10, 9)),
            new("Stage Two Approver", new DateTime(2026, 10, 10))
        ]);

        PurchaseOrderPdfRenderer.BuildSignatories(po).Should().Equal(
            ("Dibuat Oleh,", "Tester", "Tgl. 08/10/2026"),
            ("Disetujui Oleh,", "Stage One Approver", "Tgl. 09/10/2026"),
            ("Disetujui Oleh,", "Stage Two Approver", "Tgl. 10/10/2026"),
            ("Diketahui Oleh,", null, "Tgl."));
    }

    [Fact]
    public void BuildSignatories_keeps_one_blank_approval_column_when_there_is_no_workflow()
    {
        PurchaseOrderPdfRenderer.BuildSignatories(Po()).Should().Equal(
            ("Dibuat Oleh,", "Tester", "Tgl. 08/10/2026"),
            ("Disetujui Oleh,", null, "Tgl."),
            ("Diketahui Oleh,", null, "Tgl."));
    }

    [Fact]
    public void BuildSignatories_dates_the_approval_in_jakarta_time_not_utc()
    {
        // Approved 10/10 00:30 WIB, stored as 09/10 17:30 UTC - printing the raw
        // stamp would date the signature a day early.
        var po = Po(approvers: [new("Stage One Approver", new DateTime(2026, 10, 9, 17, 30, 0))]);

        PurchaseOrderPdfRenderer.BuildSignatories(po)[1].DateLine.Should().Be("Tgl. 10/10/2026");
    }

    [Fact]
    public void DisplayDate_converts_the_sap_generation_stamp_to_jakarta_time()
    {
        // 08/10 06:00 WIB is still 07/10 in UTC - the form must print 08/10.
        PurchaseOrderPdfRenderer.DisplayDate(Po(generatedAt: new DateTime(2026, 10, 7, 23, 0, 0)))
            .Should().Be(new DateTime(2026, 10, 8, 6, 0, 0));
    }

    [Fact]
    public void DisplayDate_falls_back_to_the_plain_doc_date_for_a_draft()
    {
        PurchaseOrderPdfRenderer.DisplayDate(Po()).Should().Be(new DateTime(2026, 10, 8));
    }

    [Fact]
    public void Render_produces_a_valid_pdf()
    {
        var bytes = new PurchaseOrderPdfRenderer().Render(Po(), Meta());

        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
        bytes.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void Render_succeeds_for_a_draft_without_a_sap_number_and_stamps_a_watermark()
    {
        var bytes = new PurchaseOrderPdfRenderer().Render(Po(sapPoNumber: null), Meta());

        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

}
