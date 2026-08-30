namespace WAMS.Application.Interfaces.Common;

/// <summary>
/// Abstraction over SAP PO/AP creation.
/// Switch implementations via ErpApi:UseMockSap in appsettings.json.
/// </summary>
public interface ISapApiClient
{
    Task<SapCreatePoResult?> CreatePurchaseOrderAsync(SapCreatePoRequest request, CancellationToken ct = default);
    Task<SapCreateApdpResult?> CreateApDownPaymentAsync(SapCreateApdpRequest request, CancellationToken ct = default);
    Task<SapCreateApInvoiceResult?> CreateApInvoiceAsync(SapCreateApInvoiceRequest request, CancellationToken ct = default);
}

public record SapCreatePoRequest(
    string PoCode,
    string VendorCode,
    DateTime DocDate,
    string? Remark,
    List<SapPoLineItem> Items);

/// <summary>
/// SkuItemCode = commodity SKU (from Spk.ItemCode), used for SAP's OCR cost-center lookup -
/// client-confirmed only the SKU is valid here, never the billed ItemCode. Null if no linked SPK;
/// ResolveCostCenterAsync then skips the lookup and uses its Product/Division placeholders.
/// </summary>
public record SapPoLineItem(
    string ItemCode,
    string ItemDescription,
    decimal Quantity,
    decimal UnitPrice,
    string? WarehouseCode,
    string? TaxCode,
    string? BillOfLading = null,
    string? SkuItemCode = null);

/// <summary>
/// SapPoNumber = SAP's DocNum if the wrapper returns one, else DocEntry as a string
/// </summary>
public record SapCreatePoResult(string SapPoNumber, int SapDocEntry);

/// <summary>
/// PpnTaxTypeCode = VAT (TaxCode); PphTaxTypeCode drives withholding tax (IsWhTax/BuildWhTaxLines).
/// </summary>
public record SapApLineItem(
    string ItemCode,
    string ItemDescription,
    string CoaCode,
    decimal UnitCount,
    decimal UnitCost,
    string UomCode,
    decimal BudgetPlanTotal,
    decimal BudgetRealization,
    string? PpnTaxTypeCode,
    string? PphTaxTypeCode,
    decimal? DiscountPercent,
    string? WarehouseCode = null,
    string? BillOfLading = null,
    string? SkuItemCode = null,
    int? BaseEntry = null,
    int? BaseLine = null
);

public record SapCreateApdpRequest(
    string ApCode,
    string VendorCode,
    DateTime DocDate,
    string? Remark,
    List<SapApLineItem> Items);

public record SapCreateApdpResult(int SapDocEntry);

public record SapWhTaxLine(string WtCode, decimal TaxableAmount);

public record SapCreateApInvoiceRequest(
    string ApCode,
    string VendorCode,
    DateTime DocDate,
    string? Remark,
    List<SapApLineItem> Items,
    List<SapWhTaxLine>? WhTax,
    int? ApdpDocEntry,
    decimal? DrawAmount);

public record SapCreateApInvoiceResult(string SapApNumber, int SapDocEntry);