namespace WAMS.Api.Tests.Controllers;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WAMS.Api.Controllers.TaxTypes;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.TaxTypes;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Domain.Enums;
using Xunit;

public class TaxTypesControllerTests
{
    private readonly ITaxTypeService _taxTypeService = Substitute.For<ITaxTypeService>();
    private readonly TaxTypesController _sut;

    public TaxTypesControllerTests()
    {
        _sut = new TaxTypesController(_taxTypeService);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([], "jwt"))
            }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithData()
    {
        var expected = new List<TaxTypeResponse> { new(1, "Ppn", "PPNin11", "PPn In 11%", 11m, true) };
        _taxTypeService.GetAllAsync(TaxCategory.Ppn, true, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetAll("Ppn", true, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<TaxTypeResponse>>>().Subject;
        response.Data.Should().BeEquivalentTo(expected);
    }
}
