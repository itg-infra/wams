namespace WAMS.Infrastructure.ExternalSync.ErpHttpClient;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WAMS.Infrastructure.ExternalSync.CostCenter;
using WAMS.Infrastructure.ExternalSync.Item;
using WAMS.Infrastructure.ExternalSync.Ppn;
using WAMS.Infrastructure.ExternalSync.Pph;
using WAMS.Infrastructure.ExternalSync.Project;
using WAMS.Infrastructure.ExternalSync.Spk;
using WAMS.Infrastructure.ExternalSync.TransportOrder;
using WAMS.Infrastructure.ExternalSync.Vendor;
using WAMS.Infrastructure.ExternalSync.Warehouse;

// Polly resilience pipeline (retry + circuit breaker + timeout) is configured
// on the HttpClient in Program.cs via AddResilienceHandler("erp-pipeline").
public class ErpApiClient(HttpClient http, ILogger<ErpApiClient> logger)
{
    public async Task<List<WarehouseErpDto>?> GetWarehousesAsync(
        string? companyCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkWhsCode?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<WarehouseErpDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetWarehouses failed for company={CompanyCode}",
                companyCode);

            return null;
        }
    }

    public async Task<List<VendorErpDto>?> GetVendorsAsync(
        string? companyCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkVendor?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<VendorErpDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetVendors failed for company={CompanyCode}",
                companyCode);

            return null;
        }
    }

    public async Task<List<ItemErpDto>?> GetItemsAsync(
        string? companyCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkCostItem?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<ItemErpDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetItems failed for company={CompanyCode}",
                companyCode);

            return null;
        }
    }

    public async Task<List<SpkErpDto>?> GetSpksAsync(
        string? companyCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkMOLOPMS?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<SpkErpDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetSpks failed for company={CompanyCode}",
                companyCode);

            return null;
        }
    }

    public async Task<List<PpnErpDto>?> GetPpnAsync(
        string? companyCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/PPn?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<PpnErpDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetPpn failed for company={CompanyCode}",
                companyCode);

            return null;
        }
    }

    public async Task<List<PphErpDto>?> GetPphAsync(
        string? companyCode,
        string cardCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/PPh?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}&CardCode={Uri.EscapeDataString(cardCode)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<PphErpDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetPph failed for company={CompanyCode} cardCode={CardCode}",
                companyCode,
                cardCode);

            return null;
        }
    }

    public async Task<List<ToErpDto>?> GetTransportOrdersAsync(
        string? companyCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkTOMOLOPMS?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<ToErpDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetTransportOrders failed for company={CompanyCode}",
                companyCode);

            return null;
        }
    }

    public async Task<List<OcrLookupDto>?> GetCostCenterBranchAsync(
        string? companyCode,
        string whsCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkBranch?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}&WhsCode={Uri.EscapeDataString(whsCode)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<OcrLookupDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetCostCenterBranch failed for company={CompanyCode} whsCode={WhsCode}",
                companyCode,
                whsCode);

            return null;
        }
    }

    public async Task<List<OcrLookupDto>?> GetCostCenterWarehouseAsync(
        string? companyCode,
        string whsCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkWarehouse?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}&WhsCode={Uri.EscapeDataString(whsCode)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<OcrLookupDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetCostCenterWarehouse failed for company={CompanyCode} whsCode={WhsCode}",
                companyCode,
                whsCode);

            return null;
        }
    }

    public async Task<List<OcrLookupDto>?> GetCostCenterProductAsync(
        string? companyCode,
        string itemCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkProduct?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}&ItemCode={Uri.EscapeDataString(itemCode)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<OcrLookupDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetCostCenterProduct failed for company={CompanyCode} itemCode={ItemCode}",
                companyCode,
                itemCode);

            return null;
        }
    }

    public async Task<List<OcrLookupDto>?> GetCostCenterDivisionAsync(
        string? companyCode,
        string itemCode,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkDivision?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}&ItemCode={Uri.EscapeDataString(itemCode)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<OcrLookupDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetCostCenterDivision failed for company={CompanyCode} itemCode={ItemCode}",
                companyCode,
                itemCode);

            return null;
        }
    }

    public async Task<List<ProjectLookupDto>?> GetProjectsAsync(
        string? companyCode,
        string billOfLading,
        CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetAsync(
                $"/WAMS/LkProject?Company={Uri.EscapeDataString(companyCode ?? string.Empty)}&BL={Uri.EscapeDataString(billOfLading)}",
                ct
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<ProjectLookupDto>>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[ErpApiClient] GetProjects failed for company={CompanyCode} bl={Bl}",
                companyCode,
                billOfLading);

            return null;
        }
    }
}
