namespace WAMS.Application.Validators.PurchaseOrders;

using FluentValidation;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Domain.Constants;

public class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    public CreatePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.VendorShadowId)
            .GreaterThan(0).WithMessage(ErrorMessages.Validation.Common.VendorRequired);

        RuleFor(x => x.DocDate)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.DocDateRequired);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);

        RuleForEach(x => x.Items)
            .GreaterThan(0L).WithMessage(ErrorMessages.Validation.Common.InvalidBudgetPlanItemId);
    }
}
