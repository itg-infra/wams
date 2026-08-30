namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.AuditLogs;

public static class AuditLogExportColumns
{
    public static List<ExportColumnDefinition<AuditLogResponse>> Columns =>
    [
        new("Action", x => x.Action),
        new("Table", x => x.TableName),
        new("Record ID", x => x.RecordId),
        new("Record Key", x => x.RecordKey),
        new("User Email", x => x.UserEmail),
        new("User Name", x => x.UserFullname),
        new("HTTP Method", x => x.HttpMethod),
        new("Request Path", x => x.RequestPath),
        new("IP Address", x => x.IpAddress),
        new("Created At", x => x.CreatedAt, Format: "yyyy-MM-dd HH:mm:ss"),
    ];
}
