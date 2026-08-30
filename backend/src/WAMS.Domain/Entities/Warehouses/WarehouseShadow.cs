namespace WAMS.Domain.Entities.Warehouses;

using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;

public class WarehouseShadow : IShadowEntity
{
    public long Id { get; set; }

    // ERP master data fields
    public string Code { get; set; } = string.Empty; // maps from whscode
    public string Name { get; set; } = string.Empty; // maps from whsname
    public string? Location { get; set; } // maps from location
    public long? ProvinceId { get; set; }
    public Province? Province { get; set; }


    // Tenancy
    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // Shadow table tracking
    public DateTime FirstSeenAt { get; set; }
    public DateTime SyncedAt { get; set; }
    public bool IsActive { get; set; } = true; // set to false when missing from ERP response

    public ICollection<UserWarehouse> UserWarehouses { get; set; } = [];
}
