namespace WAMS.Infrastructure.Tests.Caching;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WAMS.Infrastructure.Caching.Common;

/// <summary>
/// Builds a real in-memory HybridCache for decorator tests.
/// Tag-based invalidation works in the local cache so all decorator behaviors are testable in process.
/// </summary>
internal sealed class CacheTestFixture : IDisposable
{
    private readonly ServiceProvider _provider;
    public readonly HybridCache Cache;
    public readonly IOptions<WamsCacheOptions> Options;

    public CacheTestFixture()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        _provider = services.BuildServiceProvider();
        Cache = _provider.GetRequiredService<HybridCache>();
        Options = Microsoft.Extensions.Options.Options.Create(new WamsCacheOptions());
    }

    public void Dispose() => _provider.Dispose();
}
