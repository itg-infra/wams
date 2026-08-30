namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Application.Services.TaxTypes;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using Xunit;

public class TaxTypeServiceTests
{
    private readonly ITaxTypeRepository _taxTypeRepo = Substitute.For<ITaxTypeRepository>();
    private readonly TaxTypeService _sut;

    public TaxTypeServiceTests()
    {
        _sut = new TaxTypeService(_taxTypeRepo);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedResponses()
    {
        _taxTypeRepo.GetAllAsync(TaxCategory.Ppn, true, Arg.Any<CancellationToken>())
            .Returns([new TaxType { Id = 1, Category = TaxCategory.Ppn, Code = "PPNin11", Name = "PPn In 11%", Rate = 11m, IsActive = true }]);

        var result = await _sut.GetAllAsync(TaxCategory.Ppn, true, TestContext.Current.CancellationToken);

        result.Should().ContainSingle(r => r.Code == "PPNin11" && r.Rate == 11m);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _taxTypeRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
