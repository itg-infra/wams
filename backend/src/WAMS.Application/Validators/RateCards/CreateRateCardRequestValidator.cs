namespace WAMS.Application.Validators.RateCards;

using FluentValidation;
using WAMS.Application.DTOs.RateCards;
using WAMS.Domain.Constants;

public class CreateRateCardItemRequestValidator : AbstractValidator<CreateRateCardItemRequest>
{
    public CreateRateCardItemRequestValidator()
    {
        RuleFor(i => i.ItemShadowId).GreaterThan(0);
        RuleFor(i => i.UomMasterId).GreaterThan(0);
        RuleFor(i => i.CostValue).GreaterThan(0);
        RuleFor(i => i.CostTreatment)
            .Must(v => v is null || CostTreatments.All.Contains(v))
            .WithMessage(ErrorMessages.Validation.RateCard.InvalidCostTreatment);
    }
}

public class CreateRateCardRequestValidator : AbstractValidator<CreateRateCardRequest>
{
    public CreateRateCardRequestValidator()
    {
        RuleFor(x => x.VendorShadowId).GreaterThan(0).WithMessage(ErrorMessages.Validation.Common.VendorRequired);
        RuleFor(x => x.Items).NotEmpty().WithMessage(ErrorMessages.Validation.RateCard.AtLeastOneItemRequired);
        RuleForEach(x => x.Items).SetValidator(new CreateRateCardItemRequestValidator());
    }
}

public class UpdateRateCardRequestValidator : AbstractValidator<UpdateRateCardRequest>
{
    public UpdateRateCardRequestValidator()
    {
        RuleFor(x => x.VendorShadowId).GreaterThan(0)
            .When(x => x.VendorShadowId.HasValue)
            .WithMessage(ErrorMessages.Validation.Common.VendorRequired);
        RuleFor(x => x.Items).NotEmpty().WithMessage(ErrorMessages.Validation.RateCard.AtLeastOneItemRequired);
        RuleForEach(x => x.Items).SetValidator(new CreateRateCardItemRequestValidator());
    }
}
