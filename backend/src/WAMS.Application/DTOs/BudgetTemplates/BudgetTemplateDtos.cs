namespace WAMS.Application.DTOs.BudgetTemplates;

using WAMS.Application.Common;

public record BudgetTemplateQuery : DataTableQuery
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}

public record CreateBudgetTemplateItemRequest(
    long ItemShadowId,
    long ActivityTypeId);

public record CreateBudgetTemplateRequest(
    long? ProvinceId,
    List<CreateBudgetTemplateItemRequest> Items);

public record UpdateBudgetTemplateRequest(
    long? ProvinceId,
    List<CreateBudgetTemplateItemRequest>? Items);

public record BudgetTemplateSummaryResponse(
    long Id,
    string TemplateCode,
    long? ProvinceId,
    string? ProvinceName,
    string? ProvinceDisplay,
    DateTime Date,
    string Status);

public record BudgetTemplateResponse(
    long Id,
    string TemplateCode,
    long? ProvinceId,
    string? ProvinceName,
    string? ProvinceDisplay,
    string Status,
    List<BudgetTemplateItemResponse> Items,
    DateTime CreatedAt,
    string CreatedByName,
    DateTime? SubmittedAt,
    string? SubmittedByName);

public record BudgetTemplateItemResponse(
    long Id,
    long ItemShadowId,
    string CostDetail,
    string CostName,
    string Coa,
    string CoaName,
    int SortOrder,
    long ActivityTypeId,
    string? ActivityTypeCode,
    string? ActivityTypeName);
