namespace WAMS.Application.Services.Users;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Exceptions;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IRbacRepository _rbacRepo;
    private readonly IRbacService _rbacService;
    private readonly IUserPermissionInvalidator _permissionInvalidator;
    private readonly IWarehouseShadowRepository _warehouseRepo;
    private readonly IProvinceRepository _provinceRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _uow;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly IAuthRepository _authRepo;
    private readonly IAuditLogWriter _auditLogWriter;

    public UserService(
        IUserRepository userRepo,
        IRbacRepository rbacRepo,
        IRbacService rbacService,
        IUserPermissionInvalidator permissionInvalidator,
        IWarehouseShadowRepository warehouseRepo,
        IProvinceRepository provinceRepo,
        IPasswordHasher passwordHasher,
        ITenantContext tenantContext,
        IUnitOfWork uow,
        ICacheInvalidationService cacheInvalidationService,
        IAuthRepository authRepo,
        IAuditLogWriter auditLogWriter)
    {
        _userRepo = userRepo;
        _rbacRepo = rbacRepo;
        _rbacService = rbacService;
        _permissionInvalidator = permissionInvalidator;
        _warehouseRepo = warehouseRepo;
        _provinceRepo = provinceRepo;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
        _uow = uow;
        _cacheInvalidationService = cacheInvalidationService;
        _authRepo = authRepo;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.User.NotFound(id));

        return MapToResponse(user);
    }

    public async Task<PaginatedResponse<UserResponse>> GetAllAsync(
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var (items, total) = await _userRepo.GetAllAsync(query, ct);
        var totalPages = (int)Math.Ceiling((double)total / query.Limit);

        return new PaginatedResponse<UserResponse>(
            true,
            [.. items.Select(MapToResponse)],
            new PaginationMeta(query.Page, query.Limit, total, totalPages)
        );
    }

    public IAsyncEnumerable<UserResponse> StreamAllAsync(
        DataTableQuery query,
        int limit,
        CancellationToken ct = default
    )
        => _userRepo.StreamAllAsync(query, limit, ct);

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        long createdBy,
        CancellationToken ct = default
    )
    {
        var email = request.Email.ToLowerInvariant();

        var existing = await _userRepo.GetByEmailAsync(email, ct);
        if (existing != null)
            throw new ConflictException(ErrorMessages.User.EmailConflict(email));

        var companyId = _tenantContext.CompanyId!.Value;

        var provinces = request.ProvinceIds is { Count: > 0 }
            ? await EnsureProvincesExistAsync(request.ProvinceIds, ct)
            : [];

        if (request.WarehouseIds is { Count: > 0 })
            await EnsureWarehousesExistInCompanyAsync(request.WarehouseIds, companyId, ct);

        var user = new User
        {
            CompanyId = companyId,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Fullname = request.Fullname,
            EmployeeId = request.EmployeeId,
            CreatedBy = createdBy
        };

        var created = await _userRepo.CreateAsync(user, ct);

        // flush INSERT to get DB-generated user.Id
        await _uow.CommitAsync(ct);

        if (request.WarehouseIds is { Count: > 0 })
        {
            foreach (var warehouseId in request.WarehouseIds)
            {
                var isPrimary = warehouseId == request.PrimaryWarehouseId;
                await _userRepo.AssignWarehouseAsync(created.Id, warehouseId, isPrimary, ct);
            }

            // flush warehouse assignments
            await _uow.CommitAsync(ct);
            created = await _userRepo.GetByIdAsync(created.Id, ct) ?? created;
        }

        if (request.ProvinceIds is { Count: > 0 })
        {
            // Wrapped for symmetry with UpdateAsync's replace path (ReplaceUserProvincesAsync's
            // delete is a no-op for a brand-new user, but the transaction still guards against a
            // failed/cancelled insert leaving the user's role+warehouse commits stranded without provinces).
            await _uow.ExecuteInTransactionAsync(async token =>
            {
                await _userRepo.ReplaceUserProvincesAsync(created.Id, request.ProvinceIds, token);
                await _uow.CommitAsync(token);
            }, ct);

            // Provinces were already validated above, so patch the response in-memory instead of
            // paying for another full split-query reload just to pick up UserProvinces.
            created.UserProvinces = [.. provinces.Select(p => new UserProvince
            {
                UserId = created.Id,
                ProvinceId = p.Id,
                Province = new Province { Id = p.Id, Name = p.Name, Display = p.Display }
            })];
        }

        return MapToResponse(created);
    }

    // Provinces are global reference data (no CompanyId) - single batched existence + active check.
    // Returns the resolved Name/Display for each id so callers can build the response without
    // reloading the user afterward.
    private async Task<List<(long Id, string Name, string Display)>> EnsureProvincesExistAsync(
        IReadOnlyCollection<long> provinceIds,
        CancellationToken ct
    )
    {
        if (provinceIds.Count == 0) return [];

        var found = (await _provinceRepo.GetByIdsAsync(provinceIds, ct)).ToDictionary(p => p.Id);
        var resolved = new List<(long Id, string Name, string Display)>(provinceIds.Count);

        foreach (var provinceId in provinceIds)
        {
            if (!found.TryGetValue(provinceId, out var province))
                throw new NotFoundException(ErrorMessages.Province.NotFound(provinceId));
            if (!province.IsActive)
                throw new ValidationException(ErrorMessages.Province.NotActive(province.Name));

            resolved.Add((province.Id, province.Name, province.Display));
        }

        return resolved;
    }

    private async Task EnsureWarehousesExistInCompanyAsync(
        IReadOnlyCollection<long> warehouseIds,
        long companyId,
        CancellationToken ct
    )
    {
        var found = (await _warehouseRepo.GetCompanyIdsByIdsAsync(warehouseIds, ct))
            .ToDictionary(w => w.Id, w => w.CompanyId);
        foreach (var warehouseId in warehouseIds)
        {
            if (!found.TryGetValue(warehouseId, out var warehouseCompanyId) || warehouseCompanyId != companyId)
                throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouseId));
        }
    }

    public async Task<UserResponse> UpdateAsync(
        long id,
        UpdateUserRequest request,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.User.NotFound(id));

        // Validate before mutating anything: an invalid ProvinceIds request must fail
        // before Fullname/EmployeeId/IsActive are touched, let alone committed.
        // null => leave province scope untouched; non-null (incl. empty) => replace with the given set.
        var provinces = request.ProvinceIds != null
            ? await EnsureProvincesExistAsync(request.ProvinceIds, ct)
            : [];

        if (request.Fullname != null) user.Fullname = request.Fullname;
        if (request.EmployeeId != null) user.EmployeeId = request.EmployeeId;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, ct);

        if (request.ProvinceIds != null)
        {
            // Delete-then-insert is non-atomic on its own (ExecuteDeleteAsync commits immediately),
            // so wrap the field update + the replace in one transaction: a failure mid-way must not
            // leave Fullname/EmployeeId/IsActive committed while the province replace is not.
            // Single CommitAsync deliberately: ExecuteInTransactionAsync retries the whole delegate
            // on a transient DB fault (Npgsql EnableRetryOnFailure), and SaveChanges marks tracked
            // entities Unchanged as soon as it succeeds - two commits would let a fault between them
            // silently drop the field update on retry (the first commit's changes look "already
            // applied" to the tracker even though the transaction that held them got rolled back).
            await _uow.ExecuteInTransactionAsync(async token =>
            {
                await _userRepo.ReplaceUserProvincesAsync(id, request.ProvinceIds, token);
                await _uow.CommitAsync(token); // flushes Fullname/EmployeeId/IsActive + the province replace together
            }, ct);

            // Provinces were already validated above, so patch the response in-memory instead of
            // paying for another full split-query reload just to pick up the replaced UserProvinces.
            user.UserProvinces = [.. provinces.Select(p => new UserProvince
            {
                UserId = id,
                ProvinceId = p.Id,
                Province = new Province { Id = p.Id, Name = p.Name, Display = p.Display }
            })];

            // Warehouse visibility is derived from province scope, so a scope change
            // must invalidate this user's cached warehouse-shadow reads.
            await _cacheInvalidationService.InvalidateWarehouseShadowsForUserAsync(id, ct);
        }
        else
        {
            await _uow.CommitAsync(ct);
        }

        return MapToResponse(user);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await _userRepo.ExistsAsync(id, ct))
            throw new NotFoundException(ErrorMessages.User.NotFound(id));

        await _userRepo.SoftDeleteAsync(id, ct);
        await _uow.CommitAsync(ct);
    }

    public async Task ResetPasswordAsync(
        long id,
        ResetPasswordRequest request,
        long actorUserId,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.User.NotFound(id));

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        await _uow.CommitAsync(ct);

        await _authRepo.RevokeAllUserTokensAsync(id, exceptTokenId: null, ct: ct);

        await _auditLogWriter.LogAsync(
            action: "RESET_PASSWORD",
            tableName: "users",
            recordId: id,
            userId: actorUserId,
            ct: ct
        );
    }

    public async Task AssignRoleAsync(
        long userId,
        AssignRoleRequest request,
        CancellationToken ct = default
    )
    {
        if (!await _userRepo.ExistsAsync(userId, ct))
            throw new NotFoundException(ErrorMessages.User.NotFound(userId));

        var role = await _rbacRepo.GetRoleByIdAsync(request.RoleId, ct)
            ?? throw new NotFoundException(ErrorMessages.Role.NotFound(request.RoleId));

        var assigned = await _rbacRepo.AssignRoleToUserAsync(userId, role.Id, ct);
        if (!assigned)
            throw new ConflictException(ErrorMessages.User.AlreadyHasRole(role.Name));

        await _uow.CommitAsync(ct);
        await _permissionInvalidator.InvalidateAsync(userId, ct);
        await _cacheInvalidationService.InvalidateWarehouseShadowsForUserAsync(userId, ct);
    }

    public async Task RemoveRoleAsync(long userId, long roleId, CancellationToken ct = default)
    {
        if (!await _userRepo.ExistsAsync(userId, ct))
            throw new NotFoundException(ErrorMessages.User.NotFound(userId));

        await _rbacRepo.RemoveRoleFromUserAsync(userId, roleId, ct);
        await _uow.CommitAsync(ct);
        await _permissionInvalidator.InvalidateAsync(userId, ct);
        await _cacheInvalidationService.InvalidateWarehouseShadowsForUserAsync(userId, ct);
    }

    public async Task AssignWarehouseAsync(
        long userId,
        AssignWarehouseRequest request,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(ErrorMessages.User.NotFound(userId));

        var warehouse = await _warehouseRepo.GetByIdAsync(request.WarehouseId, ct)
            ?? throw new NotFoundException(ErrorMessages.Warehouse.NotFound(request.WarehouseId));

        // In Super Admin bypass mode the tenant query filter is disabled for both queries above,
        // so nothing else stops a warehouse from a different company being resolved here.
        if (warehouse.CompanyId != user.CompanyId)
            throw new NotFoundException(ErrorMessages.Warehouse.NotFound(request.WarehouseId));

        await _userRepo.AssignWarehouseAsync(userId, warehouse.Id, request.IsPrimary, ct);
        await _uow.CommitAsync(ct);
        await _cacheInvalidationService.InvalidateWarehouseShadowsForUserAsync(userId, ct);
    }

    public async Task RemoveWarehouseAsync(long userId, long warehouseId, CancellationToken ct = default)
    {
        if (!await _userRepo.ExistsAsync(userId, ct))
            throw new NotFoundException(ErrorMessages.User.NotFound(userId));

        await _userRepo.RemoveWarehouseAsync(userId, warehouseId, ct);
        await _uow.CommitAsync(ct);
        await _cacheInvalidationService.InvalidateWarehouseShadowsForUserAsync(userId, ct);
    }

    public Task<bool> HasGlobalAccessAsync(long userId, CancellationToken ct = default)
        => _rbacService.HasGlobalAccessAsync(userId, ct);

    public Task<List<long>> GetUserWarehouseIdsAsync(long userId, CancellationToken ct = default)
        => _userRepo.GetUserWarehouseIdsAsync(userId, ct);

    public Task<List<long>> GetUserProvinceIdsAsync(long userId, CancellationToken ct = default)
        => _userRepo.GetUserProvinceIdsAsync(userId, ct);

    private static UserResponse MapToResponse(User user) => new(
        user.Id,
        user.Email,
        user.Fullname,
        user.EmployeeId,
        user.IsActive,
        user.CreatedAt,
        [.. user.UserRoles.Select(ur => new UserRoleInfo(
            ur.RoleId,
            ur.Role.Name,
            ur.Role.DisplayName)
        )],
        [.. user.UserWarehouses.Select(uw => new UserWarehouseInfo(
            uw.WarehouseId,
            uw.Warehouse.Code,
            uw.Warehouse.Name,
            uw.IsPrimary)
        )],
        [.. user.UserProvinces.Select(up => new UserProvinceInfo(
            up.ProvinceId,
            up.Province.Name,
            up.Province.Display)).OrderBy(p => p.Display)
        ]
    );
}
