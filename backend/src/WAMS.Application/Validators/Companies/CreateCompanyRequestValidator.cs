namespace WAMS.Application.Validators.Companies;

using FluentValidation;
using WAMS.Application.DTOs.Companies;
using WAMS.Domain.Constants;

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Company.CodeRequired)
            .MaximumLength(50)
            .Matches("^[A-Z0-9_-]+$").WithMessage(ErrorMessages.Validation.Company.CodeFormat);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Company.NameRequired)
            .MaximumLength(255);

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage(ErrorMessages.Validation.Common.InvalidEmailFormat);

        RuleFor(x => x.Phone)
            .MaximumLength(30);
    }
}
