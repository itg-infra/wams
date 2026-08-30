namespace WAMS.Application.Validators.WorkflowTemplates;

using FluentValidation;
using WAMS.Application.DTOs.WorkflowTemplates;
using WAMS.Domain.Constants;

public class UpdateWorkflowTemplateRequestValidator : AbstractValidator<UpdateWorkflowTemplateRequest>
{
    public UpdateWorkflowTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(ErrorMessages.Validation.WorkflowTemplate.NameMustNotBeEmpty)
            .MaximumLength(200).WithMessage(ErrorMessages.Validation.WorkflowTemplate.NameMaxLength)
            .When(x => x.Name is not null);

        RuleFor(x => x.Stages)
            .NotEmpty().WithMessage(ErrorMessages.Validation.WorkflowTemplate.StagesMustNotBeEmptyWhenProvided)
            .When(x => x.Stages is not null);

        RuleForEach(x => x.Stages).ChildRules(stage =>
        {
            stage.RuleFor(s => s.StageOrder)
                .GreaterThan(0).WithMessage(ErrorMessages.Validation.WorkflowTemplate.StageOrderGreaterThanZero);

            stage.RuleFor(s => s.StageName)
                .NotEmpty().WithMessage(ErrorMessages.Validation.WorkflowTemplate.StageNameRequired)
                .MaximumLength(200).WithMessage(ErrorMessages.Validation.WorkflowTemplate.StageNameMaxLength);

            stage.RuleFor(s => s.ApproverRoles)
                .NotEmpty().WithMessage(ErrorMessages.Validation.WorkflowTemplate.ApproverRolesRequired);

            stage.RuleForEach(s => s.ApproverRoles)
                .NotEmpty().WithMessage(ErrorMessages.Validation.WorkflowTemplate.ApproverRoleNameRequired);
        }).When(x => x.Stages is not null);

        RuleFor(x => x.Stages)
            .Must(stages => stages == null || stages
                .GroupBy(s => s.StageOrder)
                .All(g => g.Count() == 1))
            .WithMessage(ErrorMessages.Validation.WorkflowTemplate.StageOrdersMustBeUnique);
    }
}
