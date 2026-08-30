namespace WAMS.Domain.Entities.Users;

using WAMS.Domain.Entities.Common;

public class UserProvince
{
    public long UserId { get; set; }
    public long ProvinceId { get; set; }

    public User User { get; set; } = null!;
    public Province Province { get; set; } = null!;
}
