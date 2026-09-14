namespace WAMS.Domain.Entities.Users;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;

public class UserCompany : BaseEntity
{
    public long UserId { get; set; }
    public long CompanyId { get; set; }
    public int AuthorizationVersion { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime? RemovedAt { get; set; }

    public User User { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public ICollection<UserCompanyRole> Roles { get; set; } = [];
    public ICollection<UserCompanyWarehouse> Warehouses { get; set; } = [];
    public ICollection<UserCompanyProvince> Provinces { get; set; } = [];
    public ICollection<UserCompanyPermission> Permissions { get; set; } = [];
}
