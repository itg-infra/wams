namespace WAMS.Domain.Entities.Common;

using WAMS.Domain.Common;

public class ProvinceAlias : BaseEntity
{
    public long ProvinceId { get; set; }
    public string Alias { get; set; } = string.Empty; // UPPER + trimmed

    public Province Province { get; set; } = null!;
}
