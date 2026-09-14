namespace WAMS.Application.Interfaces.Rbac;

public interface IUserPermissionInvalidator
{
    Task InvalidateAsync(long userId, CancellationToken ct = default);
    Task InvalidateMembershipAsync(long userCompanyId, CancellationToken ct = default);
}
