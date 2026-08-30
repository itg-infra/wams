namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.Companies;

public static class CompanyExportColumns
{
    public static List<ExportColumnDefinition<CompanyResponse>> Columns =>
    [
        new("Code", x => x.Code),
        new("Name", x => x.Name),
        new("Address", x => x.Address),
        new("Phone", x => x.Phone),
        new("Email", x => x.Email),
        new("Active", x => x.IsActive),
        new("Users", x => x.UserCount),
        new("Warehouses", x => x.WarehouseCount),
        new("Created At", x => x.CreatedAt, Format: "yyyy-MM-dd HH:mm"),
    ];
}
