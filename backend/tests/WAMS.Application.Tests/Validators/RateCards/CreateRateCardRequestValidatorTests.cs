namespace WAMS.Application.Tests.Validators;

using FluentAssertions;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.Validators.RateCards;
using WAMS.Domain.Constants;
using Xunit;

public class CreateRateCardRequestValidatorTests
{
    private readonly CreateRateCardRequestValidator _sut = new();

    [Fact]
    public void Validate_ItemsWithNullTaxTypeIds_Passes()
    {
        var request = new CreateRateCardRequest(1, [new CreateRateCardItemRequest(10, 20, 100m, null, null)]);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ItemsWithTaxTypeIds_Passes()
    {
        var request = new CreateRateCardRequest(1, [new CreateRateCardItemRequest(10, 20, 100m, 2, 3)]);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoItems_Fails()
    {
        var request = new CreateRateCardRequest(1, []);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}

public class CreateRateCardRequestValidatorCostTreatmentTests
{
    private static CreateRateCardRequest Request(string? costTreatment) => new(
        VendorShadowId: 1,
        Items:
        [
            new CreateRateCardItemRequest(
                ItemShadowId: 1, UomMasterId: 1, CostValue: 10m,
                PpnTaxTypeId: null, PphTaxTypeId: null, CostTreatment: costTreatment)
        ]);

    private readonly CreateRateCardRequestValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("Dibiayakan")]
    [InlineData("TidakDibiayakan")]
    public void Accepts_null_and_canonical_values(string? value)
    {
        _validator.Validate(Request(value)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("dibiayakan")]
    [InlineData("")]
    public void Rejects_non_canonical_values(string value)
    {
        var result = _validator.Validate(Request(value));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == ErrorMessages.Validation.RateCard.InvalidCostTreatment);
    }
}
