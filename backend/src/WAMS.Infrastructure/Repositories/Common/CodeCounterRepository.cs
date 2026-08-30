namespace WAMS.Infrastructure.Repositories.Common;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.Common;
using WAMS.Infrastructure.Data;

public sealed class CodeCounterRepository(AppDbContext db) : ICodeCounterRepository
{
    public async Task<long> NextValueAsync(string prefix, CancellationToken ct = default)
    {
        // INSERT-OR-INCREMENT in one statement. RETURNING gives back the post-update value.
        // Race-safe: Postgres serialises the conflicting row.
        var rows = await db.Database
            .SqlQuery<long>($"""
                INSERT INTO code_counters (prefix, value, updated_at)
                VALUES ({prefix}, 1, NOW())
                ON CONFLICT (prefix) DO UPDATE
                    SET value = code_counters.value + 1,
                        updated_at = NOW()
                RETURNING value AS "Value"
                """)
            .ToListAsync(ct);

        return rows[0];
    }

    public async Task<long> NextRangeAsync(string prefix, int count, CancellationToken ct = default)
    {
        var rows = await db.Database
            .SqlQuery<long>($"""
                INSERT INTO code_counters (prefix, value, updated_at)
                VALUES ({prefix}, {count}, NOW())
                ON CONFLICT (prefix) DO UPDATE
                    SET value = code_counters.value + {count},
                        updated_at = NOW()
                RETURNING value - {count} + 1 AS "Value"
                """)
            .ToListAsync(ct);

        return rows[0];
    }
}
