namespace WAMS.Application.Export;

public record PdfReportMetadata(
    string Title,
    string CompanyName,
    string CompanyCode,
    byte[]? LogoData,
    DateTime GeneratedAt,
    string? Address = null
);
