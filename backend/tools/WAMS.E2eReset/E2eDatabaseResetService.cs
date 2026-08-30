using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using WAMS.Domain.Entities.Common;
using WAMS.Infrastructure.Data;

namespace WAMS.E2eReset;

/// <summary>
/// E2E database reset service that drops and recreates the schema from scratch
/// while preserving ERP-synced shadow data.
/// </summary>
public class E2eDatabaseResetService(
    AppDbContext db,
    IConfiguration config,
    ILogger<E2eDatabaseResetService> logger,
    DatabaseSeeder seeder)
{
    private readonly AppDbContext _db = db;
    private readonly IConfiguration _config = config;
    private readonly ILogger<E2eDatabaseResetService> _logger = logger;
    private readonly DatabaseSeeder _seeder = seeder;

    /// <summary>
    /// Snapshots shadow data, drops all tables, runs migrations, seeds static data, restores shadow data.
    /// </summary>
    public async Task ResetAsync()
    {
        _logger.LogWarning("E2E: Starting DropAndMigrate reset...");

        var shadowTables = GetShadowTableNames();

        _logger.LogInformation("E2E: Discovered {Count} shadow tables: {Tables}",
            shadowTables.Count, string.Join(", ", shadowTables));

        // Step 1: Snapshot shadow data
        var snapshots = await SnapshotShadowDataAsync(shadowTables);

        // Guard: abort if shadow tables are empty. Without this check, dropping the schema
        // and restoring nothing would permanently lose shadow data.
        var totalSnapshotRows = snapshots.Values.Sum(rows => rows.Count);
        if (totalSnapshotRows == 0)
        {
            _logger.LogCritical(
                "E2E: Aborting DropAndMigrate - all {Count} shadow tables are empty ({Tables}). " +
                "Run ERP sync first to populate shadow data, then retry.",
                shadowTables.Count, string.Join(", ", shadowTables));
            throw new InvalidOperationException(
                "Shadow tables are empty - aborting to prevent data loss. Run ERP sync first.");
        }

        _logger.LogWarning("E2E: Snapshot complete - {Total} rows across {Count} shadow tables. Proceeding with drop.",
            totalSnapshotRows, shadowTables.Count);

        // Step 2: Drop all tables
        await DropAllTablesAsync();

        // Step 3: Pre-create the migrations history table. EF Core's MigrateAsync queries
        // this table before creating it. When the table is missing, Npgsql catches the exception
        // but EF Core still logs it as an error. Creating the table first silences that noise.
        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using (var createHistoryCmd = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            )
            """, connection))
        {
            await createHistoryCmd.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("E2E: Pre-created __EFMigrationsHistory table.");

        // Step 4: Run migrations from scratch
        _logger.LogWarning("E2E: Running migrations from scratch...");
        await _db.Database.MigrateAsync();

        // Step 5: Seed static data. Must run before restoring shadow data because shadow tables
        // have foreign keys into the companies table created by the seeder.
        _logger.LogWarning("E2E: Seeding static data...");
        await _seeder.SeedAsync();

        // Step 6: Restore shadow data
        await RestoreShadowDataAsync(snapshots);

        // Step 7: Verify row counts match
        await VerifyShadowDataAsync(snapshots);

        _logger.LogWarning("E2E: DropAndMigrate reset complete.");
    }

    private List<string> GetShadowTableNames()
    {
        return _db.Model.GetEntityTypes()
            .Where(et => typeof(IShadowEntity).IsAssignableFrom(et.ClrType))
            .Select(et => et.GetTableName())
            .Where(t => !string.IsNullOrEmpty(t))
            .Cast<string>()
            .ToList();
    }

    private async Task<Dictionary<string, List<Dictionary<string, object?>>>> SnapshotShadowDataAsync(List<string> shadowTables)
    {
        var snapshots = new Dictionary<string, List<Dictionary<string, object?>>>();
        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        foreach (var table in shadowTables)
        {
            _logger.LogInformation("E2E: Snapshotting {Table}...", table);
            var rows = new List<Dictionary<string, object?>>();

            await using var cmd = new NpgsqlCommand(
                $"SELECT * FROM \"{table}\"", connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            snapshots[table] = rows;
            _logger.LogInformation("E2E: Snapshotted {Count} rows from {Table}", rows.Count, table);
        }

        return snapshots;
    }

    private async Task DropAllTablesAsync()
    {
        _logger.LogWarning("E2E: Dropping all tables...");

        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        // Get all table names
        var allTables = new List<string>();
        await using (var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'",
            connection))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                allTables.Add(reader.GetString(0));
            }
        }

        // Drop each table with CASCADE
        foreach (var table in allTables)
        {
            _logger.LogDebug("E2E: Dropping table {Table}...", table);
            await using var dropCmd = new NpgsqlCommand(
                $"DROP TABLE IF EXISTS \"{table}\" CASCADE", connection);
            await dropCmd.ExecuteNonQueryAsync();
        }

        _logger.LogWarning("E2E: All tables dropped.");
    }

    private async Task RestoreShadowDataAsync(Dictionary<string, List<Dictionary<string, object?>>> snapshots)
    {
        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        foreach (var (table, rows) in snapshots)
        {
            if (rows.Count == 0)
            {
                _logger.LogInformation("E2E: {Table} was empty, skipping restore.", table);
                continue;
            }

            _logger.LogInformation("E2E: Restoring {Count} rows to {Table}...", rows.Count, table);

            var columns = rows[0].Keys.ToList();
            var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            var paramList = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

            foreach (var row in rows)
            {
                var sql = $"INSERT INTO \"{table}\" ({columnList}) VALUES ({paramList})";
                await using var cmd = new NpgsqlCommand(sql, connection);

                for (int i = 0; i < columns.Count; i++)
                {
                    var value = row[columns[i]];
                    cmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
                }

                await cmd.ExecuteNonQueryAsync();
            }

            // Reset the sequence to match the highest ID in the restored data.
            // PostgreSQL identifiers are case-sensitive when quoted.
            var pkCol = columns.FirstOrDefault(c => c.Equals("id", StringComparison.OrdinalIgnoreCase));
            if (pkCol != null)
            {
                await using var seqCmd = new NpgsqlCommand(
                    $"SELECT CASE WHEN pg_get_serial_sequence('\"{table}\"', '{pkCol}') IS NOT NULL " +
                    $"THEN setval(pg_get_serial_sequence('\"{table}\"', '{pkCol}'), COALESCE((SELECT MAX(\"{pkCol}\") FROM \"{table}\"), 1)) END",
                    connection);
                await seqCmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("E2E: Restored {Table} with {Count} rows.", table, rows.Count);
        }
    }

    private async Task VerifyShadowDataAsync(Dictionary<string, List<Dictionary<string, object?>>> expectedSnapshots)
    {
        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        foreach (var (table, expectedRows) in expectedSnapshots)
        {
            await using var cmd = new NpgsqlCommand(
                $"SELECT COUNT(*) FROM \"{table}\"", connection);
            var actualCount = (long)(await cmd.ExecuteScalarAsync() ?? 0L);

            if (actualCount != expectedRows.Count)
            {
                _logger.LogWarning(
                    "E2E: Shadow data verification failed for {Table}. Expected {Expected} rows, found {Actual}.",
                    table, expectedRows.Count, actualCount);
            }
            else
            {
                _logger.LogInformation(
                    "E2E: Shadow data verification passed for {Table}: {Count} rows.",
                    table, actualCount);
            }
        }
    }
}
