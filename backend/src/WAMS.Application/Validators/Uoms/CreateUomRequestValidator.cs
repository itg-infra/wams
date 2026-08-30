namespace WAMS.Application.Validators.Uoms;

using FluentValidation;
using WAMS.Application.DTOs.Uoms;

public class CreateUomRequestValidator : AbstractValidator<CreateUomRequest>
{
    public CreateUomRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
