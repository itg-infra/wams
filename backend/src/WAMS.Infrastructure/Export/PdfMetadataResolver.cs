namespace WAMS.Infrastructure.Export;

using Microsoft.Extensions.Logging;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Files;
using WAMS.Domain.Exceptions;

public class PdfMetadataResolver(
    ITenantContext tenantContext,
    ICompanyRepository companyRepository,
    IFileAttachmentStorage storage,
    ILogger<PdfMetadataResolver> logger) : IPdfMetadataResolver
{
    public async Task<PdfReportMetadata> ResolveAsync(string title, CancellationToken ct = default)
    {
        var companyName = "System";
        var companyCode = "SYS";
        string? companyAddress = null;
        byte[]? logoData = null;

        if (tenantContext.IsSet && tenantContext.CompanyId.HasValue)
        {
            var company = await companyRepository.GetByIdAsync(tenantContext.CompanyId.Value, ct);

            companyName = company?.Name ?? "System";
            companyCode = company?.Code ?? "SYS";
            companyAddress = company?.Address;

            if (company?.LogoStorageKey is not null) logoData = await TryReadLogoBytesAsync(company.LogoStorageKey, ct);
        }

        return new PdfReportMetadata(
            Title: title,
            CompanyName: companyName,
            CompanyCode: companyCode,
            LogoData: logoData,
            GeneratedAt: DateTime.UtcNow,
            Address: companyAddress
        );
    }

    private async Task<byte[]?> TryReadLogoBytesAsync(string storageKey, CancellationToken ct)
    {
        try
        {
            var stored = await storage.OpenReadAsync(storageKey, ct);
            await using var stream = stored.Content;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (NotFoundException)
        {
            logger.LogWarning("Logo file not found in storage for key {StorageKey}; rendering PDF without logo", storageKey);
            return null;
        }
    }
}
