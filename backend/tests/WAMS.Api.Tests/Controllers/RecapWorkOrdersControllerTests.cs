namespace WAMS.Api.Tests.Controllers;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using WAMS.Api.Controllers.RecapWorkOrders;
using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.RecapWorkOrders;
using Xunit;

public class RecapWorkOrdersControllerTests
{
    private readonly IRecapWorkOrderService _service = Substitute.For<IRecapWorkOrderService>();
    private readonly IRecapWorkOrderPdfRenderer _renderer = Substitute.For<IRecapWorkOrderPdfRenderer>();
    private readonly IPdfMetadataResolver _metadataResolver = Substitute.For<IPdfMetadataResolver>();
    private readonly RecapWorkOrdersController _sut;

    public RecapWorkOrdersControllerTests()
    {
        _sut = new RecapWorkOrdersController(
            _service,
            Substitute.For<IExportService>(),
            Options.Create(new ExportOptions()),
            Substitute.For<IAuditLogService>(),
            _renderer,
            _metadataResolver);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-test";
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "17"),
                new Claim(ClaimTypes.Role, "WAREHOUSE_ADMIN"),
            ], "jwt"));
    }

    [Fact]
    public async Task ExportById_returns_the_rendered_pdf_for_the_requested_recap()
    {
        var recap = Recap();
        var metadata = new PdfReportMetadata("Recap Work Order", "Company", "CMP", null, new DateTime(2026, 9, 7), null);
        var pdf = new byte[] { 1, 2, 3 };
        _service.GetByIdAsync(41, 17, Arg.Any<CancellationToken>()).Returns(recap);
        _metadataResolver.ResolveAsync("Recap Work Order", Arg.Any<CancellationToken>()).Returns(metadata);
        _renderer.Render(recap, metadata).Returns(pdf);

        var result = await _sut.ExportById(41, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
        file.FileContents.Should().Equal(pdf);
        file.FileDownloadName.Should().StartWith("Recap-WO-BP-2609000001-");
        file.FileDownloadName.Should().EndWith(".pdf");
    }

    private static RecapWorkOrderDetailResponse Recap()
    {
        var header = new RecapBpHeaderResponse(
            "BP-2609000001", "BT-01", "Approved", null,
            new DateTime(2026, 9, 7), "WH-01", "Warehouse", "Jakarta");
        return new RecapWorkOrderDetailResponse(
            41, 99, "Approved", "Reviewer", new DateTime(2026, 9, 7), null,
            new RecapPlanResponse(header, [], [], 10, 8, 2),
            new RecapRealizationResponse(header, [], 10, 8, 2, 80));
    }
}
