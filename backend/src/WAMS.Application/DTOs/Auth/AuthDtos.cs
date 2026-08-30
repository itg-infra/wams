namespace WAMS.Application.DTOs.Auth;

using WAMS.Application.DTOs.Warehouses;

public record LoginRequest(string Email, string Password, long CompanyId);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer"
);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string? RefreshToken = null);

public record MeResponse(
    long Id,
    string Email,
    string Fullname,
    bool IsActive,
    bool HasGlobalAccess,
    long CompanyId,
    string CompanyName,
    string CompanyCode,
    List<string> Roles,
    List<string> Permissions,
    Dictionary<string, Dictionary<string, List<string>>> PermissionMap,
    List<MeWarehouseResponse> Warehouses,
    List<MeProvinceResponse> Scopes,
    DateTime CreatedAt
);
