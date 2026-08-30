namespace WAMS.Application.Tests.Validators.BudgetPlans;

using FluentAssertions;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.Validators.BudgetPlans;
using WAMS.Domain.Constants;
using WAMS.Domain.Enums;
using Xunit;

public class CreateBudgetPlanRequestValidatorActivityTypeTests
{
    private readonly CreateBudgetPlanRequestValidator _sut = new();

    private static CreateBudgetPlanItemRequest Item(long activityTypeId) => new(
        ItemShadowId: 1,
        ActivityTypeId: activityTypeId,
        VendorShadowId: 1,
        Quantity: 1m,
        CostValue: null,
        Type: BudgetPlanType.External,
        IsRfba: false,
        BillOfLading: null,
        Description: null,
        SpkShadowId: null);

    private static CreateBudgetPlanRequest Request(CreateBudgetPlanItemRequest item) => new(
        BudgetTemplateId: 1,
        WarehouseShadowId: 1,
        Remark: null,
        DocDate: new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Utc),
        Items: [item],
        SpkShadowIds: null);

    [Fact]
    public void Validate_ItemWithZeroActivityTypeId_Fails()
    {
        var result = _sut.Validate(Request(Item(0)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == ErrorMessages.Validation.BudgetPlan.ActivityTypeRequired);
    }

    [Fact]
    public void Validate_ItemWithValidActivityTypeId_PassesActivityTypeRule()
    {
        var result = _sut.Validate(Request(Item(5)));

        result.Errors.Should().NotContain(e =>
            e.ErrorMessage == ErrorMessages.Validation.BudgetPlan.ActivityTypeRequired);
    }
}
