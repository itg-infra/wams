namespace WAMS.Infrastructure.ExternalSap;

using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Domain.Exceptions;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;

/// <summary>
/// Production SAP client (openapi/sap.json), enabled via ErpApi:UseMockSap=false. No HTTP retry
/// policy - retrying a create-POST risks duplicate SAP docs. Writes target ErpApi:SapEntity; the
/// Lk* cost-center/project lookups instead use the request's own tenant company code.
/// </summary>
public class SapApiClient(
    HttpClient http,
    IConfiguration configuration,
    ILogger<SapApiClient> logger,
    ErpApiClient erp,
    ITenantContext tenantContext,
    ICompanyRepository companyRepo
) : ISapApiClient
{
    public async Task<SapCreatePoResult?> CreatePurchaseOrderAsync(
        SapCreatePoRequest request, CancellationToken ct = default)
    {
        var entity = RequireEntity();
        var companyCode = await RequireCompanyCodeAsync(ct);
        var departmentCode = GetDepartmentCode();
        var docCurrency = GetDocCurrency();
        var warehouseCache = new Dictionary<string, (string? Branch, string? Warehouse)>();
        var itemCache = new Dictionary<string, (string? Product, string? Division)>();
        var projectCache = new Dictionary<string, string?>();
        var lines = new List<SapPurchaseOrderLineDto>();

        foreach (var i in request.Items)
        {
            var (branch, warehouse, product, division) = await ResolveCostCenterAsync(
                i.WarehouseCode,
                i.SkuItemCode,
                companyCode,
                warehouseCache,
                itemCache,
                ct
            );
            var project = await ResolveProjectAsync(i.BillOfLading, companyCode, projectCache, ct);

            lines.Add(
                new SapPurchaseOrderLineDto(
                    ItemCode: i.ItemCode,
                    ItemDescription: i.ItemDescription,
                    Quantity: i.Quantity,
                    UnitPrice: i.UnitPrice,
                    WarehouseCode: i.WarehouseCode,
                    TaxCode: i.TaxCode,
                    DeliveryDate: null,
                    DiscountPercent: null,
                    Project: project,
                    Branch: branch,
                    Division: division,
                    Product: product,
                    Department: departmentCode,
                    Warehouse: warehouse
                )
            );
        }

        var dto = new SapPurchaseOrderRequestDto(
            CardCode: request.VendorCode,
            DocDate: request.DocDate,
            NumAtCard: request.PoCode,
            Comments: request.Remark,
            DocCurrency: docCurrency,
            Lines: lines
        );

        var url = $"/WAMS/PurchaseOrders?Entity={Uri.EscapeDataString(entity)}";
        var body = await PostAndReadBodyAsync(url, dto, request.PoCode, "CreatePurchaseOrderAsync", ct);
        var (docEntry, docNum) = ExtractDocIdentifiers(body, request.PoCode, "CreatePurchaseOrderAsync");

        // Create response has never been observed to include docNum (only docEntry) - fetch it
        // via GET so SapPoNumber is the human-facing SAP number, not the internal DocEntry.
        docNum ??= await FetchDocNumAsync("/WAMS/PurchaseOrders", docEntry, entity, ct);
        var sapPoNumber = docNum?.ToString() ?? docEntry.ToString();

        return new SapCreatePoResult(sapPoNumber, docEntry);
    }

    public async Task<SapCreateApdpResult?> CreateApDownPaymentAsync(
        SapCreateApdpRequest request, CancellationToken ct = default)
    {
        var entity = RequireEntity();
        var companyCode = await RequireCompanyCodeAsync(ct);
        var departmentCode = GetDepartmentCode();
        var docCurrency = GetDocCurrency();
        var poBaseType = GetPoBaseType();
        var warehouseCache = new Dictionary<string, (string? Branch, string? Warehouse)>();
        var itemCache = new Dictionary<string, (string? Product, string? Division)>();
        var projectCache = new Dictionary<string, string?>();

        var lines = new List<SapApLineDto>();

        foreach (var i in request.Items)
        {
            var (branch, warehouse, product, division) = await ResolveCostCenterAsync(
                i.WarehouseCode,
                i.SkuItemCode,
                companyCode,
                warehouseCache,
                itemCache,
                ct
            );
            var project = await ResolveProjectAsync(i.BillOfLading, companyCode, projectCache, ct);

            lines.Add(
                new SapApLineDto(
                    ItemCode: i.ItemCode,
                    ItemDescription: i.ItemDescription,
                    IsWhTax: i.PphTaxTypeCode is not null ? "Y" : "N",
                    Quantity: (double)i.UnitCount,
                    UnitPrice: (double)i.UnitCost,
                    WarehouseCode: i.WarehouseCode,
                    TaxCode: i.PpnTaxTypeCode,
                    DiscountPercent: i.DiscountPercent is decimal d1 ? (double)d1 : null,
                    DeliveryDate: null,
                    Project: project,
                    BaseEntry: i.BaseEntry ?? 0,
                    BaseLine: i.BaseLine ?? 0,
                    BaseType: i.BaseEntry.HasValue ? poBaseType : 0,
                    Branch: branch,
                    Division: division,
                    Product: product,
                    Department: departmentCode,
                    Warehouse: warehouse
                )
            );
        }

        var dto = new SapApdpRequestDto(
            CardCode: request.VendorCode,
            DocDate: request.DocDate,
            NumAtCard: request.ApCode,
            Comments: request.Remark,
            DocCurrency: docCurrency,
            Lines: lines,
            // SAP requires WhTax present even when empty, despite openapi/sap.json marking it nullable.
            WhTax: []
        );

        var url = $"/WAMS/APDP?Entity={Uri.EscapeDataString(entity)}";
        var body = await PostAndReadBodyAsync(url, dto, request.ApCode, "CreateApDownPaymentAsync", ct);
        var docEntry = ParseDocEntry(body, request.ApCode, "CreateApDownPaymentAsync");

        return new SapCreateApdpResult(docEntry);
    }

    public async Task<SapCreateApInvoiceResult?> CreateApInvoiceAsync(
        SapCreateApInvoiceRequest request, CancellationToken ct = default)
    {
        var entity = RequireEntity();
        var companyCode = await RequireCompanyCodeAsync(ct);
        var departmentCode = GetDepartmentCode();
        var docCurrency = GetDocCurrency();
        var poBaseType = GetPoBaseType();
        var warehouseCache = new Dictionary<string, (string? Branch, string? Warehouse)>();
        var itemCache = new Dictionary<string, (string? Product, string? Division)>();
        var projectCache = new Dictionary<string, string?>();

        var lines = new List<SapApLineDto>();

        foreach (var i in request.Items)
        {
            var (branch, warehouse, product, division) = await ResolveCostCenterAsync(
                i.WarehouseCode, 
                i.SkuItemCode, 
                companyCode, 
                warehouseCache, 
                itemCache, ct
            );
            var project = await ResolveProjectAsync(i.BillOfLading, companyCode, projectCache, ct);

            lines.Add(new SapApLineDto(
                ItemCode: i.ItemCode,
                ItemDescription: i.ItemDescription,
                IsWhTax: i.PphTaxTypeCode is not null ? "Y" : "N",
                Quantity: (double)i.UnitCount,
                UnitPrice: (double)i.UnitCost,
                WarehouseCode: i.WarehouseCode,
                TaxCode: i.PpnTaxTypeCode,
                DiscountPercent: i.DiscountPercent is decimal d2 ? (double)d2 : null,
                DeliveryDate: null,
                Project: project,
                BaseEntry: i.BaseEntry ?? 0,
                BaseLine: i.BaseLine ?? 0,
                BaseType: i.BaseEntry.HasValue ? poBaseType : 0,
                Branch: branch,
                Division: division,
                Product: product,
                Department: departmentCode,
                Warehouse: warehouse)
            );
        }

        // Tapdp/WhTax: SAP requires these arrays present even when empty, despite
        // openapi/sap.json marking them nullable.
        var dto = new SapApInvoiceRequestDto(
            CardCode: request.VendorCode,
            DocDate: request.DocDate,
            NumAtCard: request.ApCode,
            Comments: request.Remark,
            DocCurrency: docCurrency,
            Lines: lines,
            WhTax: request.WhTax?.Select(w => new SapWhTaxDto(w.WtCode, (double)w.TaxableAmount)).ToList() ?? [],
            Tapdp: request.Tapdp?
                .Select(dp => new SapApInvoiceDpDto(dp.BaseEntryDp, (double)dp.AmountToDraw))
                .ToList() ?? []);

        var url = $"/WAMS/APInvoice?Entity={Uri.EscapeDataString(entity)}";
        var body = await PostAndReadBodyAsync(url, dto, request.ApCode, "CreateApInvoiceAsync", ct);
        var (docEntry, docNum) = ExtractDocIdentifiers(body, request.ApCode, "CreateApInvoiceAsync");

        docNum ??= await FetchDocNumAsync("/WAMS/APInvoice", docEntry, entity, ct);

        var sapApNumber = docNum?.ToString() ?? docEntry.ToString();

        return new SapCreateApInvoiceResult(sapApNumber, docEntry);
    }

    private string RequireEntity() => configuration["ErpApi:SapEntity"] 
        ?? throw new InvalidOperationException("ErpApi:SapEntity is not configured");

    /// <summary>Requesting user's own company code, for the Lk* lookups (separate from RequireEntity/SapEntity, which targets SAP writes).</summary>
    private async Task<string> RequireCompanyCodeAsync(CancellationToken ct)
    {
        var companyId = tenantContext.CompanyId
            ?? throw new InvalidOperationException("No company context is set for this request");
        var company = await companyRepo.GetByIdAsync(companyId, ct)
            ?? throw new InvalidOperationException($"Company {companyId} not found");
        return company.Code;
    }

    /// <summary>SAP DI API object-type code for PO-linked AP invoice lines. SAP's default is 22 (Purchase Order); override via ErpApi:SapPoBaseType if this SAP setup differs.</summary>
    private int GetPoBaseType() =>
        configuration.GetValue<int?>("ErpApi:SapPoBaseType") ?? 22;

    /// <summary>Doc currency, defaults to IDR unless overridden via ErpApi:SapDocCurrency.</summary>
    private string GetDocCurrency() =>
        configuration["ErpApi:SapDocCurrency"] ?? "IDR";

    /// <summary>Department is a fixed allocation, not per-line (client-confirmed 2026-07-17): always "2LNW" unless overridden via ErpApi:SapDepartmentCode.</summary>
    private string GetDepartmentCode() =>
        configuration["ErpApi:SapDepartmentCode"] ?? "2LNW";

    /// <summary>
    /// Resolves branch/warehouse/product/division. Branch/warehouse: null on failure/no match.
    /// skuItemCode must be a real SKU, never the cost item code - no SKU skips
    /// Product/Division lookup entirely and uses their placeholders instead.
    /// </summary>
    private async Task<(string? Branch, string? Warehouse, string? Product, string? Division)> ResolveCostCenterAsync(
        string? warehouseCode, string? skuItemCode, string companyCode,
        Dictionary<string, (string? Branch, string? Warehouse)> warehouseCache,
        Dictionary<string, (string? Product, string? Division)> itemCache,
        CancellationToken ct
    )
    {
        string? branch = null;
        string? warehouse = null;

        if (warehouseCode is not null)
        {
            if (!warehouseCache.TryGetValue(warehouseCode, out var cached))
            {
                var branchResults = await erp.GetCostCenterBranchAsync(companyCode, warehouseCode, ct);
                var warehouseResults = await erp.GetCostCenterWarehouseAsync(companyCode, warehouseCode, ct);

                cached = (
                    FirstOrLogAmbiguous(branchResults, "LkBranch", warehouseCode)?.OcrCode,
                    FirstOrLogAmbiguous(warehouseResults, "LkWarehouse", warehouseCode)?.OcrCode
                );
                warehouseCache[warehouseCode] = cached;
            }

            (branch, warehouse) = cached;
        }

        if (string.IsNullOrEmpty(skuItemCode))
        {
            return (branch, warehouse, GetNoProductCode(), GetNoDivisionCode());
        }

        if (!itemCache.TryGetValue(skuItemCode, out var itemCached))
        {
            var productResults = await erp.GetCostCenterProductAsync(companyCode, skuItemCode, ct);
            var divisionResults = await erp.GetCostCenterDivisionAsync(companyCode, skuItemCode, ct);
            var product = FirstOrLogAmbiguous(productResults, "LkProduct", skuItemCode)?.OcrCode;
            var division = FirstOrLogAmbiguous(divisionResults, "LkDivision", skuItemCode)?.OcrCode;

            itemCached = (
                string.IsNullOrEmpty(product) ? GetNoProductCode() : product,
                string.IsNullOrEmpty(division) ? GetNoDivisionCode() : division
            );
            itemCache[skuItemCode] = itemCached;
        }

        return (branch, warehouse, itemCached.Product, itemCached.Division);
    }

    /// <summary>No-SKU division placeholder, derived from live SAP data, not client-confirmed. Override via ErpApi:SapNoDivisionCode.</summary>
    private string GetNoDivisionCode() =>
        configuration["ErpApi:SapNoDivisionCode"] ?? "1FMAC";

    /// <summary>No-SKU product placeholder, matches SAP's own "No Product" sentinel. Override via ErpApi:SapNoProductCode.</summary>
    private string GetNoProductCode() =>
        configuration["ErpApi:SapNoProductCode"] ?? "4NOP";

    /// <summary>Resolves Project via /WAMS/LkProject, keyed by BillOfLading. SAP requires Project on every line, so a missing BillOfLading or a failed/empty lookup falls back to GetNoProjectCode() rather than null.</summary>
    private async Task<string?> ResolveProjectAsync(
        string? billOfLading, string companyCode, Dictionary<string, string?> cache, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(billOfLading))
        {
            return GetNoProjectCode();
        }

        if (cache.TryGetValue(billOfLading, out var cached))
        {
            return cached;
        }

        var results = await erp.GetProjectsAsync(companyCode, billOfLading, ct);
        var project = FirstOrLogAmbiguous(results, "LkProject", billOfLading)?.PrjCode ?? GetNoProjectCode();

        cache[billOfLading] = project;

        return project;
    }

    /// <summary>SAP's placeholder project code for items with no real project, client-confirmed 2026-08-10. Override via ErpApi:SapNoProjectCode.</summary>
    private string GetNoProjectCode() =>
        configuration["ErpApi:SapNoProjectCode"] ?? "MV.0000001";

    private T? FirstOrLogAmbiguous<T>(List<T>? results, string endpoint, string key)
    {
        if (results is null || results.Count == 0)
        {
            return default;
        }

        if (results.Count > 1)
        {
            logger.LogWarning(
                "[SapApiClient] {Endpoint} returned {Count} results for '{Key}', using the first.",
                endpoint, results.Count, key);
        }

        return results[0];
    }

    private async Task<string> PostAndReadBodyAsync<TDto>(
        string url, TDto dto, string code, string operationName, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(url, dto, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogError(ex, "[SapApiClient] {Operation} request failed. Code={Code}", operationName, code);
            throw new ValidationException($"SAP call failed for {code}: {ex.Message}");
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "[SapApiClient] {Operation} non-success response. Code={Code} Status={Status} Body={Body}",
                operationName, code, (int)response.StatusCode, body);
            throw BuildSapRejectionException(code, body);
        }

        return body;
    }

    /// <summary>
    /// SAP's validation failures come back as an ASP.NET ProblemDetails body
    /// ({"title","errors":{"Lines[0].Project":["The Project field is required."]}}). Surface the
    /// field errors structured via ValidationException.Errors so the FE gets error.details instead
    /// of parsing an escaped JSON blob out of the message string.
    /// </summary>
    private static ValidationException BuildSapRejectionException(string code, string raw)
    {
        var (errors, title) = TryParseProblemDetails(raw);
        if (errors is { Count: > 0 })
        {
            return new ValidationException(errors);
        }

        // SAP's own envelope ({"success":false,"message":"..."}) can also come back on a
        // non-2xx status, not just wrapped in a 200 - same shape ExtractDocIdentifiers handles.
        var sapMessage = TryParseSapEnvelopeMessage(raw);

        return new ValidationException($"SAP rejected {code}: {title ?? sapMessage ?? raw}");
    }

    private static string? TryParseSapEnvelopeMessage(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            return root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static (Dictionary<string, string[]>? Errors, string? Title) TryParseProblemDetails(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            Dictionary<string, string[]>? errors = null;
            if (root.TryGetProperty("errors", out var errorsEl) && errorsEl.ValueKind == JsonValueKind.Object)
            {
                errors = errorsEl.EnumerateObject().ToDictionary(
                    p => p.Name,
                    p => p.Value.EnumerateArray().Select(v => v.GetString() ?? string.Empty).ToArray());
            }

            var title = root.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
            return (errors, string.IsNullOrWhiteSpace(title) ? null : title);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private int ParseDocEntry(string body, string code, string operationName) =>
        ExtractDocIdentifiers(body, code, operationName).DocEntry;

    /// <summary>
    /// Create response omits docNum (only docEntry) - fetch it via GET so SapPoNumber/SapApNumber
    /// shows the human-facing number. Best-effort: falls back to DocEntry on failure.
    /// </summary>
    private async Task<int?> FetchDocNumAsync(string docPath, int docEntry, string entity, CancellationToken ct)
    {
        try
        {
            var url = $"{docPath}/{docEntry}?Entity={Uri.EscapeDataString(entity)}";
            var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var target = root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object
                ? dataEl
                : root;
            return TryGetIntCaseInsensitive(target, "docNum");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[SapApiClient] Failed to fetch docNum for DocEntry={DocEntry} at {Path}", docEntry, docPath);
            return null;
        }
    }

    /// <summary>
    /// Tolerates SAP's response envelope ({"success","message","data","errors"}) plus the
    /// unwrapped/bare-number shapes seen in practice. Case-insensitive property lookup.
    /// </summary>
    private (int DocEntry, int? DocNum) ExtractDocIdentifiers(string body, string code, string operationName)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "[SapApiClient] {Operation} unparsable response. Code={Code} Body={Body}",
                operationName, code, body);
            throw new ValidationException($"SAP returned an unrecognized response for {code}");
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (root.TryGetProperty("success", out var successEl) && successEl.ValueKind == JsonValueKind.False)
            {
                logger.LogError("[SapApiClient] {Operation} SAP reported failure. Code={Code} Body={Body}",
                    operationName, code, body);
                var message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
                if (message is not null)
                {
                    var (nestedErrors, _) = TryParseProblemDetails(message);
                    throw nestedErrors is { Count: > 0 }
                        ? new ValidationException(nestedErrors)
                        : new ValidationException(message);
                }

                throw new ValidationException($"SAP rejected {code}");
            }

            var searchTargets = new List<JsonElement> { root };
            if (root.TryGetProperty("data", out var dataEl))
            {
                if (dataEl.ValueKind == JsonValueKind.Object)
                {
                    searchTargets.Insert(0, dataEl);
                }
                else if (dataEl.ValueKind == JsonValueKind.Number && dataEl.TryGetInt32(out var bareDocEntry))
                {
                    logger.LogInformation(
                        "[SapApiClient] {Operation} response. Code={Code} Body={Body}", operationName, code, body);
                    return (bareDocEntry, null);
                }
            }

            foreach (var target in searchTargets)
            {
                var docEntry = TryGetIntCaseInsensitive(target, "docEntry");
                if (docEntry is not null)
                {
                    logger.LogInformation(
                        "[SapApiClient] {Operation} response. Code={Code} Body={Body}", operationName, code, body);
                    return (docEntry.Value, TryGetIntCaseInsensitive(target, "docNum"));
                }
            }

            logger.LogError("[SapApiClient] {Operation} response missing DocEntry. Code={Code} Body={Body}",
                operationName, code, body);
            throw new ValidationException($"SAP did not return a document number for {code}");
        }
    }

    private static int? TryGetIntCaseInsensitive(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (!string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var numberValue))
            {
                return numberValue;
            }

            if (prop.Value.ValueKind == JsonValueKind.String && int.TryParse(prop.Value.GetString(), out var stringValue))
            {
                return stringValue;
            }
        }

        return null;
    }
}

