namespace WAMS.Infrastructure.ExternalSap;

using Microsoft.Extensions.Logging;
using WAMS.Application.Interfaces.Common;

public class MockSapApiClient(ILogger<MockSapApiClient> logger) : ISapApiClient
{
    public Task<SapCreatePoResult?> CreatePurchaseOrderAsync(SapCreatePoRequest request, CancellationToken ct = default)
    {
        var sapPoNumber = $"SAP-PO-{Guid.NewGuid():N}";
        var fakeDocEntry = Random.Shared.Next(1000, 999999);
        logger.LogInformation(
            "[MockSapApiClient] Generated mock SAP PO. Code={Code} SapPoNumber={SapPoNumber} SapDocEntry={SapDocEntry}",
            request.PoCode, sapPoNumber, fakeDocEntry);
        return Task.FromResult<SapCreatePoResult?>(new SapCreatePoResult(sapPoNumber, fakeDocEntry));
    }

    public Task<SapCreateApdpResult?> CreateApDownPaymentAsync(SapCreateApdpRequest request, CancellationToken ct = default)
    {
        var fakeDocEntry = Random.Shared.Next(1000, 999999);
        logger.LogInformation(
            "[MockSapApiClient] Generated mock SAP APDP. Code={Code} SapDocEntry={SapDocEntry}",
            request.ApCode, fakeDocEntry);
        return Task.FromResult<SapCreateApdpResult?>(new SapCreateApdpResult(fakeDocEntry));
    }

    public Task<SapCreateApInvoiceResult?> CreateApInvoiceAsync(SapCreateApInvoiceRequest request, CancellationToken ct = default)
    {
        var sapApNumber = $"SAP-AP-{Guid.NewGuid():N}";
        var fakeDocEntry = Random.Shared.Next(1000, 999999);
        logger.LogInformation(
            "[MockSapApiClient] Generated mock SAP AP Invoice. Code={Code} SapApNumber={SapApNumber} SapDocEntry={SapDocEntry}",
            request.ApCode, sapApNumber, fakeDocEntry);
        return Task.FromResult<SapCreateApInvoiceResult?>(new SapCreateApInvoiceResult(sapApNumber, fakeDocEntry));
    }
}
