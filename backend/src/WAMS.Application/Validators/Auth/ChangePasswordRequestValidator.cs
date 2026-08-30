namespace WAMS.Application.Validators.Auth;

using FluentValidation;
using WAMS.Application.DTOs.Auth;
using WAMS.Domain.Constants;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Auth.CurrentPasswordRequired);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.NewPasswordRequired)
            .MinimumLength(8).WithMessage(ErrorMessages.Validation.Common.NewPasswordMinLength);
    }
}
