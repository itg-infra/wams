namespace WAMS.Application.Validators.Notifications;

using FluentValidation;
using WAMS.Application.DTOs.Notifications;
using WAMS.Domain.Constants;

public class SendTestNotificationRequestValidator : AbstractValidator<SendTestNotificationRequest>
{
    public SendTestNotificationRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Notification.TypeRequired)
            .MaximumLength(100);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Notification.TitleRequired)
            .MaximumLength(200);

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Notification.MessageRequired)
            .MaximumLength(500);

        RuleFor(x => x.ReferenceType)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Notification.ReferenceTypeRequired)
            .MaximumLength(100);

        RuleFor(x => x.ReferenceId)
            .NotEmpty().WithMessage(ErrorMessages.Validation.Notification.ReferenceIdRequired)
            .MaximumLength(100);
    }
}
