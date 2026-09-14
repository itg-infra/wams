namespace WAMS.Domain.Entities.Users;

using WAMS.Domain.Entities.Warehouses;

public class UserCompanyWarehouse
{
    public long UserCompanyId { get; set; }
    public long WarehouseId { get; set; }
    public bool IsPrimary { get; set; }

    public UserCompany UserCompany { get; set; } = null!;
    public WarehouseShadow Warehouse { get; set; } = null!;
}
