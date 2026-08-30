namespace WAMS.Application.Services.Files;

using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Files;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Files;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Files;
using WAMS.Domain.Exceptions;
using DomainValidationException = Domain.Exceptions.ValidationException;

public sealed class FileAttachmentService(
    IFileAttachmentRepository attachmentRepository,
    IFileAttachmentStorage storage,
    IFileAttachmentEntityResolver entityResolver,
    IFileMimeDetector fileMimeDetector,
    IValidator<FileUploadRequest> uploadValidator,
    IUnitOfWork unitOfWork,
    ILogger<FileAttachmentService> logger,
    IOptions<FileAttachmentOptions> options
) : IFileAttachmentService
{
    private readonly FileAttachmentOptions _options = options.Value;

    public async Task<List<FileAttachmentResponse>> UploadAsync(
        long userId,
        FileUploadRequest request,
        CancellationToken ct = default
    )
    {
        await ValidateUploadRequestAsync(request, ct);

        var files = request.Files ?? throw new DomainValidationException(new Dictionary<string, string[]>
        {
            ["files"] = [ErrorMessages.FileAttachment.AtLeastOneRequired],
        });

        var entity = await ResolveEntityAsync(userId, request.EntityType, request.EntityId, ct);

        if (!entity.CanModify)
            throw new ForbiddenException(ErrorMessages.FileAttachment.CannotModifyCurrentState);

        var currentCount = await attachmentRepository.CountByEntityAsync(entity.EntityType, entity.EntityId, ct);

        if (currentCount + files.Count > _options.MaxAttachmentsPerEntity)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["files"] = [ErrorMessages.FileAttachment.WouldExceedMax(files.Count, _options.MaxAttachmentsPerEntity)],
            });
        }

        var currentTotalSize = await attachmentRepository.SumSizeByEntityAsync(entity.EntityType, entity.EntityId, ct);
        var incomingTotalSize = files.Sum(f => f.Length);

        if (currentTotalSize + incomingTotalSize > _options.MaxTotalSizeBytesPerEntity)
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                ["files"] = [ErrorMessages.FileAttachment.WouldExceedTotalSize(FileSizeFormatter.Format(_options.MaxTotalSizeBytesPerEntity))],
            });
        }

        // Gate 3: read only the magic-byte header (8 bytes) per file - no full-file buffering
        var fileInfos = new List<(IUploadFile File, string SafeName, string StorageKey, Stream Stream, string DetectedMime)>(files.Count);
        foreach (var file in files)
        {
            var safeName = SanitizeDisplayFileName(file.FileName);
            var stream = await file.OpenReadStreamAsync(ct);

            var header = new byte[8];
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, 8), ct);
            stream.Seek(0, SeekOrigin.Begin); // IFormFile streams are always seekable (memory or temp-file backed)

            var detectedMime = fileMimeDetector.Detect(header, bytesRead);
            if (detectedMime is null)
            {
                await stream.DisposeAsync();
                foreach (var (_, _, _, s, _) in fileInfos) await s.DisposeAsync();
                throw new DomainValidationException(new Dictionary<string, string[]>
                {
                    ["files"] = [ErrorMessages.FileAttachment.FileTypeNotSupported],
                });
            }

            fileInfos.Add((file, safeName, BuildStorageKey(entity.EntityType, entity.EntityId, safeName), stream, detectedMime));
        }

        // Upload all to storage; track keys so we can roll back on failure
        var storedKeys = new List<string>(fileInfos.Count);
        try
        {
            try
            {
                foreach (var info in fileInfos)
                {
                    await storage.SaveAsync(info.Stream, info.StorageKey, info.DetectedMime, ct);
                    storedKeys.Add(info.StorageKey);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to store files for {EntityType}:{EntityId}", entity.EntityType, entity.EntityId);
                await RollbackStoredFilesAsync(storedKeys, CancellationToken.None);
                throw;
            }

            var attachments = fileInfos.Select(info => new FileAttachment
            {
                CompanyId = entity.CompanyId,
                EntityType = entity.EntityType,
                EntityId = entity.EntityId,
                OriginalFileName = info.SafeName,
                ContentType = info.DetectedMime,
                FileSize = info.File.Length,
                StorageKey = info.StorageKey,
                UploadedByUserId = userId
            }).ToList();

            try
            {
                await attachmentRepository.CreateManyAsync(attachments, ct);
                await unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await RollbackStoredFilesAsync(storedKeys, CancellationToken.None);
                throw;
            }

            logger.LogInformation(
                "Uploaded {Count} file attachment(s) for {EntityType}:{EntityId} by user {UserId}",
                attachments.Count,
                entity.EntityType,
                entity.EntityId,
                userId);

            var newIds = attachments.Select(a => a.Id).ToList();
            var created = await attachmentRepository.GetByIdsAsync(newIds, entity.EntityType, entity.EntityId, ct);
            return [.. created.Select(Map)];
        }
        finally
        {
            foreach (var (_, _, _, s, _) in fileInfos) await s.DisposeAsync();
        }
    }

    public async Task<List<FileAttachmentResponse>> GetByEntityAsync(
        long userId,
        string entityType,
        long entityId,
        CancellationToken ct = default
    )
    {
        var entity = await ResolveEntityAsync(userId, entityType, entityId, ct);
        var attachments = await attachmentRepository.GetByEntityAsync(entity.EntityType, entity.EntityId, ct);

        return [.. attachments.Select(Map)];
    }

    public async Task<FileDownloadResponse> DownloadAsync(
        long userId,
        string entityType,
        long entityId,
        long fileId,
        CancellationToken ct = default
    )
    {
        var entity = await ResolveEntityAsync(userId, entityType, entityId, ct);
        var attachment = await attachmentRepository.GetByIdAsync(fileId, entity.EntityType, entity.EntityId, ct)
            ?? throw new NotFoundException("File attachment", fileId);

        logger.LogInformation(
            "File attachment {FileId} downloaded for {EntityType}:{EntityId} by user {UserId}",
            attachment.Id,
            entity.EntityType,
            entity.EntityId,
            userId);

        var storedFile = await storage.OpenReadAsync(attachment.StorageKey, ct);

        return new FileDownloadResponse(
            attachment.Id,
            attachment.EntityType,
            attachment.EntityId,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.FileSize,
            attachment.CreatedAt,
            storedFile.Content,
            storedFile.LastModifiedUtc
        );
    }

    public async Task DeleteAsync(
        long userId,
        string entityType,
        long entityId,
        long fileId,
        CancellationToken ct = default
    )
    {
        var entity = await ResolveEntityAsync(userId, entityType, entityId, ct);

        if (!entity.CanModify)
            throw new ForbiddenException(ErrorMessages.FileAttachment.CannotModifyCurrentState);

        var attachment = await attachmentRepository.GetByIdAsync(fileId, entity.EntityType, entity.EntityId, ct)
            ?? throw new NotFoundException("File attachment", fileId);

        var canDelete = attachment.UploadedByUserId == userId || entity.OwnerUserId == userId;
        if (!canDelete)
            throw new ForbiddenException(ErrorMessages.FileAttachment.NoPermissionToDelete);

        await attachmentRepository.DeleteAsync(attachment, ct);
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation(
            "File attachment {FileId} deleted for {EntityType}:{EntityId} by user {UserId}",
            fileId,
            entity.EntityType,
            entity.EntityId,
            userId);

        await TryDeleteStoredFileAsync(attachment.StorageKey, ct);
    }

    private async Task ValidateUploadRequestAsync(FileUploadRequest request, CancellationToken ct)
    {
        var validation = await uploadValidator.ValidateAsync(request, ct);
        if (validation.IsValid)
            return;

        var errors = validation.Errors
            .GroupBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => ToCamelCase(g.Key),
                g => g.Select(x => x.ErrorMessage).Distinct().ToArray(),
                StringComparer.OrdinalIgnoreCase);

        throw new DomainValidationException(errors);
    }

    private async Task<FileAttachmentEntityContext> ResolveEntityAsync(
        long userId,
        string entityType,
        long entityId,
        CancellationToken ct
    )
        => await entityResolver.ResolveAsync(userId, entityType, entityId, ct)
            ?? throw new NotFoundException($"{entityType}", entityId);

    private async Task TryDeleteStoredFileAsync(string storageKey, CancellationToken ct)
    {
        try
        {
            await storage.DeleteAsync(storageKey, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete stored file {StorageKey}", storageKey);
        }
    }

    private async Task RollbackStoredFilesAsync(IEnumerable<string> storageKeys, CancellationToken ct)
    {
        foreach (var key in storageKeys)
            await TryDeleteStoredFileAsync(key, ct);
    }

    private static FileAttachmentResponse Map(FileAttachment attachment) => new(
        attachment.Id,
        attachment.EntityType,
        attachment.EntityId,
        attachment.OriginalFileName,
        attachment.ContentType,
        FileSizeFormatter.Format(attachment.FileSize),
        attachment.FileSize,
        attachment.UploadedByUserId,
        attachment.UploadedBy?.Fullname,
        attachment.CreatedAt,
        $"api/v1/files/{attachment.EntityType}/{attachment.EntityId}/{attachment.Id}"
    );

    private static string BuildStorageKey(string entityType, long entityId, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var safeExtension = extension
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '.')
            .ToArray();

        return $"{entityType}/{entityId}/{Guid.NewGuid():N}{new string(safeExtension)}";
    }

    private static string SanitizeDisplayFileName(string fileName)
    {
        var trimmed = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return "file";

        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string([.. trimmed.Select(ch => invalidChars.Contains(ch) || char.IsControl(ch) ? '_' : ch)]);

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "file";

        return value.Length == 1
            ? value.ToLowerInvariant()
            : char.ToLowerInvariant(value[0]) + value[1..];
    }
}
