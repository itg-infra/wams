namespace WAMS.Infrastructure.Export;

using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SpreadCheetah;
using SpreadCheetah.Worksheets;
using WAMS.Application.Export;
using WAMS.Domain.Constants;

public class ExportService(IPdfMetadataResolver pdfMetadataResolver, IOptions<PdfOptions> pdfOptions) : IExportService
{
    private const string DefaultReportSuffix = " Report";
    public string GetContentType(ExportFormat format) => format switch
    {
        ExportFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ExportFormat.Csv => "text/csv",
        ExportFormat.Pdf => "application/pdf",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    public string GetFileExtension(ExportFormat format) => format switch
    {
        ExportFormat.Xlsx => "xlsx",
        ExportFormat.Csv => "csv",
        ExportFormat.Pdf => "pdf",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    public async Task ExportAsync<T>(
        Stream outputStream,
        ExportFormat format,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IReadOnlyList<T> data,
        string sheetName,
        string? pdfTitle = null,
        CancellationToken ct = default)
    {
        if (format == ExportFormat.Xlsx)
            await WriteExcelAsync(outputStream, columns, data, sheetName, ct);
        else if (format == ExportFormat.Csv)
            await WriteCsvAsync(outputStream, columns, data, ct);
        else if (format == ExportFormat.Pdf)
        {
            var metadata = await pdfMetadataResolver.ResolveAsync(pdfTitle ?? $"{sheetName}{DefaultReportSuffix}", ct);
            await ExportPdfReportAsync(outputStream, columns, data, metadata, ct);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(format));
    }

    public async Task ExportPdfReportAsync<T>(
        Stream outputStream,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IReadOnlyList<T> data,
        PdfReportMetadata metadata,
        CancellationToken ct = default)
    {
        var max = pdfOptions.Value.MaxRows;
        if (data.Count > max)
            throw new InvalidOperationException(ErrorMessages.Export.PdfMaxRowsExceeded(max));

        IDocument document = new GenericPdfDocument<T>(columns, data, metadata);
        await Task.Run(() => document.GeneratePdf(outputStream), ct);
    }

    public async Task StreamExportAsync<T>(
        Stream outputStream,
        ExportFormat format,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IAsyncEnumerable<T> data,
        string sheetName,
        string? pdfTitle = null,
        CancellationToken ct = default)
    {
        if (format == ExportFormat.Xlsx)
            await WriteExcelStreamAsync(outputStream, columns, data, sheetName, ct);
        else if (format == ExportFormat.Csv)
            await WriteCsvStreamAsync(outputStream, columns, data, ct);
        else if (format == ExportFormat.Pdf)
        {
            var max = pdfOptions.Value.MaxRows;
            var list = new List<T>(max);

            await foreach (var item in data.WithCancellation(ct))
            {
                if (list.Count >= max)
                    throw new InvalidOperationException(
                        ErrorMessages.Export.PdfMaxRowsExceeded(max));
                list.Add(item);
            }

            var metadata = await pdfMetadataResolver.ResolveAsync(pdfTitle ?? $"{sheetName}{DefaultReportSuffix}", ct);
            // QuestPDF writes synchronously. Buffer to MemoryStream first,
            // then copy to Response.Body (which requires async-only writes in Kestrel).
            using var ms = new MemoryStream();

            await ExportPdfReportAsync(ms, columns, list, metadata, ct);

            ms.Position = 0;

            await ms.CopyToAsync(outputStream, ct);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(format));
    }

    private static async Task WriteExcelStreamAsync<T>(
        Stream outputStream,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IAsyncEnumerable<T> data,
        string sheetName,
        CancellationToken ct)
    {
        var options = new SpreadCheetahOptions { DefaultDateTimeFormat = null };
        await using var spreadsheet = await Spreadsheet.CreateNewAsync(outputStream, options, ct);

        var worksheetOptions = new WorksheetOptions();
        for (var i = 0; i < columns.Count; i++)
            worksheetOptions.Column(i + 1).Width = columns[i].Width;

        await spreadsheet.StartWorksheetAsync(sheetName, worksheetOptions, ct);
        await spreadsheet.AddRowAsync(columns.Select(c => new Cell(c.Header)).ToArray(), ct);

        await foreach (var item in data.WithCancellation(ct))
        {
            var cells = columns.Select(c => ToCell(c.Accessor(item), c.Format)).ToArray();
            await spreadsheet.AddRowAsync(cells, ct);
        }

        await spreadsheet.FinishAsync(ct);
    }

    private static async Task WriteCsvStreamAsync<T>(
        Stream outputStream,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IAsyncEnumerable<T> data,
        CancellationToken ct)
    {
        await using var writer = new StreamWriter(outputStream, leaveOpen: true);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (var col in columns)
            csv.WriteField(col.Header);
        await csv.NextRecordAsync();

        await foreach (var item in data.WithCancellation(ct))
        {
            foreach (var col in columns)
                csv.WriteField(FormatCsvValue(col.Accessor(item), col.Format));
            await csv.NextRecordAsync();
        }
    }

    private static Task WriteExcelAsync<T>(
        Stream outputStream,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IReadOnlyList<T> data,
        string sheetName,
        CancellationToken ct)
        => WriteExcelStreamAsync(outputStream, columns, ToAsyncEnumerable(data, ct), sheetName, ct);

    private static Cell ToCell(object? value, string? format)
        => new(ExportFormatHelper.FormatString(value, format));

    private static string FormatCsvValue(object? value, string? format)
        => ExportFormatHelper.FormatString(value, format?.Replace("#,##", "#"));

    private static Task WriteCsvAsync<T>(
        Stream outputStream,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IReadOnlyList<T> data,
        CancellationToken ct)
        => WriteCsvStreamAsync(outputStream, columns, ToAsyncEnumerable(data, ct), ct);

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        IReadOnlyList<T> list,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in list)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}
