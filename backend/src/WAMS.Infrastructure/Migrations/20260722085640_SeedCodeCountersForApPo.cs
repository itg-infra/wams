using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCodeCountersForApPo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AccountPayable/PurchaseOrder started using code_counters in
            // 7ca544d without a seed, so the first NextValueAsync call after
            // deploy collides with existing AP-/PO- codes (unique index).
            // Seed from existing rows so it picks up where COUNT(*) left off.
            migrationBuilder.Sql("""
                INSERT INTO code_counters (prefix, value, updated_at)
                SELECT prefix, MAX(seq)::bigint, NOW()
                FROM (
                    SELECT substring(code FROM 1 FOR 7) AS prefix,
                           substring(code FROM 8)::bigint AS seq
                    FROM account_payables
                    WHERE code ~ '^AP-[0-9]{4}[0-9]{6}$'
                    UNION ALL
                    SELECT substring(code FROM 1 FOR 7) AS prefix,
                           substring(code FROM 8)::bigint AS seq
                    FROM purchase_orders
                    WHERE code ~ '^PO-[0-9]{4}[0-9]{6}$'
                ) seeded
                GROUP BY prefix
                ON CONFLICT (prefix) DO UPDATE
                    SET value = GREATEST(code_counters.value, EXCLUDED.value),
                        updated_at = NOW();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM code_counters WHERE prefix ~ '^(AP|PO)-[0-9]{4}$';
                """);
        }
    }
}
