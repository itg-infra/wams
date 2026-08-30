namespace WAMS.Application.Validators.BudgetPlans;

using FluentValidation;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Domain.Constants;

public class CreateBudgetPlanRequestValidator : AbstractValidator<CreateBudgetPlanRequest>
{
    public CreateBudgetPlanRequestValidator()
    {
        RuleFor(x => x.BudgetTemplateId)
            .GreaterThan(0).WithMessage(ErrorMessages.Validation.BudgetPlan.BudgetTemplateRequired);

        RuleFor(x => x.DocDate)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.DocDateRequired);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ItemShadowId).GreaterThan(0).WithMessage(ErrorMessages.Validation.BudgetPlan.ItemRequired);
            item.RuleFor(i => i.ActivityTypeId).GreaterThan(0).WithMessage(ErrorMessages.Validation.BudgetPlan.ActivityTypeRequired);
            item.RuleFor(i => i.VendorShadowId).GreaterThan(0).WithMessage(ErrorMessages.Validation.Common.VendorRequired);
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage(ErrorMessages.Validation.BudgetPlan.QuantityMustBeGreaterThanZero);
            item.RuleFor(i => i.CostValue)
                .GreaterThan(0).WithMessage(ErrorMessages.Validation.BudgetPlan.UnitCostOverrideMustBePositive)
                .When(i => i.CostValue.HasValue);
        });

        // SpkShadowId on a cost item must reference an SPK that is part of the base document list.
        RuleFor(x => x.Items)
            .Must((req, items) => items.All(i =>
                i.SpkShadowId == null ||
                (req.SpkShadowIds != null && req.SpkShadowIds.Contains(i.SpkShadowId.Value))))
            .WithMessage(ErrorMessages.Validation.BudgetPlan.SpkReferenceMustBeInBaseList)
            .When(x => x.Items != null && x.Items.Any(i => i.SpkShadowId.HasValue));
    }
}
