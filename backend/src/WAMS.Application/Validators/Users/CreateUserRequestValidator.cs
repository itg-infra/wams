namespace WAMS.Application.Validators.Users;

using FluentValidation;
using WAMS.Application.DTOs.Users;
using WAMS.Domain.Constants;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.EmailRequired)
            .EmailAddress().WithMessage(ErrorMessages.Validation.Common.InvalidEmailFormat)
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Common.PasswordRequired)
            .MinimumLength(8).WithMessage(ErrorMessages.Validation.Common.PasswordMinLength);

        RuleFor(x => x.Fullname)
            .NotEmpty().WithMessage(ErrorMessages.Validation.User.FullnameRequired)
            .MaximumLength(100);

        When(x => x.WarehouseIds != null, () =>
        {
            RuleFor(x => x.WarehouseIds!)
                .Must(ids => ids.Count > 0)
                .WithMessage(ErrorMessages.Validation.User.WarehouseIdsMustHaveEntry)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage(ErrorMessages.Validation.User.WarehouseIdsNoDuplicates);
        });

        When(x => x.PrimaryWarehouseId.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.WarehouseIds != null && x.WarehouseIds.Contains(x.PrimaryWarehouseId!.Value))
                .WithMessage(ErrorMessages.Validation.User.PrimaryWarehouseIdMustBeInWarehouseIds);
        });
    }
}
