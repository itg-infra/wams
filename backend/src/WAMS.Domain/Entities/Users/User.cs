namespace WAMS.Domain.Entities.Users;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Auth;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;

public class User : BaseEntity
{
    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Fullname { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
    public long? CreatedBy { get; set; }

    // Navigation properties (like preload in gorm)
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<UserWarehouse> UserWarehouses { get; set; } = [];
    public ICollection<UserProvince> UserProvinces { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];

}