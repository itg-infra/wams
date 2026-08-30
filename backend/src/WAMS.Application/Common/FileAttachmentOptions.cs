namespace WAMS.Application.Common;

public sealed class FileAttachmentOptions
{
    public const string SectionName = "FileAttachments";

    public string RootPath { get; set; } = Path.Combine("storage", "attachments");
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
    public long MaxTotalSizeBytesPerEntity { get; set; } = 50 * 1024 * 1024;
    public int MaxAttachmentsPerEntity { get; set; } = 10;
    public List<string> AllowedMimeTypes { get; set; } =
    [
        "application/pdf",
        "image/png",
        "image/jpeg",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ];
}
