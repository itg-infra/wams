namespace WAMS.Application.DTOs.WorkflowTemplates;

using WAMS.Application.Common;

public record WorkflowStageRequest(
    int StageOrder,
    string StageName,
    List<string> ApproverRoles);

public record CreateWorkflowTemplateRequest(
    string DocType,
    string Name,
    bool IsActive,
    List<WorkflowStageRequest> Stages);

public record UpdateWorkflowTemplateRequest(
    string? Name,
    bool? IsActive,
    List<WorkflowStageRequest>? Stages);

public record WorkflowTemplateQuery : DataTableQuery
{
    public string? DocType { get; init; }
}

public record WorkflowStageResponse(
    long Id,
    int StageOrder,
    string StageName,
    string[] ApproverRoles);

public record WorkflowTemplateSummaryResponse(
    long Id,
    string DocType,
    string Name,
    bool IsActive,
    int StageCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record WorkflowTemplateResponse(
    long Id,
    string DocType,
    string Name,
    long CompanyId,
    bool IsActive,
    List<WorkflowStageResponse> Stages,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record WorkflowDocTypeInfo(string Value, string Label);
