namespace WAMS.Domain.Entities.Roles;

using WAMS.Domain.Common;

public class Permission : BaseEntity
{
    public string Module { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Computed convenience property. Not stored in database.
    /// Returns "module.resource.action" format like "user.warehouse.read".
    /// </summary>
    public string FullKey => $"{Module}.{Resource}.{Action}";

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}
