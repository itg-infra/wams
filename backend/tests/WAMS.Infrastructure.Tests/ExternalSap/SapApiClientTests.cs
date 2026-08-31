using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Exceptions;
using WAMS.Infrastructure.ExternalSap;
using WAMS.Infrastructure.ExternalSync.CostCenter;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;
using WAMS.Infrastructure.ExternalSync.Project;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSap;

public class SapApiClientTests
{
    private static SapCreatePoRequest CreateRequest(string? skuItemCode = null) => new(
        PoCode: "PO-001",
        VendorCode: "V-001",
        DocDate: new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
        Remark: "test remark",
        Items:
        [
            new SapPoLineItem("ITEM-A", "Item Alpha", 10m, 1000m, "WH-01", "PPN11", "SSZ1769067", skuItemCode)
        ]);

    private static SapApLineItem MakeLineItem(
        string itemCode, string coaCode, decimal unitCount, decimal unitCost, decimal budgetPlanTotal,
        string? pphTaxTypeCode = null, decimal? discountPercent = null, string? skuItemCode = null) =>
        new(itemCode, $"{itemCode} description", coaCode, unitCount, unitCost, "PCS", budgetPlanTotal, 0m,
            PpnTaxTypeCode: null, PphTaxTypeCode: pphTaxTypeCode, DiscountPercent: discountPercent,
            SkuItemCode: skuItemCode);

    private static SapApiClient CreateSut(HttpStatusCode status, string body) =>
        CreateSut(status, body, out _);

    private static SapApiClient CreateSut(HttpStatusCode status, string body, out FakeHttpHandler handler) =>
        CreateSut(status, body, out handler, DefaultOcrResponder, out _);

