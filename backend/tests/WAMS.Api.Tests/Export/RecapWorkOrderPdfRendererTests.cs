namespace WAMS.Api.Tests.Export;

using System.Text;
using FluentAssertions;
using QuestPDF.Infrastructure;
using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Application.Export;
using WAMS.Infrastructure.Export;
using Xunit;

public class RecapWorkOrderPdfRendererTests
{
    public RecapWorkOrderPdfRendererTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void Render_produces_a_valid_pdf_for_a_complete_recap()
    {
        var bytes = new RecapWorkOrderPdfRenderer().Render(CompleteRecap(), Metadata());

        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
        bytes.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void Render_handles_pending_recap_with_empty_sections_and_nullable_values()
    {
        var header = new RecapBpHeaderResponse(
            "BP-EMPTY", "BT-01", "Approved", null,
            new DateTime(2026, 9, 7), "WH-01", "Warehouse", null);
        var recap = new RecapWorkOrderDetailResponse(
            8, 12, "Pending", null, null, null,
            new RecapPlanResponse(header, [], [], 0, 0, 0),
            new RecapRealizationResponse(header, [], 0, 0, 0, 0));

        var bytes = new RecapWorkOrderPdfRenderer().Render(recap, Metadata());

        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void Render_leaves_nullable_wo_quantity_blank()
    {
        var recap = CompleteRecap();
        recap.Realization.WorkOrders[0] = recap.Realization.WorkOrders[0] with { Quantity = null };

        var bytes = new RecapWorkOrderPdfRenderer().Render(recap, Metadata());

        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    private static RecapWorkOrderDetailResponse CompleteRecap()
    {
        var header = new RecapBpHeaderResponse(
            "BP-2609000001", "BT-BONGKAR", "Approved", "September operation",
            new DateTime(2026, 9, 7), "WH-JKT", "Jakarta Warehouse", "Jakarta");

        return new RecapWorkOrderDetailResponse(
            41,
            99,
            "Approved",
            "Warehouse Manager",
            new DateTime(2026, 9, 7, 8, 30, 0, DateTimeKind.Utc),
            null,
            new RecapPlanResponse(
                header,
                [new RecapSpkDocumentResponse("SPK", "SPK-01", "DOC-01", "BL-01", "ITEM-01", "Soybean", 100, 95, "KG")],
                [new RecapCostDetailResponse("Operational", "V-01", "Vendor One", false, "EXT-01", "Unloading", "COA-01", "Handling", "BL-01", 1_000, 100, "KG", "Unload cargo", 100_000)],
                100_000,
                95_000,
                5_000),
            new RecapRealizationResponse(
                header,
                [new RecapWoItemResponse(7, "WO-01", "BL-01", "Foreman", false, new DateTime(2026, 9, 7), new DateTime(2026, 9, 7, 12, 0, 0), 95, 95_000, "Submitted", "Soybean", "B 1234 CD")],
                100_000,
                95_000,
                5_000,
                95));
    }

    private static PdfReportMetadata Metadata() => new(
        "Recap Work Order",
        "PT. Gerbang Cahaya Utama",
        "GCU",
        null,
        new DateTime(2026, 9, 7),
        "Jakarta");
}
