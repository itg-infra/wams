namespace WAMS.Application.Tests.Domain;

using System.Reflection;
using FluentAssertions;
using WAMS.Domain.Constants;
using WAMS.Infrastructure.Data;
using Xunit;

/// <summary>
/// Guards against permission constants or database rows that no code path checks. Such keys are
/// worse than dead code: they get granted to roles, so the role admin screen shows a client
/// capabilities that do nothing, while the permission actually controlling that capability is
/// usually named something else entirely.
/// </summary>
public class PermissionCatalogTests
{
    /// <summary>
    /// Declared but not enforced anywhere, on purpose. Every entry needs a reason - if a key sits
    /// here long enough that nobody remembers why, that is the signal to delete it.
    /// Currently empty: every permission in the catalog is checked by real code. Keep it that way.
    /// </summary>
    private static readonly Dictionary<string, string> NotYetEnforced = [];

    /// <summary>Seeded without a matching constant, on purpose. Currently none.</summary>
    private static readonly HashSet<string> SeededWithoutConstant = [];

    [Fact]
    public void EveryPermissionConstant_IsSeeded()
    {
        var seeded = PermissionSeeder.All
            .Select(p => $"{p.Module}.{p.Resource}.{p.Action}")
            .ToHashSet();

        var missing = AllConstants()
            .Where(c => !seeded.Contains(c.Key))
            .Select(c => $"{c.Path} = \"{c.Key}\"")
            .ToList();

        missing.Should().BeEmpty(
            "every permission constant needs a row in PermissionSeeder.Data.cs, otherwise it can " +
            "never be granted and any endpoint gating on it is unreachable");
    }

    [Fact]
    public void EverySeededPermission_HasAConstant()
    {
        // Wildcards count as declared here: they are seeded rows that roles get granted, they
        // just are not gated on by any single endpoint.
        var declared = AllConstants(includeWildcards: true).Select(c => c.Key).ToHashSet();

        var orphans = PermissionSeeder.All
            .Select(p => $"{p.Module}.{p.Resource}.{p.Action}")
            .Where(k => !declared.Contains(k) && !SeededWithoutConstant.Contains(k))
            .ToList();

        orphans.Should().BeEmpty(
            "a seeded permission with no constant cannot be referenced from code, so it will " +
            "silently never be checked; add it to Permissions.cs or delete the seed row");
    }

    [Fact]
    public void EveryPermissionConstant_IsReferencedOutsideTheSeeder()
    {
        var source = SourceFilesOutsideSeeder();

        var unreferenced = AllConstants()
            .Where(c => !NotYetEnforced.ContainsKey(c.Path))
            .Where(c => !source.Any(text => text.Contains($"Permissions.{c.Path}", StringComparison.Ordinal)))
            .Select(c => $"{c.Path} = \"{c.Key}\"")
            .ToList();

        unreferenced.Should().BeEmpty(
            "a permission nobody checks still shows up in the role admin UI as a capability, so " +
            "the client is told it does something it does not; either gate an endpoint on it, " +
            "delete it, or add it to NotYetEnforced with a reason");
    }

    [Fact]
    public void NotYetEnforcedEntries_StillExist()
    {
        var declared = AllConstants().Select(c => c.Path).ToHashSet();

        var stale = NotYetEnforced.Keys.Where(k => !declared.Contains(k)).ToList();

        stale.Should().BeEmpty("NotYetEnforced names constants that no longer exist - clean it up");
    }

    // Modules holds module-name literals for the seeder helpers, never permission keys, so it is
    // always excluded. Wildcards are real seeded rows but resolve at grant time instead of gating
    // an endpoint, so they are excluded wherever the question is "does code check this?".
    private static IEnumerable<(string Path, string Key)> AllConstants(bool includeWildcards = false) =>
        typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .Where(t => t.Name != nameof(Permissions.Modules))
            .Where(t => includeWildcards || t.Name != nameof(Permissions.Wildcards))
            .SelectMany(t => t
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
                .Select(f => ($"{t.Name}.{f.Name}", (string)f.GetRawConstantValue()!)));

    private static List<string> SourceFilesOutsideSeeder()
    {
        var root = RepositoryRoot();

        return Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
            // Seeder files reference every key by definition, so counting them would make this test
            // always pass.
            .Where(f => !Path.GetFileName(f).Contains("Seeder", StringComparison.Ordinal))
            .Where(f => Path.GetFileName(f) != "Permissions.cs")
            .Select(File.ReadAllText)
            .ToList();
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WAMS.sln")))
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate WAMS.sln above the test output directory");
    }
}
