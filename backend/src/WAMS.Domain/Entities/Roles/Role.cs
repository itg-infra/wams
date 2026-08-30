namespace WAMS.Domain.Entities.Roles;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Companies;

public class Role : BaseEntity
{
    public long? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool GlobalAccess { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
