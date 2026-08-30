namespace WAMS.Application.Tests.Helpers;

using WAMS.Domain.Entities.Auth;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;

/// <summary>
/// Lightweight entity builders used across all Application test classes.
/// Each builder returns a minimal valid entity; callers can mutate properties inline.
/// </summary>
internal static class TestBuilders
{
    public static User ActiveUser(long id = 1, long companyId = 1, string email = "alice@example.com") => new()
    {
        Id = id,
        CompanyId = companyId,
        Email = email,
        PasswordHash = "hashed",
        Fullname = "Alice",
        IsActive = true,
        Company = Company(id: companyId),
    };

    public static User InactiveUser(long id = 2, long companyId = 1) => new()
    {
        Id = id,
        CompanyId = companyId,
        Email = "inactive@example.com",
        PasswordHash = "hashed",
        Fullname = "Inactive",
        IsActive = false,
    };

    public static Role UserRole(long id = 10, bool isSystem = false) => new()
    {
        Id = id,
        Name = "USER",
        DisplayName = "User",
        IsSystem = isSystem,
        GlobalAccess = false,
        RolePermissions = [],
    };

    public static Role GlobalAccessRole(long id = 20) => new()
    {
        Id = id,
        Name = "HO_SPV",
        DisplayName = "HO SPV",
        IsSystem = true,
        GlobalAccess = true,
        RolePermissions = [],
    };

    public static User GlobalAccessUser(long id = 50, long companyId = 1) => new()
    {
        Id = id,
        CompanyId = companyId,
        Email = "ho-spv@example.com",
        PasswordHash = "hashed",
        Fullname = "HO SPV",
        IsActive = true,
        Company = Company(id: companyId),
        UserRoles = [new UserRole { RoleId = 20, UserId = id, Role = GlobalAccessRole() }],
    };

    public static RefreshToken GlobalAccessRefreshToken(long userId = 50, long companyId = 1) => new()
    {
        Id = 300,
        UserId = userId,
        CompanyId = companyId,
        TokenHash = "gahash",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        User = GlobalAccessUser(userId, companyId),
    };

    public static Role SystemRole(long id = 1) => new()
    {
        Id = id,
        Name = "SUPERADMIN",
        DisplayName = "Super Admin",
        IsSystem = true,
        GlobalAccess = true,
        RolePermissions =
        [
            new RolePermission
            {
                RoleId = id,
                PermissionId = 999,
                Permission = new Permission { Id = 999, Module = "*", Resource = "*", Action = "*", Description = "*.*.*" }
            }
        ],
    };

    public static User SuperAdminUser(long id = 99, long companyId = 1) => new()
    {
        Id = id,
        CompanyId = companyId,
        Email = "sa@example.com",
        PasswordHash = "hashed",
        Fullname = "Super Admin",
        IsActive = true,
        Company = Company(id: companyId),
        UserRoles = [new UserRole { RoleId = 1, UserId = id, Role = SystemRole() }],
    };

    public static RefreshToken SuperAdminRefreshToken(long userId = 99, long companyId = 1) => new()
    {
        Id = 200,
        UserId = userId,
        CompanyId = companyId,
        TokenHash = "sahash",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        User = SuperAdminUser(userId, companyId),
    };

    public static RefreshToken ActiveRefreshToken(long userId = 1, long companyId = 1) => new()
    {
        Id = 100,
        UserId = userId,
        CompanyId = companyId,
        TokenHash = "validhash",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        User = ActiveUser(userId, companyId),
    };

    public static RefreshToken ExpiredRefreshToken(long userId = 1, long companyId = 1) => new()
    {
        Id = 101,
        UserId = userId,
        CompanyId = companyId,
        TokenHash = "expiredhash",
        ExpiresAt = DateTime.UtcNow.AddDays(-1),
        User = ActiveUser(userId, companyId),
    };

    public static RefreshToken RevokedRefreshToken(long userId = 1, long companyId = 1) => new()
    {
        Id = 102,
        UserId = userId,
        CompanyId = companyId,
        TokenHash = "revokedhash",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        RevokedAt = DateTime.UtcNow.AddHours(-1),
        User = ActiveUser(userId, companyId),
    };

    public static Permission PermissionEntity(long id = 1, string module = "user", string resource = "user", string action = "read") => new()
    {
        Id = id,
        Module = module,
        Resource = resource,
        Action = action,
        Description = $"{module}.{resource}.{action}",
    };

    /// <summary>Active (non-expired) user-level permission override.</summary>
    public static UserPermission ActiveOverride(long userId = 1, long permissionId = 1, bool isGranted = true,
        string module = "user", string resource = "user", string action = "read") => new()
        {
            UserId = userId,
            PermissionId = permissionId,
            IsGranted = isGranted,
            GrantedBy = 99,
            GrantedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = null,
            Permission = new Permission { Id = permissionId, Module = module, Resource = resource, Action = action },
        };

    /// <summary>Expired user-level permission override (should be ignored by HasPermissionAsync).</summary>
    public static UserPermission ExpiredOverride(long userId = 1, long permissionId = 2, bool isGranted = false,
        string module = "user", string resource = "user", string action = "delete") => new()
        {
            UserId = userId,
            PermissionId = permissionId,
            IsGranted = isGranted,
            GrantedBy = 99,
            GrantedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // already expired
            Permission = new Permission { Id = permissionId, Module = module, Resource = resource, Action = action },
        };

    public static WAMS.Application.DTOs.Companies.CompanyResponse CompanyResponse(
        long id = 1, string code = "ACME", bool isActive = true,
        int userCount = 0, int warehouseCount = 0) => new(
            id, code, "Acme Corp", null, null, null, isActive, DateTime.UtcNow, userCount, warehouseCount, false);

    public static Company Company(long id = 1, string code = "ACME", bool isActive = true) => new()
    {
        Id = id,
        Code = code,
        Name = "Acme Corp",
        IsActive = isActive,
        Users = [],
        Warehouses = [],
    };

    public static WarehouseShadow WarehouseShadow(long id = 1, long companyId = 1, string code = "WH-01", string? location = null, long? provinceId = null) => new()
    {
        Id = id,
        CompanyId = companyId,
        Code = code,
        Name = "Main Warehouse",
        Location = location,
        ProvinceId = provinceId,
        IsActive = true,
    };
}
