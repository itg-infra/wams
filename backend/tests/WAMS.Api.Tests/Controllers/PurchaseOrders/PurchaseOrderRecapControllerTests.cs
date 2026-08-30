namespace WAMS.Api.Tests.Controllers;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using WAMS.Api.Controllers.PurchaseOrders;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.DTOs.Rfba;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rfba;
using Xunit;

public class PurchaseOrderRecapControllerTests
{
    private readonly IPurchaseOrderService service = Substitute.For<IPurchaseOrderService>();
    private readonly IExportService exportService = Substitute.For<IExportService>();
    private readonly IRfbaFormPdfRenderer rfbaRenderer = Substitute.For<IRfbaFormPdfRenderer>();
    private readonly IPdfMetadataResolver metadataResolver = Substitute.For<IPdfMetadataResolver>();
    private readonly PurchaseOrderRecapController controller;

    public PurchaseOrderRecapControllerTests()
    {
        var exportOptions = Options.Create(new ExportOptions { MaxRows = 5000 });
        controller = new PurchaseOrderRecapController(service, exportService, exportOptions, rfbaRenderer, metadataResolver);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "1")], "jwt"));
    }

    private static ApprovedBudgetPlanPoStatusResponse SampleListRow() => new(
        1, "BP.001", null, DateTime.UtcNow, "Approved", "Approved", true,
        3, "V.001", "AC INDO PERKASA", null, null, [], null, 1000m, 900m, -100m);

    [Fact]
    public async Task GetApdpList_ReturnsPaginatedList()
    {
        var query = new DataTableQuery { Page = 1, Limit = 10 };
        service.GetRecapAsync(true, 1L, query, Arg.Any<CancellationToken>())
            .Returns(([SampleListRow()], 1));

        var result = await controller.GetApdpList(query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<PaginatedResponse<ApprovedBudgetPlanPoStatusResponse>>(ok.Value);
        Assert.Single(body.Data);
        await service.Received(1).GetRecapAsync(true, 1L, query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ApprovedBudgetPlanResponse_exposes_all_generated()
    {
        SampleListRow().AllGenerated.Should().BeFalse();
    }

    [Fact]
    public async Task GetNonApdpList_PassesIsRfbaFalse()
    {
        var query = new DataTableQuery { Page = 1, Limit = 10 };
        service.GetRecapAsync(false, 1L, query, Arg.Any<CancellationToken>())
            .Returns(([], 0));

        await controller.GetNonApdpList(query, CancellationToken.None);

        await service.Received(1).GetRecapAsync(false, 1L, query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetApdpDetail_ReturnsDetail()
    {
        var detail = new RecapPurchaseOrderDetailResponse(
            1, "PO-2607000001", "AC INDO PERKASA", "Generated", null,
            DateTime.UtcNow, DateTime.UtcNow, "System Administrator", DateTime.UtcNow, "System Administrator",
            [], [], 33450m, 1);
        service.GetRecapDetailAsync(true, 1L, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await controller.GetApdpDetail(1L, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ApiResponse<RecapPurchaseOrderDetailResponse>>(ok.Value);
        Assert.Equal("PO-2607000001", body.Data!.Code);
        await service.Received(1).GetRecapDetailAsync(true, 1L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetNonApdpDetail_PassesIsRfbaFalse()
    {
        var detail = new RecapPurchaseOrderDetailResponse(
            2, "PO-2607000002", "AC INDO PERKASA", "Generated", null,
            DateTime.UtcNow, DateTime.UtcNow, "System Administrator", null, null,
            [], [], 0m, 0);
        service.GetRecapDetailAsync(false, 2L, Arg.Any<CancellationToken>()).Returns(detail);

        await controller.GetNonApdpDetail(2L, CancellationToken.None);

        await service.Received(1).GetRecapDetailAsync(false, 2L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExportApdpRfbaPdf_renders_the_recap_detail()
    {
        var detail = new RecapPurchaseOrderDetailResponse(
            1, "PO-001", "Vendor", "Generated", null,
            DateTime.UtcNow, DateTime.UtcNow, "Maker", null, null,
            [],
            [new PurchaseOrderItemResponse(
                1, 11, 21, "ITEM", "RFBA item", "COA", "Cost", 31, "V", "Vendor",
                41, "EA", "Each", true, "BL-1", 10m, 2m, 20m, 0,
                null, 0m, null, 0m, 0m, 0m, 20m, null)],
            20m, 1);
        service.GetRecapDetailAsync(true, 1L, Arg.Any<CancellationToken>()).Returns(detail);
        metadataResolver.ResolveAsync("RFBA", Arg.Any<CancellationToken>())
            .Returns(new PdfReportMetadata("RFBA", "Company", "CMP", null, DateTime.UtcNow, "Jakarta"));
        rfbaRenderer.Render(Arg.Any<RfbaFormDocument>(), Arg.Any<PdfReportMetadata>()).Returns([1, 2]);

        var result = await controller.ExportApdpRfbaPdf(1L, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        file.FileDownloadName.Should().Be("RFBA-PO-001.pdf");
        await service.Received(1).GetRecapDetailAsync(true, 1L, Arg.Any<CancellationToken>());
    }
}
