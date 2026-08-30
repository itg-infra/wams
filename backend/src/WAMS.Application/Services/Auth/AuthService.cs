namespace WAMS.Application.Services.Auth;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using WAMS.Application.DTOs.Auth;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Auth;
using WAMS.Domain.Exceptions;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuthRepository _authRepo;
    private readonly IRbacRepository _rbacRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContext _tenantContext;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly IWamsMetrics _metrics;
    private readonly int _expirationMinutes;
    private readonly int _refreshExpirationMinutes;
    private readonly int _idleTimeoutMinutes;

    public AuthService(
        IUserRepository userRepo,
        IAuthRepository authRepo,
        IRbacRepository rbacRepo,
        IPasswordHasher passwordHasher,
        ICompanyRepository companyRepo,
        ITenantContext tenantContext,
        ITokenService tokenService,
        IUnitOfWork uow,
        IConfiguration config,
        IAuditLogWriter auditLogWriter,
        IWamsMetrics metrics)
    {
        _userRepo = userRepo;
        _authRepo = authRepo;
        _rbacRepo = rbacRepo;
        _passwordHasher = passwordHasher;
        _companyRepo = companyRepo;
        _tenantContext = tenantContext;
        _tokenService = tokenService;
        _uow = uow;
        _auditLogWriter = auditLogWriter;
        _metrics = metrics;
        _expirationMinutes = int.Parse(config["Jwt:ExpirationMinutes"] ?? "1440");
        _refreshExpirationMinutes = int.Parse(config["Jwt:RefreshExpirationMinutes"] ?? "10080");
        _idleTimeoutMinutes = int.Parse(config["Jwt:IdleTimeoutMinutes"] ?? "30");
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? deviceInfo,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByEmailWithRolesAsync(request.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedException(ErrorMessages.Auth.InvalidCredentials);

        if (!user.IsActive)
            throw new UnauthorizedException(ErrorMessages.Auth.AccountInactive);

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _metrics.RecordLoginFailure();
            throw new UnauthorizedException(ErrorMessages.Auth.InvalidCredentials);
        }

        var hasWildcard = user.UserRoles.Any(ur => ur.Role.RolePermissions.Any(rp => rp.Permission?.FullKey == "*.*.*"));

        long actingCompanyId;
        if (hasWildcard)
        {
            var company = await _companyRepo.GetByIdAsync(request.CompanyId, ct);
            if (company is null || !company.IsActive)
                throw new UnauthorizedException(ErrorMessages.Auth.CompanyNotFoundOrInactive);
            actingCompanyId = request.CompanyId;
        }
        else
        {
            if (request.CompanyId != user.CompanyId)
                throw new UnauthorizedException(ErrorMessages.Auth.InvalidCredentials);
            actingCompanyId = user.CompanyId;
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles, actingCompanyId, hasWildcard);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var tokenHash = HashToken(refreshToken);

        await _authRepo.CreateRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            CompanyId = actingCompanyId,
            TokenHash = tokenHash,
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_refreshExpirationMinutes)
        }, ct);
        await _uow.CommitAsync(ct);

        await _auditLogWriter.LogAsync(
            action: "LOGIN",
            tableName: "users",
            recordId: user.Id,
            userId: user.Id,
            userEmail: user.Email,
            userFullname: user.Fullname,
            companyId: actingCompanyId,
            ipAddress: ipAddress,
            userAgent: deviceInfo,
            ct: ct
        );

        _metrics.RecordLogin(actingCompanyId);

        return new LoginResponse(accessToken, refreshToken, _expirationMinutes * 60);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await _authRepo.GetRefreshTokenByHashAsync(tokenHash, ct)
            ?? throw new UnauthorizedException(ErrorMessages.Auth.InvalidRefreshToken);

        if (!storedToken.IsActive)
            throw new UnauthorizedException(ErrorMessages.Auth.RefreshTokenExpiredOrRevoked);

        // Idle timeout relies on refresh being triggered on-demand by real requests
        if (DateTime.UtcNow - storedToken.CreatedAt > TimeSpan.FromMinutes(_idleTimeoutMinutes))
        {
            await _authRepo.RevokeRefreshTokenAsync(storedToken.Id, ct);
            await _uow.CommitAsync(ct);
            throw new SessionIdleTimeoutException(ErrorMessages.Auth.SessionIdleTimeout);
        }

        if (storedToken.CompanyId <= 0)
            throw new UnauthorizedException(ErrorMessages.Auth.InvalidRefreshToken);

        // Token rotation: revoke old token
        await _authRepo.RevokeRefreshTokenAsync(storedToken.Id, ct);

        var user = storedToken.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var hasWildcard = user.UserRoles.Any(ur => ur.Role.RolePermissions.Any(rp => rp.Permission?.FullKey == "*.*.*"));
        var accessToken = _tokenService.GenerateAccessToken(user, roles, storedToken.CompanyId, hasWildcard);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newTokenHash = HashToken(newRefreshToken);

        await _authRepo.CreateRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            CompanyId = storedToken.CompanyId,
            TokenHash = newTokenHash,
            IpAddress = storedToken.IpAddress,
            DeviceInfo = storedToken.DeviceInfo,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_refreshExpirationMinutes)
        }, ct);
        await _uow.CommitAsync(ct);

        return new LoginResponse(accessToken, newRefreshToken, _expirationMinutes * 60);
    }

    public async Task LogoutAsync(
        long userId,
        string jti,
        string refreshToken,
        CancellationToken ct = default
    )
    {
        await _tokenService.BlacklistTokenAsync(jti, TimeSpan.FromMinutes(_expirationMinutes));

        // Revoke the refresh token in DB
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _authRepo.GetRefreshTokenByHashAsync(tokenHash, ct);

        if (storedToken != null)
            await _authRepo.RevokeRefreshTokenAsync(storedToken.Id, ct);
    }

    public async Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(ErrorMessages.User.NotFound(userId));

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException(ErrorMessages.Auth.InvalidCredentials);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, ct);
        await _uow.CommitAsync(ct);

        long? exceptTokenId = null;
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var currentToken = await _authRepo.GetRefreshTokenByHashAsync(HashToken(request.RefreshToken), ct);
            exceptTokenId = currentToken?.Id;
        }

        await _authRepo.RevokeAllUserTokensAsync(userId, exceptTokenId, ct);

        await _auditLogWriter.LogAsync(
            action: "CHANGE_PASSWORD",
            tableName: "users",
            recordId: user.Id,
            userId: user.Id,
            userEmail: user.Email,
            userFullname: user.Fullname,
            companyId: user.CompanyId,
            ct: ct
        );
    }

    public async Task<MeResponse> GetCurrentUserAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(ErrorMessages.User.NotFound(userId));

        var permissions = await _rbacRepo.GetUserPermissionKeysAsync(userId, ct);
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var hasGlobalAccess = user.UserRoles.Any(ur => ur.Role.GlobalAccess);
        var permissionMap = BuildPermissionMap(permissions);
        var warehouses = user.UserWarehouses
            .OrderByDescending(uw => uw.IsPrimary)
            .Select(uw => new MeWarehouseResponse(
                uw.Warehouse.Id,
                uw.Warehouse.Code,
                uw.Warehouse.Name,
                uw.Warehouse.Location,
                uw.IsPrimary))
            .ToList();
        var provinces = user.UserProvinces
            .Select(up => new MeProvinceResponse(
                up.Province.Id,
                up.Province.Name,
                up.Province.Display))
            .OrderBy(p => p.Display)
            .ToList();

        // Acting company (from the JWT's company_id claim, set by TenantMiddleware), not the
        // user's home company on their own row - the two differ when a Super Admin is acting
        // as a company other than their own.
        var actingCompanyId = _tenantContext.CompanyId ?? user.CompanyId;
        var actingCompany = actingCompanyId == user.CompanyId
            ? user.Company
            : await _companyRepo.GetByIdAsync(actingCompanyId, ct) ?? user.Company;

        return new MeResponse(
            user.Id,
            user.Email,
            user.Fullname,
            user.IsActive,
            hasGlobalAccess,
            actingCompany.Id,
            actingCompany.Name,
            actingCompany.Code,
            roles,
            permissions,
            permissionMap,
            warehouses,
            provinces,
            user.CreatedAt
        );
    }

    private static Dictionary<string, Dictionary<string, List<string>>> BuildPermissionMap(List<string> permissions)
    {
        var map = new Dictionary<string, Dictionary<string, List<string>>>();
        foreach (var key in permissions)
        {
            var parts = key.Split('.');
            if (parts.Length != 3) continue;
            var (module, resource, action) = (parts[0], parts[1], parts[2]);
            if (!map.TryGetValue(module, out var resources))
                map[module] = resources = new Dictionary<string, List<string>>();
            if (!resources.TryGetValue(resource, out var actions))
                resources[resource] = actions = [];
            actions.Add(action);
        }

        return map;
    }

    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
