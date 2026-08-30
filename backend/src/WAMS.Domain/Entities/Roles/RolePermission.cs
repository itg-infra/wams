namespace WAMS.Domain.Entities.Roles;

public class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public long? GrantedBy { get; set; }

    /// <summary>
    /// Optional JSON constraints (e.g. {"max_approval_amount": 500000000}).
    /// Populated for future enforcement - not yet evaluated by HasPermissionAsync.
    /// </summary>
    public string? Constraints { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
