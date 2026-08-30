namespace WAMS.Api.Tests.Export;

using System.Text;
using FluentAssertions;
using QuestPDF.Infrastructure;
using WAMS.Application.DTOs.Rfba;
using WAMS.Application.Export;
using WAMS.Infrastructure.Export;
using Xunit;

public class RfbaFormPdfRendererTests
{
    public RfbaFormPdfRendererTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static RfbaFormPage Page(string? bl, string? produk, decimal total) =>
        new(
            RfbaId: "BP-2602000012",
            Produk: produk,
            BillOfLading: bl,
            Vessel: null,
            AreaGudang: "Medan - Agung",
            DocDate: new DateTime(2026, 2, 3),
            Rows:
            [
                new RfbaFormRow("Bongkar curah container 40 ft", 9m, "Kontainer", 675_000m, 6_075_000m),
                new RfbaFormRow("Bongkar rebagging in container 40 ft", 1m, "Kontainer", 1_240_000m, 1_240_000m),
                new RfbaFormRow("Lintas timbang container", 287_000m, "Kg", 2m, 574_000m)
            ],
            Total: total,
            PayeeName: null,
            PayeeAccountNumber: null,
            PayeeBank: null);

    private static PdfReportMetadata Meta() =>
        new(
            Title: "RFBA",
            CompanyName: "PT. Gerbang Cahaya Utama",
            CompanyCode: "GCU",
            LogoData: null,
            GeneratedAt: new DateTime(2026, 2, 3),
            Address: "Komplek Delta Building Blok B 20");

    [Fact]
    public void Render_produces_a_valid_pdf()
    {
        var doc = new RfbaFormDocument(
            [Page("SSZI711911", "DDGS Brazil", 7_889_000m)],
            IsDraft: false,
            MakerName: "M Ridwan Nasution",
            MakerDate: new DateTime(2026, 2, 3),
            Approvers: [new RfbaApprover("Budi Santoso", new DateTime(2026, 2, 4))]);

        var bytes = new RfbaFormPdfRenderer().Render(doc, Meta());

        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
        bytes.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void Render_handles_multiple_pages_and_a_draft_watermark()
    {
        var doc = new RfbaFormDocument(
            [
                Page("SSZI711911", "DDGS Brazil", 7_889_000m),
                Page("MEDUJM026632", "SBM Bolivia", 18_690_000m)
            ],
            IsDraft: true,
            MakerName: "M Ridwan Nasution",
            MakerDate: new DateTime(2026, 2, 3),
            Approvers: [new RfbaApprover("Andi", new DateTime(2026, 2, 4)), new RfbaApprover(null, null)]);

        var bytes = new RfbaFormPdfRenderer().Render(doc, Meta());

        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void Render_handles_a_page_with_no_bl_or_produk()
    {
        var doc = new RfbaFormDocument(
            [Page(null, null, 750_000m)],
            IsDraft: true,
            MakerName: null,
            MakerDate: null,
            Approvers: []);

        var bytes = new RfbaFormPdfRenderer().Render(doc, Meta());

        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void BuildSignatories_renders_one_approval_column_per_workflow_stage()
    {
        var doc = new RfbaFormDocument(
            [Page("SSZI711911", "DDGS Brazil", 7_889_000m)],
            IsDraft: true,
            MakerName: "M Ridwan Nasution",
            MakerDate: new DateTime(2026, 2, 3),
            Approvers:
            [
                new RfbaApprover("Andi", new DateTime(2026, 2, 4)),
                new RfbaApprover(null, null)
            ]);

        RfbaFormPdfRenderer.BuildSignatories(doc).Should().Equal(
            ("Dibuat oleh,", "M Ridwan Nasution", "Tgl. 03/02/2026"),
            ("Disetujui Oleh,", "Andi", "Tgl. 04/02/2026"),
            ("Disetujui Oleh,", null, "Tgl."),
            ("Mengetahui", null, "Tgl."));
    }

    [Fact]
    public void BuildSignatories_keeps_one_blank_approval_column_when_there_is_no_workflow()
    {
        var doc = new RfbaFormDocument(
            [Page(null, null, 750_000m)],
            IsDraft: true,
            MakerName: null,
            MakerDate: null,
            Approvers: []);

        RfbaFormPdfRenderer.BuildSignatories(doc).Should().Equal(
            ("Dibuat oleh,", null, "Tgl."),
            ("Disetujui Oleh,", null, "Tgl."),
            ("Mengetahui", null, "Tgl."));
    }

    [Fact]
    public void BuildSignatories_dates_the_approval_in_jakarta_time_not_utc()
    {
        // Approved 04/02 00:30 WIB, stored as 03/02 17:30 UTC.
        var doc = new RfbaFormDocument(
            [Page("SSZI711911", "DDGS Brazil", 7_889_000m)],
            IsDraft: false,
            MakerName: "M Ridwan Nasution",
            MakerDate: new DateTime(2026, 2, 3),
            Approvers: [new RfbaApprover("Andi", new DateTime(2026, 2, 3, 17, 30, 0))]);

        RfbaFormPdfRenderer.BuildSignatories(doc)[1].DateLine.Should().Be("Tgl. 04/02/2026");
    }

    [Fact]
    public void BuildFillerRowCount_pads_short_tables_to_the_reference_grid_height()
    {
        RfbaFormPdfRenderer.BuildFillerRowCount(3).Should().Be(10);
        RfbaFormPdfRenderer.BuildFillerRowCount(13).Should().Be(0);
        RfbaFormPdfRenderer.BuildFillerRowCount(20).Should().Be(0);
    }
}
