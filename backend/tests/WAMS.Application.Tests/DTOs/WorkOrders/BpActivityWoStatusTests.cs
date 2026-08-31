using System.Text.Json;
using FluentAssertions;
using WAMS.Application.DTOs.WorkOrders;
using Xunit;

namespace WAMS.Application.Tests.DTOs.WorkOrders;

public class BpActivityWoStatusTests
{
    [Fact]
    public void SerializesCoaNameForWorkOrderActivityTabs()
    {
        var activity = new BpActivityWoStatus(
            BudgetPlanItemId: 1,
            ItemShadowId: 2,
            ItemCode: "ITEM-001",
            ActivityName: "Cleaning",
            ActivityTypeCode: "OTHERS",
            ActivityTypeDisplay: "Others",
            CoaName: "Warehouse Cleaning",
            WorkOrderId: null,
            WorkOrderCode: null,
            WorkOrderStatus: null);

        var json = JsonSerializer.Serialize(
            activity,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        json.Should().Contain("\"coaName\":\"Warehouse Cleaning\"");
    }
}
