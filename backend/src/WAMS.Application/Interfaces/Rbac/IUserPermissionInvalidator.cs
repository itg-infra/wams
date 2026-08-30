namespace WAMS.Application.Interfaces.Rbac;

public interface IUserPermissionInvalidator
{
    Task InvalidateAsync(long userId, CancellationToken ct = default);
}
