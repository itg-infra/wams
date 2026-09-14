namespace WAMS.Application.Services.Users;

using WAMS.Application.DTOs.Users;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Exceptions;

public sealed class UserCompanyService(
    IUserCompanyRepository repository,
    IUserRepository userRepository,
    ICompanyRepository companyRepository,
    IRbacService rbac,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IUserPermissionInvalidator permissionInvalidator,
    IAuthRepository authRepository) : IUserCompanyService
{
    private readonly IUserCompanyRepository _repository = repository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ICompanyRepository _companyRepository = companyRepository;
    private readonly IRbacService _rbac = rbac;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserPermissionInvalidator _permissionInvalidator = permissionInvalidator;
    private readonly IAuthRepository _authRepository = authRepository;

    public async Task<UserCompanyResponse> AddMembershipAsync(
        long userId,
        long companyId,
        long actorUserId,
        AddUserCompanyRequest? request = null,
        CancellationToken ct = default)
    {
        var company = await RequireActiveCompanyAsync(companyId, ct);
        var user = await _repository.GetUserAsync(userId, ct)
            ?? throw new NotFoundException(ErrorMessages.User.NotFound(userId));
        if (await _rbac.HasGlobalAccessAsync(userId, ct))
            throw new ConflictException(ErrorMessages.User.GlobalAccessMembershipNotApplicable);

        var live = await _repository.GetLiveAsync(userId, companyId, ct);
        if (live is not null)
            throw new ConflictException(ErrorMessages.User.AlreadyAssignedToCompany);

        var existing = await _repository.GetAnyAsync(userId, companyId, ct);
        if (existing is not null)
        {
            if (!await _rbac.HasGlobalAccessAsync(actorUserId, ct))
                throw new ForbiddenException("Only a system administrator can attach an existing identity to another company");

            await RestoreMembershipInternalAsync(existing, company.Id, request, ct);
            await _unitOfWork.CommitAsync(ct);
            return Map(existing, company.Code, company.Name);
        }

        if (!await _rbac.HasGlobalAccessAsync(actorUserId, ct))
            throw new ForbiddenException("Only a system administrator can attach an existing identity to another company");

        var membership = new UserCompany
        {
            UserId = user.Id,
            CompanyId = company.Id,
            CreatedBy = actorUserId,
            AuthorizationVersion = 0,
            User = user,
            Company = company
        };

        await ApplyAssignmentsAsync(membership, request, companyId, ct);
        await _repository.AddAsync(membership, ct);
        await _unitOfWork.CommitAsync(ct);
        return Map(membership, company.Code, company.Name);
    }

    public async Task RemoveMembershipAsync(long userId, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await RequireActiveCompanyAsync(companyId, ct);
        if (await _rbac.HasGlobalAccessAsync(userId, ct))
            throw new ConflictException(ErrorMessages.User.GlobalAccessMembershipNotApplicable);
        var membership = await _repository.GetLiveAsync(userId, companyId, ct)
            ?? throw new NotFoundException("User company membership", $"{userId}/{companyId}");

        var actorMembership = await _repository.GetLiveAsync(actorUserId, companyId, ct);
        var canRemove = await _rbac.HasGlobalAccessAsync(actorUserId, ct) ||
            (actorMembership is not null &&
             await _rbac.HasPermissionAsync(actorUserId, actorMembership.Id, "user", "user", "update", ct));
        if (!canRemove)
            throw new ForbiddenException("You do not have permission to remove users from this company");

        membership.RemovedAt = DateTime.UtcNow;
        membership.UpdatedAt = DateTime.UtcNow;
        await _repository.IncrementAuthorizationVersionAsync(membership.Id, ct);
        await _repository.RevokeRefreshTokensAsync(membership.Id, ct);
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    public async Task<UserCompanyResponse> RestoreMembershipAsync(long userId, long companyId, long actorUserId, CancellationToken ct = default)
        => await AddMembershipAsync(userId, companyId, actorUserId, null, ct);

    public async Task<UserCompanyResponse?> GetMembershipAsync(long userId, long companyId, CancellationToken ct = default)
    {
        var membership = await _repository.GetLiveAsync(userId, companyId, ct);
        if (membership is null) return null;
        var company = await _repository.GetCompanyAsync(companyId, ct)
            ?? throw new NotFoundException("Company", companyId);
        return Map(membership, company.Code, company.Name);
    }

    public async Task<List<UserCompanyResponse>> GetMembershipsAsync(long userId, bool includeAllCompanies = false, CancellationToken ct = default)
    {
        var memberships = includeAllCompanies
            ? await _repository.GetAllForUserSystemAsync(userId, ct)
            : await _repository.GetForUserAsync(userId, ct: ct);
        return [.. memberships.Select(m => Map(m, m.Company.Code, m.Company.Name))];
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, long actingCompanyId, long createdBy, CancellationToken ct = default)
    {
        var company = await RequireActiveCompanyAsync(actingCompanyId, ct);
        var actorMembership = await _repository.GetLiveAsync(createdBy, actingCompanyId, ct);
        var systemAdministrator = await _rbac.HasGlobalAccessAsync(createdBy, ct);
        var companyAdministrator = actorMembership is not null &&
            await _rbac.HasPermissionAsync(createdBy, actorMembership.Id, "user", "user", "create", ct);
        if (!systemAdministrator && !companyAdministrator)
            throw new ForbiddenException("You do not have permission to create users in this company");
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _repository.GetUserByEmailAsync(email, ct) is not null)
            throw new ConflictException(ErrorMessages.User.EmailConflict(email));

        var user = new User
        {
            CompanyId = actingCompanyId,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Fullname = request.Fullname,
            EmployeeId = request.EmployeeId,
            CreatedBy = createdBy,
            Company = company
        };

        UserCompany? membership = null;
        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _userRepository.CreateAsync(user, token);
            await _unitOfWork.CommitAsync(token);
            membership = new UserCompany
            {
                UserId = user.Id,
                CompanyId = actingCompanyId,
                CreatedBy = createdBy,
                User = user,
                Company = company
            };
            await ApplyAssignmentsAsync(membership, new AddUserCompanyRequest(null, request.WarehouseIds, request.PrimaryWarehouseId, request.ProvinceIds), actingCompanyId, token);
            await _repository.AddAsync(membership, token);
            await _unitOfWork.CommitAsync(token);
        }, ct);

        return MapUser(user, membership!);
    }

    public async Task AssignRoleAsync(long userId, long companyId, long roleId, long actorUserId, CancellationToken ct = default)
    {
        var membership = await RequireMembershipAsync(userId, companyId, actorUserId, ct);
        var roles = await _repository.GetRolesByIdsAsync([roleId], ct);
        var role = roles.SingleOrDefault() ?? throw new NotFoundException("Role", roleId);
        if (role.Name == RoleCodes.SuperAdmin || (role.CompanyId.HasValue && role.CompanyId != companyId))
            throw new ForbiddenException(ErrorMessages.User.MembershipSuperAdminRoleDenied);
        if (membership.Roles.Any(r => r.RoleId == roleId))
            throw new ConflictException(ErrorMessages.User.AlreadyHasRole(role.Name));
        membership.Roles.Add(new UserCompanyRole { UserCompanyId = membership.Id, RoleId = roleId, Role = role });
        membership.AuthorizationVersion++;
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    public async Task RemoveRoleAsync(long userId, long companyId, long roleId, long actorUserId, CancellationToken ct = default)
    {
        var membership = await RequireMembershipAsync(userId, companyId, actorUserId, ct);
        var role = membership.Roles.FirstOrDefault(r => r.RoleId == roleId);
        if (role is null) throw new NotFoundException("User company role", roleId);
        membership.Roles.Remove(role);
        membership.AuthorizationVersion++;
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    public async Task AssignWarehouseAsync(long userId, long companyId, long warehouseId, bool isPrimary, long actorUserId, CancellationToken ct = default)
    {
        var membership = await RequireMembershipAsync(userId, companyId, actorUserId, ct);
        var warehouse = (await _repository.GetWarehousesByIdsAsync([warehouseId], ct)).SingleOrDefault()
            ?? throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouseId));
        if (warehouse.CompanyId != companyId)
            throw new ConflictException(ErrorMessages.User.MembershipWarehouseCompanyMismatch);
        if (membership.Warehouses.Any(w => w.WarehouseId == warehouseId))
            throw new ConflictException("User is already assigned to this warehouse");
        if (isPrimary)
            foreach (var current in membership.Warehouses) current.IsPrimary = false;
        membership.Warehouses.Add(new UserCompanyWarehouse { UserCompanyId = membership.Id, WarehouseId = warehouseId, Warehouse = warehouse, IsPrimary = isPrimary });
        membership.AuthorizationVersion++;
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    public async Task RemoveWarehouseAsync(long userId, long companyId, long warehouseId, long actorUserId, CancellationToken ct = default)
    {
        var membership = await RequireMembershipAsync(userId, companyId, actorUserId, ct);
        var warehouse = membership.Warehouses.FirstOrDefault(w => w.WarehouseId == warehouseId)
            ?? throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouseId));
        membership.Warehouses.Remove(warehouse);
        membership.AuthorizationVersion++;
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    public async Task ReplaceProvincesAsync(long userId, long companyId, IReadOnlyCollection<long> provinceIds, long actorUserId, CancellationToken ct = default)
    {
        var membership = await RequireMembershipAsync(userId, companyId, actorUserId, ct);
        var ids = provinceIds.Distinct().ToArray();
        var provinces = await _repository.GetProvincesByIdsAsync(ids, ct);
        if (provinces.Count != ids.Length || provinces.Any(p => !p.IsActive))
            throw new NotFoundException("One or more provinces", string.Join(",", ids));
        await _repository.ClearProvinceAssignmentsAsync(membership.Id, ct);
        membership.Provinces.Clear();
        foreach (var province in provinces)
            membership.Provinces.Add(new UserCompanyProvince { UserCompanyId = membership.Id, ProvinceId = province.Id, Province = province });
        membership.AuthorizationVersion++;
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    public async Task GrantPermissionAsync(long userId, long companyId, long permissionId, bool isGranted, UserPermissionOverrideRequest request, long actorUserId, CancellationToken ct = default)
    {
        var membership = await RequireMembershipAsync(userId, companyId, actorUserId, ct);
        var permission = await _repository.GetPermissionByIdAsync(permissionId, ct)
            ?? throw new NotFoundException("Permission", permissionId);
        var existing = membership.Permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (existing is null)
        {
            membership.Permissions.Add(new UserCompanyPermission
            {
                UserCompanyId = membership.Id,
                PermissionId = permissionId,
                Permission = permission,
                IsGranted = isGranted,
                GrantedBy = actorUserId,
                ExpiresAt = request.ExpiresAt,
                Reason = request.Reason
            });
        }
        else
        {
            existing.IsGranted = isGranted;
            existing.GrantedBy = actorUserId;
            existing.ExpiresAt = request.ExpiresAt;
            existing.Reason = request.Reason;
        }
        membership.AuthorizationVersion++;
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    public async Task RemovePermissionAsync(long userId, long companyId, long permissionId, long actorUserId, CancellationToken ct = default)
    {
        var membership = await RequireMembershipAsync(userId, companyId, actorUserId, ct);
        var permission = membership.Permissions.FirstOrDefault(p => p.PermissionId == permissionId)
            ?? throw new NotFoundException("User company permission", permissionId);
        membership.Permissions.Remove(permission);
        membership.AuthorizationVersion++;
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    public async Task<List<UserPermissionOverrideResponse>> GetPermissionOverridesAsync(long userId, long companyId, CancellationToken ct = default)
    {
        var membership = await _repository.GetLiveAsync(userId, companyId, ct)
            ?? throw new NotFoundException("User company membership", $"{userId}/{companyId}");
        return [.. membership.Permissions.Select(p => new UserPermissionOverrideResponse(
            p.PermissionId,
            p.Permission.Module,
            p.Permission.Resource,
            p.Permission.Action,
            p.IsGranted,
            p.GrantedBy,
            p.GrantedAt,
            p.ExpiresAt,
            p.Reason))];
    }

    private async Task<UserCompany> RequireMembershipAsync(long userId, long companyId, long actorUserId, CancellationToken ct)
    {
        await RequireActiveCompanyAsync(companyId, ct);
        var membership = await _repository.GetLiveAsync(userId, companyId, ct)
            ?? throw new NotFoundException("User company membership", $"{userId}/{companyId}");
        var actorMembership = await _repository.GetLiveAsync(actorUserId, companyId, ct);
        var allowed = await _rbac.HasGlobalAccessAsync(actorUserId, ct) ||
            (actorMembership is not null && await _rbac.HasPermissionAsync(actorUserId, actorMembership.Id, "user", "user", "update", ct));
        if (!allowed) throw new ForbiddenException("You do not have permission to manage this company membership");
        return membership;
    }

    private async Task RestoreMembershipInternalAsync(UserCompany membership, long companyId, AddUserCompanyRequest? request, CancellationToken ct)
    {
        await _repository.ClearAssignmentsAsync(membership.Id, ct);
        membership.RemovedAt = null;
        membership.UpdatedAt = DateTime.UtcNow;
        membership.AuthorizationVersion++;
        membership.Roles.Clear();
        membership.Warehouses.Clear();
        membership.Provinces.Clear();
        await ApplyAssignmentsAsync(membership, request, companyId, ct);
        await _permissionInvalidator.InvalidateMembershipAsync(membership.Id, ct);
    }

    private async Task ApplyAssignmentsAsync(UserCompany membership, AddUserCompanyRequest? request, long companyId, CancellationToken ct)
    {
        if (request is null) return;

        var roleIds = (request.RoleIds ?? []).Distinct().ToArray();
        if (roleIds.Length > 0)
        {
            var roles = await _repository.GetRolesByIdsAsync(roleIds, ct);
            if (roles.Count != roleIds.Length)
                throw new NotFoundException("One or more roles", string.Join(",", roleIds));
            if (roles.Any(r => r.Name == RoleCodes.SuperAdmin || (r.CompanyId.HasValue && r.CompanyId != companyId)))
                throw new ForbiddenException("SUPER_ADMIN is a system role and cannot be assigned to a company membership");
            foreach (var role in roles)
                membership.Roles.Add(new UserCompanyRole { UserCompanyId = membership.Id, RoleId = role.Id, Role = role });
        }

        var warehouseIds = (request.WarehouseIds ?? []).Distinct().ToArray();
        if (warehouseIds.Length > 0)
        {
            var warehouses = await _repository.GetWarehousesByIdsAsync(warehouseIds, ct);
            if (warehouses.Count != warehouseIds.Length)
                throw new NotFoundException("One or more warehouses", string.Join(",", warehouseIds));
            if (warehouses.Any(w => w.CompanyId != companyId))
                throw new ConflictException("Every warehouse assignment must belong to the membership company");
            if (request.PrimaryWarehouseId.HasValue && !warehouseIds.Contains(request.PrimaryWarehouseId.Value))
                throw new ConflictException("The primary warehouse must be one of the assigned warehouses");
            foreach (var warehouse in warehouses)
                membership.Warehouses.Add(new UserCompanyWarehouse
                {
                    UserCompanyId = membership.Id,
                    WarehouseId = warehouse.Id,
                    Warehouse = warehouse,
                    IsPrimary = request.PrimaryWarehouseId == warehouse.Id
                });
        }

        var provinceIds = (request.ProvinceIds ?? []).Distinct().ToArray();
        if (provinceIds.Length > 0)
        {
            var provinces = await _repository.GetProvincesByIdsAsync(provinceIds, ct);
            if (provinces.Count != provinceIds.Length)
                throw new NotFoundException("One or more provinces", string.Join(",", provinceIds));
            if (provinces.Any(p => !p.IsActive))
                throw new ConflictException("Every province assignment must refer to an active province");
            foreach (var province in provinces)
                membership.Provinces.Add(new UserCompanyProvince { UserCompanyId = membership.Id, ProvinceId = province.Id, Province = province });
        }
    }

    private async Task<WAMS.Domain.Entities.Companies.Company> RequireActiveCompanyAsync(long companyId, CancellationToken ct)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, ct)
            ?? throw new NotFoundException("Company", companyId);
        if (!company.IsActive)
            throw new ForbiddenException(ErrorMessages.Auth.CompanyNotFoundOrInactive);
        return company;
    }

    private static UserCompanyResponse Map(UserCompany membership, string code, string name)
        => new(
            membership.Id,
            membership.UserId,
            membership.CompanyId,
            code,
            name,
            membership.AuthorizationVersion,
            [.. membership.Roles.Where(r => r.ExpiresAt is null || r.ExpiresAt > DateTime.UtcNow).Select(r => new UserRoleInfo(r.RoleId, r.Role.Name, r.Role.DisplayName))],
            [.. membership.Warehouses.Select(w => new UserWarehouseInfo(w.WarehouseId, w.Warehouse.Code, w.Warehouse.Name, w.IsPrimary))],
            [.. membership.Provinces.Select(p => new UserProvinceInfo(p.ProvinceId, p.Province.Name, p.Province.Display))],
            membership.CreatedAt,
            membership.RemovedAt);

    private static UserResponse MapUser(User user, UserCompany membership)
        => new(
            user.Id,
            user.Email,
            user.Fullname,
            user.IsActive,
            user.CreatedAt,
            [.. membership.Roles.Where(r => r.ExpiresAt is null || r.ExpiresAt > DateTime.UtcNow).Select(r => new UserRoleInfo(r.RoleId, r.Role.Name, r.Role.DisplayName))],
            [.. membership.Warehouses.Select(w => new UserWarehouseInfo(w.WarehouseId, w.Warehouse.Code, w.Warehouse.Name, w.IsPrimary))],
            [.. membership.Provinces.Select(p => new UserProvinceInfo(p.ProvinceId, p.Province.Name, p.Province.Display))],
            user.EmployeeId);
}
