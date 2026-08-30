namespace WAMS.Domain.Entities.Roles;

using WAMS.Domain.Entities.Users;

public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
