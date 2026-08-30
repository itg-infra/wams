namespace WAMS.Domain.Entities.Auth;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Users;

public class RefreshToken : BaseEntity
{
    public long UserId { get; set; }
    public long CompanyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    public User User { get; set; } = null!;
}
