namespace WAMS.Application.Validators.WorkOrders;

using FluentValidation;
using WAMS.Application.DTOs.WorkOrders;
using WAMS.Domain.Constants;

file sealed class FumigationDetailValidator : AbstractValidator<CreateFumigationDetailRequest>
{
    private const decimal MaxDosage = 9_999_999m;
    private const decimal MaxTemperature = 1_000m;
    private const decimal MinTemperature = -100m;

    public FumigationDetailValidator()
    {
        When(x => x.InitialTemperature.HasValue, () =>
            RuleFor(x => x.InitialTemperature!.Value)
                .InclusiveBetween(MinTemperature, MaxTemperature)
                .WithMessage(ErrorMessages.Validation.WorkOrder.TemperatureRange("Initial", MinTemperature, MaxTemperature)));

        When(x => x.FinalTemperature.HasValue, () =>
            RuleFor(x => x.FinalTemperature!.Value)
                .InclusiveBetween(MinTemperature, MaxTemperature)
                .WithMessage(ErrorMessages.Validation.WorkOrder.TemperatureRange("Final", MinTemperature, MaxTemperature)));

        When(x => x.PhosphineDosage.HasValue, () =>
            Dosage(RuleFor(x => x.PhosphineDosage!.Value), "Phosphine"));

        When(x => x.MethylBromideDosage.HasValue, () =>
            Dosage(RuleFor(x => x.MethylBromideDosage!.Value), "Methyl bromide"));

        When(x => x.SulphurFluorideDosage.HasValue, () =>
            Dosage(RuleFor(x => x.SulphurFluorideDosage!.Value), "Sulphur fluoride"));
    }

    private static void Dosage(IRuleBuilder<CreateFumigationDetailRequest, decimal> rule, string name) =>
        rule.InclusiveBetween(0m, MaxDosage).WithMessage(ErrorMessages.Validation.WorkOrder.DosageRange(name, MaxDosage));
}

public sealed class UpdateWorkOrderRequestValidator : AbstractValidator<UpdateWorkOrderRequest>
{
    public UpdateWorkOrderRequestValidator()
    {
        When(x => x.PicUserId.HasValue, () =>
            RuleFor(x => x.PicUserId!.Value).GreaterThan(0));

        When(x => x.Fumigation is not null, () =>
            RuleFor(x => x.Fumigation!).SetValidator(new FumigationDetailValidator()));
    }
}
