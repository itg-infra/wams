using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

public class ErpApiClientTests
{
    private static ErpApiClient CreateSut(HttpStatusCode status, string body) =>
        new(new HttpClient(new FakeHttpHandler(status, body)) { BaseAddress = new Uri("http://erp.test") },
            Substitute.For<ILogger<ErpApiClient>>());

    [Fact]
    public async Task GetCostCenterBranchAsync_Success_ReturnsParsedList()
    {
        var sut = CreateSut(HttpStatusCode.OK, """[{"ocrCode":"3JKT","ocrName":"Jakarta"}]""");

        var result = await sut.GetCostCenterBranchAsync("GCU", "WHJKT011", TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result![0].OcrCode.Should().Be("3JKT");
        result[0].OcrName.Should().Be("Jakarta");
    }

    [Fact]
    public async Task GetCostCenterBranchAsync_NonSuccessStatus_ReturnsNull()
    {
        var sut = CreateSut(HttpStatusCode.InternalServerError, "error");

        var result = await sut.GetCostCenterBranchAsync("GCU", "WHJKT011", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCostCenterWarehouseAsync_Success_ReturnsParsedList()
    {
        var sut = CreateSut(HttpStatusCode.OK, """[{"ocrCode":"5JKSG","ocrName":"JKT - SGT"}]""");

        var result = await sut.GetCostCenterWarehouseAsync("GCU", "WHJKT011", TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result![0].OcrCode.Should().Be("5JKSG");
    }

    [Fact]
    public async Task GetCostCenterWarehouseAsync_NonSuccessStatus_ReturnsNull()
    {
        var sut = CreateSut(HttpStatusCode.InternalServerError, "error");

        var result = await sut.GetCostCenterWarehouseAsync("GCU", "WHJKT011", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCostCenterProductAsync_Success_ReturnsParsedList()
    {
        var sut = CreateSut(HttpStatusCode.OK, """[{"ocrCode":"4NOP","ocrName":"No Product"}]""");

        var result = await sut.GetCostCenterProductAsync("GCU", "Z.KIRIM002", TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result![0].OcrCode.Should().Be("4NOP");
    }

    [Fact]
    public async Task GetCostCenterProductAsync_NonSuccessStatus_ReturnsNull()
    {
        var sut = CreateSut(HttpStatusCode.InternalServerError, "error");

        var result = await sut.GetCostCenterProductAsync("GCU", "Z.KIRIM002", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCostCenterDivisionAsync_Success_ReturnsParsedListWithEmptyCode()
    {
        // Live-observed shape: some items resolve to an empty ocrCode/ocrName, a valid "no division" answer.
        var sut = CreateSut(HttpStatusCode.OK, """[{"ocrCode":"","ocrName":""}]""");

        var result = await sut.GetCostCenterDivisionAsync("GCU", "Z.KIRIM002", TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result![0].OcrCode.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCostCenterDivisionAsync_NonSuccessStatus_ReturnsNull()
    {
        var sut = CreateSut(HttpStatusCode.InternalServerError, "error");

        var result = await sut.GetCostCenterDivisionAsync("GCU", "Z.KIRIM002", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    private sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}
