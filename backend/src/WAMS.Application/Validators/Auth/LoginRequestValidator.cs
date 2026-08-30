namespace WAMS.Application.Validators.Auth;

using FluentValidation;
using WAMS.Application.DTOs.Auth;
using WAMS.Domain.Constants;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.EmailRequired)
            .EmailAddress().WithMessage(ErrorMessages.Validation.Common.InvalidEmailFormat);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.PasswordRequired);

        RuleFor(x => x.CompanyId)
            .GreaterThan(0).WithMessage(ErrorMessages.Validation.Auth.CompanySelectionRequired);
    }
}