    private static SapApiClient CreateSut(
        HttpStatusCode status, string body, out FakeHttpHandler handler,
        Func<Uri, HttpResponseMessage> erpResponder, out FakeErpHttpHandler erpHandler)
    {
        handler = new FakeHttpHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://sap.test") };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new("ErpApi:SapEntity", "Test")])
            .Build();

        erpHandler = new FakeErpHttpHandler(erpResponder);
        var erpHttp = new HttpClient(erpHandler) { BaseAddress = new Uri("http://erp.test") };
        var erpClient = new ErpApiClient(erpHttp, Substitute.For<ILogger<ErpApiClient>>());

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(1L);
        var companyRepo = Substitute.For<ICompanyRepository>();
        companyRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 1L, Code = "COMP01" });

        return new SapApiClient(http, config, Substitute.For<ILogger<SapApiClient>>(), erpClient, tenantContext, companyRepo);
    }

    private static HttpResponseMessage DefaultOcrResponder(Uri uri)
    {
        if (uri.AbsolutePath.Contains("LkProject", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"bl":"SSZ1769067","prjCode":"MV.0000565","prjName":"CMA CGM THORIUM"}]""",
                    Encoding.UTF8, "application/json"),
            };
        }

        var (code, name) = uri.AbsolutePath switch
        {
            var p when p.Contains("LkBranch", StringComparison.OrdinalIgnoreCase) => ("3JKT", "Jakarta"),
            var p when p.Contains("LkWarehouse", StringComparison.OrdinalIgnoreCase) => ("5JKSG", "JKT - SGT"),
            var p when p.Contains("LkProduct", StringComparison.OrdinalIgnoreCase) => ("4NOP", "No Product"),
            var p when p.Contains("LkDivision", StringComparison.OrdinalIgnoreCase) => ("9DIV", "Test Division"),
            _ => throw new InvalidOperationException($"Unexpected ERP lookup path: {uri}"),
        };

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""[{"ocrCode":"{{code}}","ocrName":"{{name}}"}]""", Encoding.UTF8, "application/json"),
        };
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_SuccessWithDocEntryAndDocNum_ReturnsDocNumAsPoNumber()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 501, "docNum": 9001}""");

        var result = await sut.CreatePurchaseOrderAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapDocEntry.Should().Be(501);
        result.SapPoNumber.Should().Be("9001");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_SuccessWithOnlyDocEntry_FallsBackToDocEntryAsPoNumber()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"DocEntry": 777}""");

        var result = await sut.CreatePurchaseOrderAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapDocEntry.Should().Be(777);
        result.SapPoNumber.Should().Be("777");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_NonSuccessStatus_ThrowsWithSapErrorBody()
    {
        var sut = CreateSut(HttpStatusCode.BadRequest, """{"error": "Invalid item code ITEM-A"}""");

        var act = () => sut.CreatePurchaseOrderAsync(CreateRequest());

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Message.Should().Contain("Invalid item code ITEM-A");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_NonSuccessStatus_ProblemDetailsBody_ExposesFieldErrors()
    {
        var sut = CreateSut(HttpStatusCode.BadRequest,
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Lines[0].Project":["The Project field is required."]},"traceId":"00-abc-def-00"}""");

        var act = () => sut.CreatePurchaseOrderAsync(CreateRequest());

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Lines[0].Project")
            .WhoseValue.Should().ContainSingle().Which.Should().Be("The Project field is required.");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_SuccessWithoutDocEntry_ThrowsValidationException()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"status": "ok"}""");

        var act = () => sut.CreatePurchaseOrderAsync(CreateRequest());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_UnparsableBody_ThrowsValidationException()
    {
        var sut = CreateSut(HttpStatusCode.OK, "not json");

        var act = () => sut.CreatePurchaseOrderAsync(CreateRequest());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_ResolvesCostCenterFieldsFromLookups()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 501}""", out var handler, DefaultOcrResponder, out _);

        await sut.CreatePurchaseOrderAsync(
            CreateRequest(skuItemCode: "SKU-A"), TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var line = json.RootElement.GetProperty("lines").EnumerateArray().Single();
        line.GetProperty("branch").GetString().Should().Be("3JKT");
        line.GetProperty("warehouse").GetString().Should().Be("5JKSG");
        line.GetProperty("product").GetString().Should().Be("4NOP");
        line.GetProperty("division").GetString().Should().Be("9DIV");
        line.GetProperty("department").GetString().Should().Be("2LNW");
        line.GetProperty("project").GetString().Should().Be("MV.0000565");

        foreach (var stillNullProperty in new[] { "deliveryDate", "discountPercent" })
        {
            line.GetProperty(stillNullProperty).ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_NoSkuItemCode_SkipsLookupAndUsesPlaceholders()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 501}""", out var handler,
            DefaultOcrResponder, out var erpHandler);

        await sut.CreatePurchaseOrderAsync(CreateRequest(), TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var line = json.RootElement.GetProperty("lines").EnumerateArray().Single();
        line.GetProperty("product").GetString().Should().Be("4NOP");
        line.GetProperty("division").GetString().Should().Be("1FMAC");
        erpHandler.Requests.Should().NotContain(u => u.AbsolutePath.Contains("LkProduct", StringComparison.OrdinalIgnoreCase));
        erpHandler.Requests.Should().NotContain(u => u.AbsolutePath.Contains("LkDivision", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_LookupFailure_ProceedsWithNullBranchWarehouseAndFallbackProjectAndDivision()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 501}""", out var handler,
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError), out _);

        await sut.CreatePurchaseOrderAsync(
            CreateRequest(skuItemCode: "SKU-A"), TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var line = json.RootElement.GetProperty("lines").EnumerateArray().Single();
        foreach (var propertyName in new[] { "branch", "warehouse" })
        {
            line.GetProperty(propertyName).ValueKind.Should().Be(JsonValueKind.Null);
        }

        line.GetProperty("project").GetString().Should().Be("MV.0000001");
        line.GetProperty("division").GetString().Should().Be("1FMAC");
        line.GetProperty("product").GetString().Should().Be("4NOP");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_DivisionLookupReturnsBlankOcrCode_UsesFallbackCode()
    {
        Func<Uri, HttpResponseMessage> blankDivisionResponder = uri =>
            uri.AbsolutePath.Contains("LkDivision", StringComparison.OrdinalIgnoreCase)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """[{"ocrCode":"","ocrName":""}]""", Encoding.UTF8, "application/json"),
                }
                : DefaultOcrResponder(uri);
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 501}""", out var handler, blankDivisionResponder, out _);

        await sut.CreatePurchaseOrderAsync(
            CreateRequest(skuItemCode: "SKU-A"), TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var line = json.RootElement.GetProperty("lines").EnumerateArray().Single();
        line.GetProperty("division").GetString().Should().Be("1FMAC");
        line.GetProperty("product").GetString().Should().Be("4NOP");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_NoBillOfLading_SkipsProjectLookupAndUsesFallbackCode()
    {
        var request = new SapCreatePoRequest(
            PoCode: "PO-001",
            VendorCode: "V-001",
            DocDate: new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            Remark: "test remark",
            Items: [new SapPoLineItem("ITEM-A", "Item Alpha", 10m, 1000m, "WH-01", "PPN11", BillOfLading: null)]);
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 501}""", out var handler,
            DefaultOcrResponder, out var erpHandler);

        await sut.CreatePurchaseOrderAsync(request, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var line = json.RootElement.GetProperty("lines").EnumerateArray().Single();
        line.GetProperty("project").GetString().Should().Be("MV.0000001");
        erpHandler.Requests.Should().NotContain(u => u.AbsolutePath.Contains("LkProject", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_BillOfLadingWithNoProjectMatch_UsesFallbackCode()
    {
        Func<Uri, HttpResponseMessage> noMatchResponder = uri =>
            uri.AbsolutePath.Contains("LkProject", StringComparison.OrdinalIgnoreCase)
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", Encoding.UTF8, "application/json"),
                }
                : DefaultOcrResponder(uri);
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 501}""", out var handler, noMatchResponder, out _);

        await sut.CreatePurchaseOrderAsync(CreateRequest(), TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var line = json.RootElement.GetProperty("lines").EnumerateArray().Single();
        line.GetProperty("project").GetString().Should().Be("MV.0000001");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_CachesLookupsPerWarehouseAndItemAcrossLines()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 501}""", out _, DefaultOcrResponder, out var erpHandler);
        var request = new SapCreatePoRequest(
            PoCode: "PO-001",
            VendorCode: "V-001",
            DocDate: new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            Remark: "test remark",
            Items:
            [
                new SapPoLineItem("ITEM-A", "Item Alpha", 10m, 1000m, "WH-01", "PPN11", "SSZ1769067", "SKU-A"),
                new SapPoLineItem("ITEM-A", "Item Alpha", 5m, 1000m, "WH-01", "PPN11", "SSZ1769067", "SKU-A"),
            ]);

        await sut.CreatePurchaseOrderAsync(request, TestContext.Current.CancellationToken);

        erpHandler.Requests.Count(u => u.AbsolutePath.Contains("LkBranch", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        erpHandler.Requests.Count(u => u.AbsolutePath.Contains("LkWarehouse", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        erpHandler.Requests.Count(u => u.AbsolutePath.Contains("LkProduct", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        erpHandler.Requests.Count(u => u.AbsolutePath.Contains("LkDivision", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        erpHandler.Requests.Count(u => u.AbsolutePath.Contains("LkProject", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_EnvelopedObjectData_ExtractsDocEntryAndDocNum()
    {
        var sut = CreateSut(HttpStatusCode.OK,
            """{"success":true,"message":"OK","data":{"docEntry":501,"docNum":9001},"errors":[]}""");

        var result = await sut.CreatePurchaseOrderAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapDocEntry.Should().Be(501);
        result.SapPoNumber.Should().Be("9001");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_EnvelopedBareNumberData_ExtractsDocEntry()
    {
        var sut = CreateSut(HttpStatusCode.OK,
            """{"success":true,"message":"OK","data":501,"errors":[]}""");

        var result = await sut.CreatePurchaseOrderAsync(CreateRequest(), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapDocEntry.Should().Be(501);
        result.SapPoNumber.Should().Be("501");
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_EnvelopeSuccessFalseWith200Status_ThrowsWithSapMessage()
    {
        var sut = CreateSut(HttpStatusCode.OK,
            """{"success":false,"message":"Gagal membuat Purchase Order. Project tidak boleh kosong","data":0,"errors":[]}""");

        var act = () => sut.CreatePurchaseOrderAsync(CreateRequest());

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Message.Should().Contain("Project tidak boleh kosong");
    }

    private static SapCreateApdpRequest CreateApdpRequest() => new(
        ApCode: "AP-001",
        VendorCode: "V-001",
        DocDate: new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc),
        Remark: "test remark",
        Items: [MakeLineItem("ITEM-A", "ACC-01", 10m, 1000m, 10000m)]);

    private static SapCreateApInvoiceRequest CreateApInvoiceRequest(
        List<SapWhTaxLine>? whTax = null, List<SapApInvoiceDpLine>? tapdp = null) => new(
        ApCode: "AP-001",
        VendorCode: "V-001",
        DocDate: new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc),
        Remark: "test remark",
        Items: [MakeLineItem("ITEM-A", "ACC-01", 10m, 1000m, 10000m)],
        WhTax: whTax,
        Tapdp: tapdp);

    [Fact]
    public async Task CreateApDownPaymentAsync_ResolvesProductAndDivisionOnly()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 301}""", out var handler, DefaultOcrResponder, out _);
        var request = new SapCreateApdpRequest(
            ApCode: "AP-001",
            VendorCode: "V-001",
            DocDate: new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc),
            Remark: "test remark",
            Items: [MakeLineItem("ITEM-A", "ACC-01", 10m, 1000m, 10000m, skuItemCode: "SKU-A")]);

        await sut.CreateApDownPaymentAsync(request, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var line = json.RootElement.GetProperty("lines").EnumerateArray().Single();
        line.GetProperty("product").GetString().Should().Be("4NOP");
        line.GetProperty("division").GetString().Should().Be("9DIV");
        line.GetProperty("department").GetString().Should().Be("2LNW");
        line.GetProperty("project").GetString().Should().Be("MV.0000001");

        foreach (var stillNullProperty in new[] { "deliveryDate", "branch", "warehouse" })
        {
            line.GetProperty(stillNullProperty).ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task CreateApInvoiceAsync_ResolvesProductAndDivisionOnly()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 401}""", out var handler, DefaultOcrResponder, out _);
        var request = new SapCreateApInvoiceRequest(
            ApCode: "AP-001",
            VendorCode: "V-001",
            DocDate: new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc),
            Remark: "test remark",
            Items: [MakeLineItem("ITEM-A", "ACC-01", 10m, 1000m, 10000m, skuItemCode: "SKU-A")],
            WhTax: null,
            Tapdp: null);

        await sut.CreateApInvoiceAsync(request, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var line = json.RootElement.GetProperty("lines").EnumerateArray().Single();
        line.GetProperty("product").GetString().Should().Be("4NOP");
        line.GetProperty("division").GetString().Should().Be("9DIV");
        line.GetProperty("department").GetString().Should().Be("2LNW");
        line.GetProperty("project").GetString().Should().Be("MV.0000001");

        foreach (var stillNullProperty in new[] { "deliveryDate", "branch", "warehouse" })
        {
            line.GetProperty(stillNullProperty).ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task CreateApDownPaymentAsync_Success_ReturnsDocEntry()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 301}""");

        var result = await sut.CreateApDownPaymentAsync(CreateApdpRequest(), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapDocEntry.Should().Be(301);
    }

    [Fact]
    public async Task CreateApDownPaymentAsync_NonSuccessStatus_ThrowsWithSapErrorBody()
    {
        var sut = CreateSut(HttpStatusCode.BadRequest, """{"error": "Branch: required"}""");

        var act = () => sut.CreateApDownPaymentAsync(CreateApdpRequest());

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Message.Should().Contain("Branch: required");
    }

    [Fact]
    public async Task CreateApDownPaymentAsync_MapsIsWhTaxFromPphTaxTypeCodePerLine()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 301}""", out var handler);
        var request = new SapCreateApdpRequest(
            ApCode: "AP-001",
            VendorCode: "V-001",
            DocDate: new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc),
            Remark: "test remark",
            Items:
            [
                MakeLineItem("ITEM-A", "ACC-01", 10m, 1000m, 10000m, pphTaxTypeCode: "PPH23"),
                MakeLineItem("ITEM-B", "ACC-02", 5m, 500m, 2500m),
            ]);

        await sut.CreateApDownPaymentAsync(request, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var lines = json.RootElement.GetProperty("lines").EnumerateArray().ToList();
        lines[0].GetProperty("isWhTax").GetString().Should().Be("Y");
        lines[1].GetProperty("isWhTax").GetString().Should().Be("N");
    }

    [Fact]
    public async Task CreateApDownPaymentAsync_EnvelopedObjectData_ExtractsDocEntry()
    {
        var sut = CreateSut(HttpStatusCode.OK,
            """{"success":true,"message":"OK","data":{"docEntry":301},"errors":[]}""");

        var result = await sut.CreateApDownPaymentAsync(CreateApdpRequest(), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapDocEntry.Should().Be(301);
    }

    [Fact]
    public async Task CreateApDownPaymentAsync_SuccessWithoutDocEntry_ThrowsValidationException()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"status": "ok"}""");

        var act = () => sut.CreateApDownPaymentAsync(CreateApdpRequest());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateApInvoiceAsync_Success_ReturnsApNumberAndDocEntry()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 401, "docNum": 9101}""");

        var result = await sut.CreateApInvoiceAsync(
            CreateApInvoiceRequest(whTax: [new SapWhTaxLine("PPH23", 10000m)]), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.SapDocEntry.Should().Be(401);
        result.SapApNumber.Should().Be("9101");
    }

    [Fact]
    public async Task CreateApInvoiceAsync_MultipleTapdp_SerializesEveryDownPaymentReference()
    {
        var sut = CreateSut(HttpStatusCode.OK, """{"docEntry": 401, "docNum": 9101}""", out var handler);

        await sut.CreateApInvoiceAsync(
            CreateApInvoiceRequest(tapdp:
            [
                new SapApInvoiceDpLine(301, 10000m),
                new SapApInvoiceDpLine(302, 20000m),
            ]),
            TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var tapdp = json.RootElement.GetProperty("tapdp").EnumerateArray().ToList();
        tapdp.Should().HaveCount(2);
        tapdp[0].GetProperty("baseEntryDP").GetInt32().Should().Be(301);
        tapdp[0].GetProperty("amountToDraw").GetDouble().Should().Be(10000d);
        tapdp[1].GetProperty("baseEntryDP").GetInt32().Should().Be(302);
        tapdp[1].GetProperty("amountToDraw").GetDouble().Should().Be(20000d);
    }

    [Fact]
    public async Task CreateApInvoiceAsync_NonSuccessStatus_ThrowsWithSapErrorBody()
    {
        var sut = CreateSut(HttpStatusCode.BadRequest, """{"error": "TaxCode: required"}""");

        var act = () => sut.CreateApInvoiceAsync(CreateApInvoiceRequest(
            tapdp: [new SapApInvoiceDpLine(301, 10000m)]));

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Message.Should().Contain("TaxCode: required");
    }

    private sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class FakeErpHttpHandler(Func<Uri, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(responder(request.RequestUri!));
        }
    }
}
