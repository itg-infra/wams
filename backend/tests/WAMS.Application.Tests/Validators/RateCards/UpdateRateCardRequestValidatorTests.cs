namespace WAMS.Application.Tests.Validators;

using FluentAssertions;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.Validators.RateCards;
using WAMS.Domain.Constants;
using Xunit;

public class UpdateRateCardRequestValidatorTests
{
    private readonly UpdateRateCardRequestValidator _sut = new();

    [Fact]
    public void Validate_NullVendorShadowId_Passes()
    {
        var request = new UpdateRateCardRequest(null, [new CreateRateCardItemRequest(10, 20, 100m, null, null)]);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_VendorShadowIdZero_Fails()
    {
        var request = new UpdateRateCardRequest(0, [new CreateRateCardItemRequest(10, 20, 100m, null, null)]);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoItems_Fails()
    {
        var request = new UpdateRateCardRequest(null, []);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ItemCostValueZero_Fails()
    {
        var request = new UpdateRateCardRequest(null, [new CreateRateCardItemRequest(10, 20, 0m, null, null)]);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("dibiayakan")]
    [InlineData("")]
    public void Validate_ItemInvalidCostTreatment_Fails(string value)
    {
        var request = new UpdateRateCardRequest(null,
            [new CreateRateCardItemRequest(10, 20, 100m, null, null, CostTreatment: value)]);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == ErrorMessages.Validation.RateCard.InvalidCostTreatment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Dibiayakan")]
    [InlineData("TidakDibiayakan")]
    public void Validate_ItemCanonicalCostTreatment_Passes(string? value)
    {
        var request = new UpdateRateCardRequest(null,
            [new CreateRateCardItemRequest(10, 20, 100m, null, null, CostTreatment: value)]);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
