namespace WAMS.Application.Validators.Files;

using FluentValidation;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Files;
using WAMS.Domain.Constants;

public sealed class FileUploadRequestValidator : AbstractValidator<FileUploadRequest>
{
    private static readonly string[] InvalidEntityTypeCharacters = ["..", "/", "\\"];

    public FileUploadRequestValidator(IOptions<FileAttachmentOptions> options)
    {
        var fileOptions = options.Value;
        var allowedMimeTypes = fileOptions.AllowedMimeTypes
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage(ErrorMessages.Validation.FileUpload.EntityTypeRequired)
            .MaximumLength(100).WithMessage(ErrorMessages.Validation.FileUpload.EntityTypeTooLong)
            .Matches("^[a-z0-9-]+$").WithMessage(ErrorMessages.Validation.FileUpload.EntityTypeFormat)
            .Must(x => x is not null && InvalidEntityTypeCharacters.All(invalid => !x.Contains(invalid, StringComparison.Ordinal)))
            .WithMessage(ErrorMessages.Validation.FileUpload.EntityTypeInvalidPathCharacters);

        RuleFor(x => x.EntityId)
            .GreaterThan(0).WithMessage(ErrorMessages.Validation.FileUpload.EntityIdRequired);

        RuleFor(x => x.Files)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ErrorMessages.FileAttachment.AtLeastOneRequired)
            .Must(f => f!.Count > 0).WithMessage(ErrorMessages.FileAttachment.AtLeastOneRequired)
            .Must(f => f!.Count <= fileOptions.MaxAttachmentsPerEntity)
            .WithMessage(ErrorMessages.Validation.FileUpload.MaxAttachmentsExceeded(fileOptions.MaxAttachmentsPerEntity));

        When(x => x.Files is not null && x.Files.Count > 0, () =>
        {
            RuleForEach(x => x.Files).ChildRules(file =>
            {
                file.RuleFor(f => f.Length)
                    .Cascade(CascadeMode.Stop)
                    .GreaterThan(0).WithMessage(ErrorMessages.Validation.FileUpload.FileRequired)
                    .LessThanOrEqualTo(fileOptions.MaxFileSizeBytes)
                    .WithMessage(ErrorMessages.Validation.FileUpload.FileSizeExceeds(FileSizeFormatter.Format(fileOptions.MaxFileSizeBytes)));

                file.RuleFor(f => f.ContentType)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage(ErrorMessages.Validation.FileUpload.ContentTypeRequired)
                    .Must(allowedMimeTypes.Contains)
                    .WithMessage(ErrorMessages.Validation.FileUpload.FileTypeNotAllowed);

                file.RuleFor(f => f.FileName)
                    .NotEmpty().WithMessage(ErrorMessages.Validation.FileUpload.FileNameRequired);
            });
        });
    }
}
