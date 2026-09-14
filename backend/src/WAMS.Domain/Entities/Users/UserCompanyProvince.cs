namespace WAMS.Domain.Entities.Users;

using WAMS.Domain.Entities.Common;

public class UserCompanyProvince
{
    public long UserCompanyId { get; set; }
    public long ProvinceId { get; set; }

    public UserCompany UserCompany { get; set; } = null!;
    public Province Province { get; set; } = null!;
}
