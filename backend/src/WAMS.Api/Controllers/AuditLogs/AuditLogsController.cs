namespace WAMS.Api.Controllers.AuditLogs;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Domain.Exceptions;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize]
public class AuditLogsController(
    IAuditLogService auditLogService,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions) : BaseController
{
    /// <summary>Gets a paginated list of audit logs.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Audit.LogRead)]
    [ProducesResponseType(typeof(PaginatedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AuditLogQuery query, CancellationToken ct)
    {
        var result = await auditLogService.GetAllAsync(query, ct);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta,
            SuccessMessages.AuditLog.ListRetrieved
        ));
    }

    /// <summary>Gets an audit log by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Audit.LogRead)]
    [ProducesResponseType(typeof(ApiResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await auditLogService.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(string.Format(ErrorCodes.AuditLogNotFound, id));

        return Ok(OkResponse(
            result,
            SuccessMessages.AuditLog.Retrieved
        ));
    }

    /// <summary>Exports audit logs to a file in the requested format.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Audit.LogExport)]
    public async Task<IActionResult> Export(
        [FromQuery] AuditLogQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = auditLogService.StreamAllAsync(query, exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            AuditLogExportColumns.Columns,
            $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Audit Logs",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets the paginated change history for a specific record.</summary>
    [HttpGet("record/{tableName}/{recordId:long}")]
    [RequirePermission(Permissions.Audit.LogRead)]
    [ProducesResponseType(typeof(PaginatedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecordHistory(
        string tableName, long recordId, [FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var result = await auditLogService.GetRecordHistoryAsync(tableName, recordId, query, ct);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta,
            SuccessMessages.AuditLog.RecordHistory
        ));
    }
}
