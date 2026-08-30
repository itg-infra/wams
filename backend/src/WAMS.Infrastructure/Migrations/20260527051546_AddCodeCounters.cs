using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sequence counter keyed by code prefix (e.g. BP.2605, WO.2605).
            // Replaces COUNT(*) / MAX(code) prefix scans in BudgetPlan + WorkOrder code generation.
            // Concurrency safety: UPSERT with RETURNING value+1 inside a single statement.
            migrationBuilder.Sql("""
                CREATE TABLE code_counters (
                    prefix text PRIMARY KEY,
                    value bigint NOT NULL,
                    updated_at timestamp with time zone NOT NULL DEFAULT NOW()
                );
                """);

            // Seed from existing codes so the first call after deploy does not collide.
            // Pattern: 7-char prefix (e.g. BP.2605) + 6-digit zero-padded sequence.
            migrationBuilder.Sql("""
                INSERT INTO code_counters (prefix, value, updated_at)
                SELECT prefix, MAX(seq)::bigint, NOW()
                FROM (
                    SELECT substring(code FROM 1 FOR 7) AS prefix,
                           substring(code FROM 8)::bigint AS seq
                    FROM budget_plans
                    WHERE code ~ '^BP\.[0-9]{4}[0-9]{6}$'
                    UNION ALL
                    SELECT substring(code FROM 1 FOR 7) AS prefix,
                           substring(code FROM 8)::bigint AS seq
                    FROM work_orders
                    WHERE code ~ '^WO\.[0-9]{4}[0-9]{6}$'
                ) seeded
                GROUP BY prefix;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS code_counters;");
        }
    }
}
