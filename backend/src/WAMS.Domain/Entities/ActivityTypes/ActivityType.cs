namespace WAMS.Domain.Entities.ActivityTypes;

using WAMS.Domain.Common;

public class ActivityType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
}
