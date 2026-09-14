namespace WAMS.Domain.Entities.Users;

using WAMS.Domain.Entities.Roles;

public class UserCompanyRole
{
    public long UserCompanyId { get; set; }
    public long RoleId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public UserCompany UserCompany { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
