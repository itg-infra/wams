namespace WAMS.Application.Tests.Export;

using FluentAssertions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Xunit;
using WAMS.Application.Export;
using WAMS.Infrastructure.Export;

public class GenericPdfDocumentTests
{
    public GenericPdfDocumentTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static List<ExportColumnDefinition<Row>> Columns =>
    [
        new("Name", x => x.Name),
        new("Amount", x => x.Amount, Format: "#,##0.00"),
    ];

    private static PdfReportMetadata Metadata => new(
        Title: "Test Report",
        CompanyName: "Acme Corp",
        CompanyCode: "ACM",
        LogoData: null,
        GeneratedAt: new DateTime(2026, 6, 8, 10, 0, 0, DateTimeKind.Utc)
    );

    [Fact]
    public void GeneratePdf_ProducesValidPdfBytes()
    {
        var rows = new List<Row> { new("Alice", 1500m), new("Bob", 0m) };
        IDocument doc = new GenericPdfDocument<Row>(Columns, rows, Metadata);

        using var stream = new MemoryStream();
        doc.GeneratePdf(stream);

        stream.Length.Should().BeGreaterThan(0);
        stream.Position = 0;
        var header = new byte[4];
        stream.Read(header);
        // PDF files start with "%PDF"
        header.Should().Equal(0x25, 0x50, 0x44, 0x46);
    }

    [Fact]
    public void GeneratePdf_EmptyData_ProducesValidPdf()
    {
        IDocument doc = new GenericPdfDocument<Row>(Columns, [], Metadata);

        using var stream = new MemoryStream();
        doc.GeneratePdf(stream);

        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GeneratePdf_WithLogoData_ProducesValidPdf()
    {
        // Minimal 1×1 red PNG
        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwADhQGAWjR9awAAAABJRU5ErkJggg==");
        var metadata = Metadata with { LogoData = pngBytes };
        IDocument doc = new GenericPdfDocument<Row>(Columns, [], metadata);

        using var stream = new MemoryStream();
        var act = () => doc.GeneratePdf(stream);
        act.Should().NotThrow();
    }

    private record Row(string Name, decimal Amount);
}
