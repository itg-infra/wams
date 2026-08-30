namespace WAMS.Infrastructure.Caching.Common;

using Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Cache config bound from appsettings "Cache" section.
/// All cache entries are process-local and use the configured TTL.
/// </summary>
public sealed class WamsCacheOptions
{
    public CacheEntryConfig RbacPermission { get; set; } = new(60);

    public CacheEntryConfig PermissionsCatalog { get; set; } = new(300);

    public CacheEntryConfig Uom { get; set; } = new(300);

    public CacheEntryConfig ActivityType { get; set; } = new(300);

    public CacheEntryConfig WorkflowTemplate { get; set; } = new(300);

    public CacheEntryConfig WarehouseShadow { get; set; } = new(120);

    public CacheEntryConfig RateCard { get; set; } = new(120);

    public CacheEntryConfig TaxType { get; set; } = new(300);
}

public sealed class CacheEntryConfig(int ttlSeconds)
{
    public int TtlSeconds { get; set; } = ttlSeconds;

    public HybridCacheEntryOptions ToHybridOptions() => new()
    {
        LocalCacheExpiration = TimeSpan.FromSeconds(TtlSeconds),
        Expiration = TimeSpan.FromSeconds(TtlSeconds),
    };
}
