namespace WAMS.Application.Validators.Auth;

using FluentValidation;
using WAMS.Application.DTOs.Auth;
using WAMS.Domain.Constants;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Fullname)
            .MaximumLength(100)
            .When(x => x.Fullname is not null);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .When(x => x.Email is not null)
            .WithMessage(ErrorMessages.Validation.Auth.CurrentPasswordRequired);

        RuleFor(x => x)
            .Must(x => x.Fullname is not null || x.Email is not null)
            .WithMessage("At least one profile field must be provided");
    }
}
