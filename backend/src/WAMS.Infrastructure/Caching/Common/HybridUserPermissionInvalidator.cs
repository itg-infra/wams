namespace WAMS.Infrastructure.Caching.Common;

using Microsoft.Extensions.Caching.Hybrid;
using WAMS.Application.Interfaces.Rbac;

public sealed class HybridUserPermissionInvalidator(HybridCache cache) : IUserPermissionInvalidator
{
    public Task InvalidateAsync(long userId, CancellationToken ct = default)
        => cache.RemoveByTagAsync(CacheTags.RbacUser(userId), ct).AsTask();
}
