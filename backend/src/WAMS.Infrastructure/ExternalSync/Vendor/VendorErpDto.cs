namespace WAMS.Infrastructure.ExternalSync.Vendor;

/// <summary>
/// Maps the JSON response from GET /WAMS/LkVendor
/// Field names must exactly match the ERP response casing.
/// </summary>
public record VendorErpDto(string CardCode, string CardName);
