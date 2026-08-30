namespace WAMS.Domain.Entities.Roles;

using WAMS.Domain.Entities.Users;

public class UserPermission
{
    public long UserId { get; set; }
    public long PermissionId { get; set; }

    /// <summary>
    /// true = explicit grant (adds a permission beyond the user's roles)
    /// false = explicit deny (removes a permission even if the role grants it)
    /// </summary>
    public bool IsGranted { get; set; }

    public long GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// null = permanent override; set to expire temporary elevations / restrictions
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    public string? Reason { get; set; }

    /// <summary>
    /// Optional JSON constraints (e.g. {"max_approval_amount": 500000000}).
    /// Populated for future enforcement - not yet evaluated by HasPermissionAsync.
    /// </summary>
    public string? Constraints { get; set; }

    public User User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
    public User GrantedByUser { get; set; } = null!;
}