file sealed record SapPurchaseOrderRequestDto(
    [property: JsonPropertyName("cardCode")] string CardCode,
    [property: JsonPropertyName("docDate")] DateTime DocDate,
    [property: JsonPropertyName("numAtCard")] string? NumAtCard,
    [property: JsonPropertyName("comments")] string? Comments,
    [property: JsonPropertyName("docCurrency")] string DocCurrency,
    [property: JsonPropertyName("lines")] List<SapPurchaseOrderLineDto> Lines);

file sealed record SapPurchaseOrderLineDto(
    [property: JsonPropertyName("itemCode")] string ItemCode,
    [property: JsonPropertyName("itemDescription")] string ItemDescription,
    [property: JsonPropertyName("quantity")] decimal Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice,
    [property: JsonPropertyName("warehouseCode")] string? WarehouseCode,
    [property: JsonPropertyName("taxCode")] string? TaxCode,
    [property: JsonPropertyName("deliveryDate")] DateTime? DeliveryDate,
    [property: JsonPropertyName("discountPercent")] double? DiscountPercent,
    [property: JsonPropertyName("project")] string? Project,
    [property: JsonPropertyName("branch")] string? Branch,
    [property: JsonPropertyName("division")] string? Division,
    [property: JsonPropertyName("product")] string? Product,
    [property: JsonPropertyName("department")] string? Department,
    [property: JsonPropertyName("warehouse")] string? Warehouse);

