namespace WAMS.Domain.Entities.Companies;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;

public class Company : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LogoStorageKey { get; set; }

    public ICollection<User> Users { get; set; } = [];
    public ICollection<WarehouseShadow> Warehouses { get; set; } = [];
}