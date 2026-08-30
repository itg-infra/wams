namespace WAMS.Api.Tests.Controllers;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WAMS.Api.Controllers.RateCards;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.TaxTypes;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.RateCards;
using Microsoft.Extensions.Options;
using Xunit;

public class RateCardsControllerPphTests
{
    private readonly IRateCardService _rateCardService = Substitute.For<IRateCardService>();
    private readonly IRateCardRepository _rateCardRepo = Substitute.For<IRateCardRepository>();
    private readonly IExportService _exportService = Substitute.For<IExportService>();
    private readonly IPphLookupService _pphLookupService = Substitute.For<IPphLookupService>();
    private readonly RateCardsController _sut;

    public RateCardsControllerPphTests()
    {
        var exportOptions = Options.Create(new ExportOptions());
        _sut = new RateCardsController(_rateCardService, _rateCardRepo, _exportService, exportOptions, _pphLookupService);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([], "jwt"))
            }
        };
    }

    [Fact]
    public async Task GetPphSuggestions_ReturnsOkWithData()
    {
        var expected = new List<TaxTypeResponse> { new(1, "Pph", "P23c", "Hutang PPH Pasal 23 - 2", 2.0m, true) };
        _pphLookupService.GetOrRefreshAsync(42, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetPphSuggestions(42, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<TaxTypeResponse>>>().Subject;
        response.Data.Should().BeEquivalentTo(expected);
    }
}
