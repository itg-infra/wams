namespace WAMS.Application.Interfaces.Auth;

using WAMS.Application.DTOs.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(long userId, string jti, string refreshToken, CancellationToken ct = default);
    Task<MeResponse> GetCurrentUserAsync(long userId, CancellationToken ct = default);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct = default);
}