file sealed record SapApLineDto(
    [property: JsonPropertyName("itemCode")] string ItemCode,
    [property: JsonPropertyName("itemDescription")] string? ItemDescription,
    [property: JsonPropertyName("isWhTax")] string? IsWhTax,
    [property: JsonPropertyName("quantity")] double Quantity,
    [property: JsonPropertyName("unitPrice")] double UnitPrice,
    [property: JsonPropertyName("warehouseCode")] string? WarehouseCode,
    [property: JsonPropertyName("taxCode")] string? TaxCode,
    [property: JsonPropertyName("discountPercent")] double? DiscountPercent,
    [property: JsonPropertyName("deliveryDate")] DateTime? DeliveryDate,
    [property: JsonPropertyName("project")] string? Project,
    [property: JsonPropertyName("baseEntry")] int BaseEntry,
    [property: JsonPropertyName("baseLine")] int BaseLine,
    [property: JsonPropertyName("baseType")] int BaseType,
    [property: JsonPropertyName("branch")] string? Branch,
    [property: JsonPropertyName("division")] string? Division,
    [property: JsonPropertyName("product")] string? Product,
    [property: JsonPropertyName("department")] string? Department,
    [property: JsonPropertyName("warehouse")] string? Warehouse);

file sealed record SapWhTaxDto(
    [property: JsonPropertyName("wtCode")] string WtCode,
    [property: JsonPropertyName("taxableAmount")] double TaxableAmount);

