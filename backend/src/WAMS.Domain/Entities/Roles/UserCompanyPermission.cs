namespace WAMS.Domain.Entities.Roles;

using WAMS.Domain.Entities.Users;

public class UserCompanyPermission
{
    public long UserCompanyId { get; set; }
    public long PermissionId { get; set; }

    /// <summary>
    /// true = explicit grant; false = explicit deny.
    /// </summary>
    public bool IsGranted { get; set; }

    public long GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
    public string? Constraints { get; set; }

    public UserCompany UserCompany { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
    public User GrantedByUser { get; set; } = null!;
}
