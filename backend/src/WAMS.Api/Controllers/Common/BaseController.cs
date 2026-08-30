namespace WAMS.Api.Controllers.Common;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Export;
using WAMS.Domain.Constants;
using WAMS.Domain.Exceptions;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected string GetRequestId()
    {
        return HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
    }

    protected long GetUserId()
    {
        var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(subClaim))
            throw new UnauthorizedException(ErrorMessages.Permission.UserIdClaimNotFound);

        return long.Parse(subClaim);
    }

    protected long GetCompanyId()
    {
        var claim = User.FindFirst("company_id")?.Value;

        if (string.IsNullOrEmpty(claim))
            throw new UnauthorizedException(ErrorMessages.Permission.CompanyIdClaimNotFound);

        return long.Parse(claim);
    }

    protected string? GetFullname()
        => User.FindFirst("fullname")?.Value;

    protected IReadOnlyList<string> GetUserRoles()
        => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    protected ApiResponse<T> OkResponse<T>(T data, string message = SuccessMessages.General.OperationSuccessful)
    {
        return new ApiResponse<T>(true, data, message, GetRequestId());
    }

    protected ApiResponse<object> OkResponse(string message = SuccessMessages.General.OperationSuccessful)
    {
        return new ApiResponse<object>(true, null, message, GetRequestId());
    }

    protected PaginatedResponse<T> OkPaginatedResponse<T>(List<T> data, PaginationMeta meta, string message = SuccessMessages.General.DataRetrieved)
    {
        return new PaginatedResponse<T>(true, data, meta, GetRequestId());
    }

    protected ErrorResponse ErrorResponse(string message, string code = ErrorCodes.ValidationError)
    {
        return new ErrorResponse(false, message, new ErrorDetail(code), GetRequestId());
    }

    protected async Task StreamExportResponseAsync<T>(
        IAsyncEnumerable<T> data,
        ExportFormat format,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        string fileName,
        string sheetName,
        IExportService exportService,
        CancellationToken ct,
        string? pdfTitle = null)
    {
        var ext = exportService.GetFileExtension(format);

        Response.ContentType = exportService.GetContentType(format);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}.{ext}\"";

        await exportService.StreamExportAsync(Response.Body, format, columns, data, sheetName, pdfTitle, ct);
    }

    protected async Task ExportResponseAsync<T>(
        IReadOnlyList<T> data,
        ExportFormat format,
        IReadOnlyList<ExportColumnDefinition<T>> columns,
        string fileName,
        string sheetName,
        IExportService exportService,
        CancellationToken ct,
        string? pdfTitle = null)
    {
        var ext = exportService.GetFileExtension(format);

        Response.ContentType = exportService.GetContentType(format);
        Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}.{ext}\"";

        await exportService.ExportAsync(Response.Body, format, columns, data, sheetName, pdfTitle, ct);
    }

}
