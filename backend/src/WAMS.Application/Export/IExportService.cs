namespace WAMS.Application.Export;

public enum ExportFormat
{
    Xlsx,
    Csv,
    Pdf
}

public interface IExportService
{
    Task ExportAsync<T>(
        Stream outputStream,
        ExportFormat format,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IReadOnlyList<T> data,
        string sheetName,
        string? pdfTitle = null,
        CancellationToken ct = default
    );

    Task StreamExportAsync<T>(
        Stream outputStream,
        ExportFormat format,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        IAsyncEnumerable<T> data,
        string sheetName,
        string? pdfTitle = null,
        CancellationToken ct = default);

    string GetContentType(ExportFormat format);
    string GetFileExtension(ExportFormat format);
}