file sealed record SapApInvoiceDpDto(
    [property: JsonPropertyName("baseEntryDP")] int BaseEntryDp,
    [property: JsonPropertyName("amountToDraw")] double AmountToDraw);

file sealed record SapApdpRequestDto(
    [property: JsonPropertyName("cardCode")] string CardCode,
    [property: JsonPropertyName("docDate")] DateTime DocDate,
    [property: JsonPropertyName("numAtCard")] string? NumAtCard,
    [property: JsonPropertyName("comments")] string? Comments,
    [property: JsonPropertyName("docCurrency")] string DocCurrency,
    [property: JsonPropertyName("lines")] List<SapApLineDto> Lines,
    [property: JsonPropertyName("whTax")] List<SapWhTaxDto>? WhTax);

file sealed record SapApInvoiceRequestDto(
    [property: JsonPropertyName("cardCode")] string CardCode,
    [property: JsonPropertyName("docDate")] DateTime DocDate,
    [property: JsonPropertyName("numAtCard")] string? NumAtCard,
    [property: JsonPropertyName("comments")] string? Comments,
    [property: JsonPropertyName("docCurrency")] string DocCurrency,
    [property: JsonPropertyName("lines")] List<SapApLineDto> Lines,
    [property: JsonPropertyName("whTax")] List<SapWhTaxDto>? WhTax,
    [property: JsonPropertyName("tapdp")] List<SapApInvoiceDpDto>? Tapdp);
