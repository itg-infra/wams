namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.Roles;

public static class RoleExportColumns
{
    public static List<ExportColumnDefinition<RoleResponse>> Columns =>
    [
        new("Name", x => x.Name),
        new("Display Name", x => x.DisplayName),
        new("Description", x => x.Description),
        new("System Role", x => x.IsSystem),
        new("Global Access", x => x.GlobalAccess),
        new("Permission Count", x => x.Permissions.Count),
        new("Created At", x => x.CreatedAt, Format: "yyyy-MM-dd HH:mm"),
    ];
}
