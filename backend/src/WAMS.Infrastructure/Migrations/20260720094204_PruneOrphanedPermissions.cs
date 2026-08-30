using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <summary>
    /// Removes 17 permission rows that no code path checks: synonyms for permissions that already
    /// exist (realization/quality keys, covered by workorder.workorder.update and the recap flow),
    /// rows describing capabilities the system doesn't have (work order create, warehouse update,
    /// tax type write, SPK close), and document.* keys -attachment access is governed by
    /// WorkOrderFileAttachmentEntityHandler instead, inherited from the parent work order.
    ///
    /// role_permissions and user_permissions cascade on permission_id, so grants and per-user
    /// overrides pointing at these rows are removed automatically.
    ///
    /// Module/Resource/Action/Description are quoted PascalCase columns; created_at and
    /// updated_at are snake_case. Both spellings below are deliberate.
    /// </summary>
    public partial class PruneOrphanedPermissions : Migration
    {
        // Matched on the natural key - row ids differ per environment.
        private const string OrphanedKeys = @"
            ('workorder', 'realization', 'create'),
            ('workorder', 'realization', 'read'),
            ('workorder', 'realization', 'recap'),
            ('workorder', 'realization', 'verify'),
            ('quality',   'fumigation',  'create'),
            ('quality',   'fumigation',  'read'),
            ('quality',   'moisture',    'create'),
            ('quality',   'moisture',    'read'),
            ('workorder', 'workorder',   'create'),
            ('workorder', 'workorder',   'close'),
            ('user',      'warehouse',   'update'),
            ('report',    'report',      'read'),
            ('budget',    'tax_type',    'create'),
            ('budget',    'tax_type',    'update'),
            ('budget',    'tax_type',    'delete'),
            ('document',  'document',    'create'),
            ('document',  'document',    'read')";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DELETE FROM permissions
                WHERE (""Module"", ""Resource"", ""Action"") IN ({OrphanedKeys});");

            // Corrected labels for the permissions that cover what the deleted keys described.
            // Repeated here (also synced by the seeder) so a migrate-without-reseed environment
            // still gets the right client-facing text.
            migrationBuilder.Sql(@"
                UPDATE permissions SET ""Description"" = 'View work orders and their realization data'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'workorder' AND ""Action"" = 'read';
                UPDATE permissions SET ""Description"" = 'Record work order realization - field data entry, fumigation, QC/moisture'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'workorder' AND ""Action"" = 'update';
                UPDATE permissions SET ""Description"" = 'Submit work orders for recap'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'workorder' AND ""Action"" = 'submit';
                UPDATE permissions SET ""Description"" = 'View daily work order recap'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'recap' AND ""Action"" = 'read';
                UPDATE permissions SET ""Description"" = 'Verify and approve daily work order recap'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'recap' AND ""Action"" = 'approve';
                UPDATE permissions SET ""Description"" = 'Reject daily work order recap for correction'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'recap' AND ""Action"" = 'reject';
                UPDATE permissions SET ""Description"" = 'Assign a warehouse to a user'
                    WHERE ""Module"" = 'user' AND ""Resource"" = 'warehouse' AND ""Action"" = 'create';
                UPDATE permissions SET ""Description"" = 'Remove a warehouse from a user'
                    WHERE ""Module"" = 'user' AND ""Resource"" = 'warehouse' AND ""Action"" = 'delete';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the permission rows only, not the role grants that pointed at them.
            migrationBuilder.Sql(@"
                INSERT INTO permissions (""Module"", ""Resource"", ""Action"", ""Description"", created_at, updated_at)
                VALUES
                    ('workorder', 'realization', 'create', 'Record daily realization (Foreman)',              NOW(), NOW()),
                    ('workorder', 'realization', 'read',   'View realizations',                               NOW(), NOW()),
                    ('workorder', 'realization', 'recap',  'Recap daily work order (Warehouse Admin)',        NOW(), NOW()),
                    ('workorder', 'realization', 'verify', 'Verify/check realization (Warehouse Admin/Head)', NOW(), NOW()),
                    ('quality',   'fumigation',  'create', 'Schedule fumigation',                             NOW(), NOW()),
                    ('quality',   'fumigation',  'read',   'View fumigation records',                         NOW(), NOW()),
                    ('quality',   'moisture',    'create', 'Record moisture content',                         NOW(), NOW()),
                    ('quality',   'moisture',    'read',   'View moisture readings',                          NOW(), NOW()),
                    ('workorder', 'workorder',   'create', 'Create work orders',                              NOW(), NOW()),
                    ('workorder', 'workorder',   'close',  'Close SPK',                                       NOW(), NOW()),
                    ('user',      'warehouse',   'update', 'Update warehouses',                               NOW(), NOW()),
                    ('report',    'report',      'read',   'View all reports',                                NOW(), NOW()),
                    ('budget',    'tax_type',    'create', 'Create tax types',                                NOW(), NOW()),
                    ('budget',    'tax_type',    'update', 'Update tax types',                                NOW(), NOW()),
                    ('budget',    'tax_type',    'delete', 'Delete tax types',                                NOW(), NOW()),
                    ('document',  'document',    'create', 'Upload photos/documents',                         NOW(), NOW()),
                    ('document',  'document',    'read',   'View documents',                                  NOW(), NOW())
                ON CONFLICT DO NOTHING;");

            migrationBuilder.Sql(@"
                UPDATE permissions SET ""Description"" = 'View work orders'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'workorder' AND ""Action"" = 'read';
                UPDATE permissions SET ""Description"" = 'Update work orders'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'workorder' AND ""Action"" = 'update';
                UPDATE permissions SET ""Description"" = 'Submit work orders'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'workorder' AND ""Action"" = 'submit';
                UPDATE permissions SET ""Description"" = 'View recap work order list and detail'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'recap' AND ""Action"" = 'read';
                UPDATE permissions SET ""Description"" = 'Approve recap work order'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'recap' AND ""Action"" = 'approve';
                UPDATE permissions SET ""Description"" = 'Reject recap work order'
                    WHERE ""Module"" = 'workorder' AND ""Resource"" = 'recap' AND ""Action"" = 'reject';
                UPDATE permissions SET ""Description"" = 'Create warehouses'
                    WHERE ""Module"" = 'user' AND ""Resource"" = 'warehouse' AND ""Action"" = 'create';
                UPDATE permissions SET ""Description"" = 'Delete warehouses'
                    WHERE ""Module"" = 'user' AND ""Resource"" = 'warehouse' AND ""Action"" = 'delete';");
        }
    }
}
