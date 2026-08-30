namespace WAMS.Application.Validators.Auth;

using FluentValidation;
using WAMS.Application.DTOs.Users;
using WAMS.Domain.Constants;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.NewPasswordRequired)
            .MinimumLength(8).WithMessage(ErrorMessages.Validation.Common.NewPasswordMinLength);
    }
}
