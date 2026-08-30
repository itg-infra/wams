namespace WAMS.Domain.Entities.Users;

using WAMS.Domain.Entities.Warehouses;

public class UserWarehouse
{
    public long UserId { get; set; }
    public long WarehouseId { get; set; }
    public bool IsPrimary { get; set; }

    public User User { get; set; } = null!;
    public WarehouseShadow Warehouse { get; set; } = null!;
}
