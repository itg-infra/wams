using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WAMS.E2eReset;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Services.Common;

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true));
var logger = loggerFactory.CreateLogger("E2eReset");

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

// Layer 1: Hard environment gate. Reset is only allowed in explicit E2E/Testing environments.
var envName = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
if (!envName.Equals("E2E", StringComparison.OrdinalIgnoreCase) &&
    !envName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    logger.LogCritical(
        "SAFETY LOCK: E2E reset is only allowed in E2E/Testing environment. Current environment is '{Environment}'. Aborting.",
        envName);
    return;
}

// Layer 2: Explicit destructive switch.
var allowDestructiveReset = configuration.GetValue<bool>("E2E:AllowDestructiveReset");
if (!allowDestructiveReset)
{
    logger.LogCritical("SAFETY LOCK: E2E:AllowDestructiveReset is false. Aborting.");
    return;
}

var dbConnString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
var connParts = dbConnString
    .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Split('=', 2))
    .Where(kv => kv.Length == 2)
    .ToDictionary(kv => kv[0].Trim(), kv => kv[1].Trim(), StringComparer.OrdinalIgnoreCase);

// Layer 3: Connection identity gate (host + DB name pattern).
connParts.TryGetValue("Host", out var actualHost);
connParts.TryGetValue("Server", out var actualServer);
var normalizedHost = (actualHost ?? actualServer ?? string.Empty).ToLowerInvariant();
var allowedHosts = (configuration["E2E:AllowedDbHosts"] ?? "localhost,127.0.0.1")
    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    .Select(h => h.ToLowerInvariant())
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
if (!allowedHosts.Contains(normalizedHost))
{
    logger.LogCritical(
        "SAFETY LOCK: DB host '{Host}' is not in E2E:AllowedDbHosts. Aborting.",
        string.IsNullOrWhiteSpace(normalizedHost) ? "<empty>" : normalizedHost);
    return;
}

connParts.TryGetValue("Database", out var actualDatabase);
connParts.TryGetValue("Initial Catalog", out var initialCatalog);
var normalizedDatabase = (actualDatabase ?? initialCatalog ?? string.Empty).ToLowerInvariant();
var requiredDbNameSuffix = (configuration["E2E:RequiredDbNameSuffix"] ?? "_e2e").ToLowerInvariant();
if (string.IsNullOrWhiteSpace(normalizedDatabase) ||
    !normalizedDatabase.EndsWith(requiredDbNameSuffix, StringComparison.Ordinal))
{
    logger.LogCritical(
        "SAFETY LOCK: DB name '{Database}' does not end with required suffix '{Suffix}'. Aborting.",
        string.IsNullOrWhiteSpace(normalizedDatabase) ? "<empty>" : normalizedDatabase,
        requiredDbNameSuffix);
    return;
}

// Layer 4: Port fingerprint.
var allowedPort = configuration.GetValue<int>("E2E:AllowedDbPort");
var actualPort = dbConnString
    .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
    .Select(s => s.Split('=', 2))
    .Where(kv => kv.Length == 2 && kv[0].Equals("Port", StringComparison.OrdinalIgnoreCase))
    .Select(kv => int.TryParse(kv[1].Trim(), out var p) ? p : 5432)
    .FirstOrDefault(5432);
if (actualPort != allowedPort)
{
    logger.LogCritical(
        "SAFETY LOCK: DB port is {Actual}, expected {Allowed}. Aborting.",
        actualPort, allowedPort);
    return;
}

// Migrate now (idempotent if already up to date) so the user-count / SA-email checks below
// have tables to query, mirroring the API's pre-E2E-branch auto-migrate step.
var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(dbConnString, npgsql => npgsql.EnableRetryOnFailure(3))
    .Options;
await using var db = new AppDbContext(dbOptions);
await db.Database.MigrateAsync();

// Layer 5: User count - prod/staging DBs have many users; test DB has few.
var userCount = await db.Users.IgnoreQueryFilters().CountAsync();
var maxUsers = configuration.GetValue("E2E:MaxUserCount", 10);
if (userCount > maxUsers)
{
    logger.LogCritical(
        "SAFETY LOCK: DB has {Count} users (max allowed {Max}). Looks like a populated DB. Aborting.",
        userCount, maxUsers);
    return;
}

// Layer 6: SA email fingerprint - if the DB has users, the SA email must match InitialAdmin:Email.
var expectedSaEmail = configuration["InitialAdmin:Email"]?.ToLowerInvariant();
var anyUsers = await db.Users.IgnoreQueryFilters().AnyAsync();
if (anyUsers)
{
    var saExists = await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == expectedSaEmail);
    if (!saExists)
    {
        logger.LogCritical(
            "SAFETY LOCK: DB has users but no SA with email '{Email}'. Wrong database. Aborting.",
            expectedSaEmail);
        return;
    }
}

logger.LogWarning(
    "E2E: Safety checks passed (env={Environment}, host={Host}, db={Database}, port={Port}, users={Users}, sa='{Sa}'). Resetting DB.",
    envName, normalizedHost, normalizedDatabase, actualPort, userCount, expectedSaEmail);

var uow = new UnitOfWork(db);
var passwordHasher = new PasswordService();
var seeder = new DatabaseSeeder(db, passwordHasher, configuration, loggerFactory.CreateLogger<DatabaseSeeder>(), uow);
var resetService = new E2eDatabaseResetService(db, configuration, loggerFactory.CreateLogger<E2eDatabaseResetService>(), seeder);

await resetService.ResetAsync();

logger.LogWarning("E2E: Reset complete.");
