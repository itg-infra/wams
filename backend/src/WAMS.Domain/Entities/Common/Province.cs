namespace WAMS.Domain.Entities.Common;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Warehouses;

public class Province : BaseEntity
{
    public string Code { get; set; } = string.Empty;   // e.g. "ID-LA", "GLOBAL"
    public string Name { get; set; } = string.Empty;   // UPPER, e.g. "LAMPUNG" - used for matching/aliases
    public string Display { get; set; } = string.Empty; // proper case, e.g. "Lampung" - for frontend UI
    public bool IsActive { get; set; } = true;

    public ICollection<ProvinceAlias> Aliases { get; set; } = [];
    public ICollection<WarehouseShadow> Warehouses { get; set; } = [];
}
