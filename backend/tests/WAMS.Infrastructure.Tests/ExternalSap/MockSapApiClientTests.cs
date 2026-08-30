namespace WAMS.Infrastructure.Tests.ExternalSap;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Infrastructure.ExternalSap;
using Xunit;

public class MockSapApiClientTests
{
    private readonly MockSapApiClient _sut = new(Substitute.For<ILogger<MockSapApiClient>>());

    [Fact]
    public async Task CreateApDownPaymentAsync_ReturnsFakeDocEntry()
    {
        var request = new SapCreateApdpRequest("AP-001", "V-001", DateTime.UtcNow, null,
            [new SapApLineItem("ITEM-A", "Item A", "ACC-01", 1m, 100m, "PCS", 100m, 0m, null, null, null)]);

        var result = await _sut.CreateApDownPaymentAsync(request, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapDocEntry.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateApInvoiceAsync_ReturnsFakeApNumberAndDocEntry()
    {
        var request = new SapCreateApInvoiceRequest("AP-001", "V-001", DateTime.UtcNow, null,
            [new SapApLineItem("ITEM-A", "Item A", "ACC-01", 1m, 100m, "PCS", 100m, 0m, null, null, null)],
            WhTax: [new SapWhTaxLine("PPH23", 100m)], ApdpDocEntry: null, DrawAmount: null);

        var result = await _sut.CreateApInvoiceAsync(request, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapApNumber.Should().StartWith("SAP-AP-");
        result.SapDocEntry.Should().BeGreaterThan(0);
    }
}
