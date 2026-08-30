namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.Users;

public static class UserExportColumns
{
    public static List<ExportColumnDefinition<UserResponse>> Columns =>
    [
        new("Email", x => x.Email),
        new("Full Name", x => x.Fullname),
        new("Employee ID", x => x.EmployeeId),
        new("Active", x => x.IsActive),
        new("Roles", x => string.Join(", ", x.Roles.Select(r => r.RoleName))),
        new("Warehouses", x => string.Join(", ", x.Warehouses.Select(w => w.Code))),
        new("Created At", x => x.CreatedAt, Format: "yyyy-MM-dd HH:mm"),
    ];
}
