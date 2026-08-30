namespace WAMS.Api.Tests.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using WAMS.Api.Controllers.FinanceReports;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.FinanceReports;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.FinanceReports;
using Xunit;

public class FinanceReportsControllerTests
{
    private readonly IFinanceReportService service = Substitute.For<IFinanceReportService>();
    private readonly IExportService exportService = Substitute.For<IExportService>();
    private readonly FinanceReportsController controller;

    public FinanceReportsControllerTests()
    {
        controller = new FinanceReportsController(service, exportService);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "1")], "jwt"));
    }

    [Fact]
    public async Task GetAll_ReturnsPaginatedList()
    {
        var query = new DataTableQuery { Page = 1, Limit = 10 };
        service.GetAllAsync(query, 1L, Arg.Any<CancellationToken>())
            .Returns(([new ApprovedBudgetPlanPoStatusResponse(
                1, "BP.001", null, DateTime.UtcNow, "Approved", "Approved", false,
                null, null, null, null, null, [], null, 1000m, 900m, -100m)], 1));

        var result = await controller.GetAll(query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<PaginatedResponse<ApprovedBudgetPlanPoStatusResponse>>(ok.Value);
        Assert.Single(body.Data);
    }

    [Fact]
    public async Task GetDetail_ReturnsBudgetPlanDetail()
    {
        var detail = new FinanceReportDetailResponse(
            new FinanceReportHeaderResponse(1, "BP.001", "T.0001", "Draft", null, DateTime.UtcNow, "WH01", "Warehouse 1", "Lampung"),
            [], 0, 0, 0, 0, new FinanceReportBudgetRecapResponse(0, 0, 0));
        service.GetDetailAsync(1L, 1L, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await controller.GetDetail(1L, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ApiResponse<FinanceReportDetailResponse>>(ok.Value);
        Assert.Equal("BP.001", body.Data!.Header.BudgetNo);
    }

    [Fact]
    public async Task Export_FiltersByWorkOrderId_WhenProvided()
    {
        List<FinanceReportCostDetailResponse>? capturedData = null;
        service.GetCostDetailsForExportAsync(1L, "WO.001", 1L, Arg.Any<CancellationToken>())
            .Returns([new FinanceReportCostDetailResponse(
                1, "WO.001", null, null, "Product", null, false, null, null,
                1000m, false, 0, 0, false, null, 0, 1000m, "Unpaid")]);
        exportService.GetFileExtension(ExportFormat.Xlsx).Returns("xlsx");
        exportService.GetContentType(ExportFormat.Xlsx).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        exportService
            .When(x => x.ExportAsync(
                Arg.Any<Stream>(), ExportFormat.Xlsx, Arg.Any<IReadOnlyList<ExportColumnDefinition<FinanceReportCostDetailResponse>>>(),
                Arg.Do<IReadOnlyList<FinanceReportCostDetailResponse>>(d => capturedData = d.ToList()),
                "Finance Report", null, Arg.Any<CancellationToken>()))
            .Do(_ => { });

        controller.ControllerContext.HttpContext.Response.Body = new MemoryStream();
        await controller.Export(1L, "WO.001", ExportFormat.Xlsx, CancellationToken.None);

        Assert.NotNull(capturedData);
        Assert.Single(capturedData!);
        Assert.Equal("WO.001", capturedData![0].WorkOrderId);
    }
}
