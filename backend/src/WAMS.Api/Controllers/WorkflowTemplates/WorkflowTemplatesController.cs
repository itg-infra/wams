namespace WAMS.Api.Controllers.WorkflowTemplates;

using WAMS.Api.Controllers.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.WorkflowTemplates;
using WAMS.Application.Interfaces.WorkflowTemplates;

[ApiController]
[Route("api/v1/workflow-templates")]
[Authorize]
public class WorkflowTemplatesController(
    IWorkflowTemplateService service,
    IValidator<CreateWorkflowTemplateRequest> createValidator,
    IValidator<UpdateWorkflowTemplateRequest> updateValidator
) : BaseController
{
    /// <summary>Gets the list of supported workflow document types.</summary>
    [HttpGet("doc-types")]
    [RequirePermission(Permissions.Workflow.TemplateRead)]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowDocTypeInfo>>), StatusCodes.Status200OK)]
    public IActionResult GetDocTypes()
    {
        var result = service.GetDocTypes();

        return Ok(OkResponse(
            result,
            SuccessMessages.WorkflowTemplate.DocumentTypesRetrieved
        ));
    }

    /// <summary>Gets a paginated list of workflow templates.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Workflow.TemplateRead)]
    [ProducesResponseType(typeof(PaginatedResponse<WorkflowTemplateSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] WorkflowTemplateQuery query, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var (items, total) = await service.GetAllAsync(query, companyId, ct);
        var meta = new PaginationMeta(
            query.Page, 
            query.Limit, 
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.WorkflowTemplate.ListRetrieved
        ));
    }

    /// <summary>Gets a workflow template by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Workflow.TemplateRead)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        var result = await service.GetByIdAsync(id, companyId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.WorkflowTemplate.Retrieved
        ));
    }

    /// <summary>Creates a new workflow template.</summary>
    [HttpPost]
    [RequirePermission(Permissions.Workflow.TemplateCreate)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowTemplateRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new WAMS.Domain.Exceptions.ValidationException(errors);
        }

        var companyId = GetCompanyId();
        var result = await service.CreateAsync(companyId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.WorkflowTemplate.Created
        ));
    }

    /// <summary>Updates an existing workflow template.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.Workflow.TemplateUpdate)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateWorkflowTemplateRequest request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new WAMS.Domain.Exceptions.ValidationException(errors);
        }

        var companyId = GetCompanyId();
        var result = await service.UpdateAsync(id, companyId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.WorkflowTemplate.Updated
        ));
    }

    /// <summary>Activates a workflow template.</summary>
    [HttpPost("{id:long}/activate")]
    [RequirePermission(Permissions.Workflow.TemplateUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(long id, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        await service.ActivateAsync(id, companyId, ct);

        return NoContent();
    }

    /// <summary>Deactivates a workflow template.</summary>
    [HttpPost("{id:long}/deactivate")]
    [RequirePermission(Permissions.Workflow.TemplateUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        await service.DeactivateAsync(id, companyId, ct);

        return NoContent();
    }

    /// <summary>Deletes a workflow template by id.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Workflow.TemplateDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        await service.DeleteAsync(id, companyId, ct);

        return NoContent();
    }
}
