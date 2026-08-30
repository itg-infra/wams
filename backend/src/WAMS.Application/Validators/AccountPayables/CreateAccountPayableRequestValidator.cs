namespace WAMS.Application.Validators.AccountPayables;

using FluentValidation;
using WAMS.Application.DTOs.AccountPayables;
using WAMS.Domain.Constants;

public class CreateAccountPayableRequestValidator : AbstractValidator<CreateAccountPayableRequest>
{
    public CreateAccountPayableRequestValidator()
    {
        RuleFor(x => x.VendorShadowId)
            .GreaterThan(0).WithMessage(ErrorMessages.Validation.Common.VendorRequired);

        RuleFor(x => x.DocDate)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.DocDateRequired);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);

        RuleForEach(x => x.Items)
            .GreaterThan(0L).WithMessage(ErrorMessages.Validation.Common.InvalidBudgetPlanItemId);

        RuleFor(x => x.Remark)
            .MaximumLength(500).WithMessage(ErrorMessages.Validation.AccountPayable.RemarkMaxLength)
            .When(x => x.Remark is not null);
    }
}
