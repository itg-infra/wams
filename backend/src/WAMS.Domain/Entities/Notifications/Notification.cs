namespace WAMS.Domain.Entities.Notifications;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;

public class Notification : BaseEntity
{
    public long CompanyId { get; set; }
    public long RecipientUserId { get; set; }
    public long? ActorUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public Company Company { get; set; } = null!;
    public User RecipientUser { get; set; } = null!;
    public User? ActorUser { get; set; }
}
