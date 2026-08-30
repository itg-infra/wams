namespace WAMS.Application.Export;

public interface IPdfMetadataResolver
{
    Task<PdfReportMetadata> ResolveAsync(string title, CancellationToken ct = default);
}
