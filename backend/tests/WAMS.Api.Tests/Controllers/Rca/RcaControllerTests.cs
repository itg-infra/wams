namespace WAMS.Api.Tests.Controllers;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WAMS.Api.Controllers.Rca;
using WAMS.Application.DTOs.Rca;
using WAMS.Application.Interfaces.Rca;
using Xunit;

public class RcaControllerTests
{
    private readonly IRcaService _rcaService = Substitute.For<IRcaService>();
    private readonly IRcaPdfRenderer _renderer = Substitute.For<IRcaPdfRenderer>();
    private readonly RcaController _sut;

    private static readonly DateOnly From = new(2026, 2, 13);
    private static readonly DateOnly To   = new(2026, 2, 19);

    private static readonly RcaDocument FakeDoc = new(
        "RCA/GCU/WH001/19022026", "Company", null,
        "WH001", "Medan", From, To, [], [], new RcaSignatures(null, new List<string?> { null }));

    public RcaControllerTests()
    {
        _sut = new RcaController(_rcaService, _renderer);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-test";
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "1"),
                new Claim(ClaimTypes.Role, "WAREHOUSE_ADMIN"),
            ], "jwt"));
    }

    [Fact]
    public async Task Export_ValidDates_ReturnsPdfFile()
    {
        var pdfBytes = new byte[] { 1, 2, 3 };
        _rcaService.GetDocumentAsync(Arg.Any<RcaQuery>(), 1, Arg.Any<CancellationToken>())
            .Returns(FakeDoc);
        _renderer.Render(FakeDoc).Returns(pdfBytes);

        var result = await _sut.Export("WH001", From, To, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
        file.FileContents.Should().Equal(pdfBytes);
        file.FileDownloadName.Should().StartWith("RCA-WH001-");
        file.FileDownloadName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Export_DateFromAfterDateTo_ReturnsBadRequest()
    {
        var result = await _sut.Export("WH001", To, From, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _rcaService.DidNotReceive().GetDocumentAsync(
            Arg.Any<RcaQuery>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_PassesCorrectQueryToService()
    {
        _rcaService.GetDocumentAsync(Arg.Any<RcaQuery>(), 1, Arg.Any<CancellationToken>())
            .Returns(FakeDoc);
        _renderer.Render(FakeDoc).Returns([]);

        await _sut.Export("WH001", From, To, CancellationToken.None);

        await _rcaService.Received(1).GetDocumentAsync(
            Arg.Is<RcaQuery>(q =>
                q.WarehouseCode == "WH001" &&
                q.DateFrom == From &&
                q.DateTo == To),
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_PassesDocumentToRenderer()
    {
        _rcaService.GetDocumentAsync(Arg.Any<RcaQuery>(), 1, Arg.Any<CancellationToken>())
            .Returns(FakeDoc);
        _renderer.Render(FakeDoc).Returns([]);

        await _sut.Export("WH001", From, To, CancellationToken.None);

        _renderer.Received(1).Render(FakeDoc);
    }
}
