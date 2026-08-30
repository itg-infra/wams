namespace WAMS.Domain.Entities.Files;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;

public sealed class FileAttachment : BaseEntity
{
    public long CompanyId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public long UploadedByUserId { get; set; }

    public Company Company { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}
