namespace WAMS.Api.Controllers.Companies;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Companies;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Common;
using WAMS.Domain.Constants;

[ApiController]
[Route("api/v1/companies")]
public class CompaniesController(ICompanyService companyService, IExportService exportService, IOptions<ExportOptions> exportOptions) : BaseController
{
    private readonly ICompanyService _companyService = companyService;
    private readonly IExportService _exportService = exportService;
    private readonly IOptions<ExportOptions> _exportOptions = exportOptions;

    /// <summary>Exports companies to a file in the requested format.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.System.CompanyExport)]
    public async Task<IActionResult> Export(
        [FromQuery] DataTableQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = _companyService.StreamAllAsync(query, _exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            CompanyExportColumns.Columns,
            $"companies-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Companies",
            _exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>
    /// Public - no auth required. For the login page company dropdown.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicList([FromQuery] string? code, CancellationToken ct)
    {
        var companies = await _companyService.GetActivePublicAsync(code, ct);

        return Ok(OkResponse(
            companies,
            SuccessMessages.Company.ListRetrieved
        ));
    }

    /// <summary>
    /// Admin - returns all companies with full details.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.System.CompanyRead)]
    [ProducesResponseType(typeof(PaginatedResponse<CompanyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var result = await _companyService.GetAllAsync(query, ct);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta,
            SuccessMessages.Company.ListRetrieved
        ));
    }

    /// <summary>Gets a company by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.System.CompanyRead)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var company = await _companyService.GetByIdAsync(id, ct);

        return Ok(OkResponse(
            company, 
            SuccessMessages.Company.Retrieved
        ));
    }

    /// <summary>Creates a new company.</summary>
    [HttpPost]
    [RequirePermission(Permissions.System.CompanyCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        var company = await _companyService.CreateAsync(request, ct);

        return StatusCode(
            201, 
            OkResponse(
                company, 
                SuccessMessages.Company.Created
            )
        );
    }

    /// <summary>Updates a company by id.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.System.CompanyUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var company = await _companyService.UpdateAsync(id, request, ct);

        return Ok(OkResponse(
            company, 
            SuccessMessages.Company.Updated
        ));
    }

    /// <summary>
    /// Soft-deactivate a company. Does not delete data.
    /// </summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.System.CompanyDelete)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        await _companyService.DeactivateAsync(id, ct);
        
        return Ok(OkResponse(SuccessMessages.Company.Deactivated));
    }

    /// <summary>
    /// Move a user to a different company. Clears their warehouse assignments.
    /// </summary>
    [HttpPost("{companyId:long}/users/{userId:long}")]
    [RequirePermission(Permissions.System.CompanyAssign)]
    public async Task<IActionResult> AssignUser(long companyId, long userId, CancellationToken ct)
    {
        await _companyService.AssignUserToCompanyAsync(userId, companyId, ct);

        return Ok(OkResponse(SuccessMessages.Company.UserAssigned));
    }

    /// <summary>Gets a company's logo image by id.</summary>
    [HttpGet("{id:long}/logo")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogo(long id, CancellationToken ct)
    {
        var (content, contentType) = await _companyService.GetLogoAsync(id, ct);

        return File(content, contentType);
    }

    /// <summary>Uploads or replaces a company's logo image.</summary>
    [HttpPut("{id:long}/logo")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadLogo(long id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ErrorResponse(ErrorMessages.Company.LogoFileRequired));

        if (file.Length > LogoConstants.MaxSizeBytes)
            return BadRequest(ErrorResponse(ErrorMessages.Company.LogoExceedsMaxSize));

        if (!LogoConstants.AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(ErrorResponse(ErrorMessages.Company.LogoContentTypeNotAllowed(file.ContentType)));

        await using var stream = file.OpenReadStream();
        await _companyService.UploadLogoAsync(
            id, 
            stream, 
            file.ContentType, 
            ct
        );

        return Ok(OkResponse(SuccessMessages.Company.LogoUploaded));
    }

    /// <summary>Removes a company's logo image.</summary>
    [HttpDelete("{id:long}/logo")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLogo(long id, CancellationToken ct)
    {
        await _companyService.RemoveLogoAsync(id, ct);
        return NoContent();
    }
}
