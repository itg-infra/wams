namespace WAMS.Application.Tests.Export;

using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using WAMS.Application.Export;
using WAMS.Infrastructure.Export;

public class ExportServiceTests
{
    private readonly ExportService _sut;

    public ExportServiceTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var resolver = Substitute.For<IPdfMetadataResolver>();
        resolver.ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PdfReportMetadata("Test Report", "Test Co", "TST", null, DateTime.UtcNow));
        _sut = new ExportService(resolver, Options.Create(new PdfOptions()));
    }

    private static List<ExportColumnDefinition<TestRow>> Columns =>
    [
        new("Name", x => x.Name),
        new("Amount", x => x.Amount, Format: "#,##0.00"),
        new("Date", x => x.Date, Format: "yyyy-MM-dd"),
        new("Optional", x => x.Optional),
    ];

    private static List<TestRow> Rows =>
    [
        new("Alice", 1234.56m, new DateTime(2026, 1, 15), "present"),
        new("Bob", 0m, new DateTime(2026, 6, 1), null),
    ];

    [Fact]
    public void GetContentType_Xlsx_ReturnsSpreadsheetMime()
    {
        _sut.GetContentType(ExportFormat.Xlsx)
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public void GetContentType_Csv_ReturnsCsvMime()
    {
        _sut.GetContentType(ExportFormat.Csv).Should().Be("text/csv");
    }

    [Fact]
    public void GetFileExtension_Xlsx_ReturnsXlsx()
    {
        _sut.GetFileExtension(ExportFormat.Xlsx).Should().Be("xlsx");
    }

    [Fact]
    public void GetFileExtension_Csv_ReturnsCsv()
    {
        _sut.GetFileExtension(ExportFormat.Csv).Should().Be("csv");
    }

    private async Task<string> GetCsvOutputAsync(
        List<ExportColumnDefinition<TestRow>> columns,
        IReadOnlyList<TestRow> rows)
    {
        var stream = new MemoryStream();
        await _sut.ExportAsync(stream, ExportFormat.Csv, columns, rows, "Sheet1");
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task ExportAsync_Csv_WritesHeaderAndDataRows()
    {
        var text = await GetCsvOutputAsync(Columns, Rows);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3);
        lines[0].Should().Contain("Name").And.Contain("Amount").And.Contain("Date").And.Contain("Optional");
        lines[1].Should().Contain("Alice").And.Contain("1234.56").And.Contain("2026-01-15");
        lines[2].Should().Contain("Bob").And.Contain("0.00").And.Contain("2026-06-01");
    }

    [Fact]
    public async Task ExportAsync_Csv_NullField_WritesEmptyString()
    {
        var text = await GetCsvOutputAsync(Columns, Rows);
        text.Should().NotContain("null");
    }

    [Fact]
    public async Task ExportAsync_Xlsx_OutputIsValidZipContainer()
    {
        var stream = new MemoryStream();

        await _sut.ExportAsync(stream, ExportFormat.Xlsx, Columns, Rows, "Sheet1", ct: TestContext.Current.CancellationToken);

        stream.Length.Should().BeGreaterThan(0);
        // XLSX files start with PK (zip header)
        stream.Position = 0;
        var header = new byte[2];
        await stream.ReadAsync(header, TestContext.Current.CancellationToken);
        header.Should().Equal(0x50, 0x4B); // "PK"
    }

    [Fact]
    public async Task ExportAsync_EmptyData_WritesHeaderOnly()
    {
        var stream = new MemoryStream();

        await _sut.ExportAsync(stream, ExportFormat.Csv, Columns, Array.Empty<TestRow>(), "Sheet1", ct: TestContext.Current.CancellationToken);

        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1); // header only
    }

    [Fact]
    public void GetContentType_Pdf_ReturnsPdfMime()
        => _sut.GetContentType(ExportFormat.Pdf).Should().Be("application/pdf");

    [Fact]
    public void GetFileExtension_Pdf_ReturnsPdf()
        => _sut.GetFileExtension(ExportFormat.Pdf).Should().Be("pdf");

    [Fact]
    public async Task ExportPdfReportAsync_ProducesValidPdf()
    {
        var metadata = new PdfReportMetadata("Items Report", "Acme", "ACM", null, DateTime.UtcNow);
        var stream = new MemoryStream();

        await _sut.ExportPdfReportAsync(stream, Columns, Rows, metadata, TestContext.Current.CancellationToken);

        stream.Length.Should().BeGreaterThan(0);
        stream.Position = 0;
        var header = new byte[4];
        await stream.ReadAsync(header, TestContext.Current.CancellationToken);
        header.Should().Equal(0x25, 0x50, 0x44, 0x46); // %PDF
    }

    private record TestRow(string Name, decimal Amount, DateTime Date, string? Optional);
}
