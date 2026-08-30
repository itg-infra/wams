namespace WAMS.Application.DTOs.Files;

using WAMS.Application.Interfaces.Files;

public static class FileSizeFormatter
{
    public static string Format(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

public sealed record FileUploadRequest(
    string EntityType,
    long EntityId,
    IReadOnlyList<IUploadFile>? Files
);

public sealed record FileAttachmentResponse(
    long Id,
    string EntityType,
    long EntityId,
    string OriginalFileName,
    string ContentType,
    string FileSize,
    long FileSizeRaw,
    long UploadedByUserId,
    string? UploadedByName,
    DateTime UploadedAt,
    string Url
);

public sealed record FileDownloadResponse(
    long Id,
    string EntityType,
    long EntityId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTime UploadedAt,
    Stream Content,
    DateTimeOffset? LastModifiedUtc
);
