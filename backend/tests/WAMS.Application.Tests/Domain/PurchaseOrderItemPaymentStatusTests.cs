namespace WAMS.Application.Tests.Domain;

using FluentAssertions;
using WAMS.Domain.Entities.PurchaseOrders;
using WAMS.Domain.Enums;
using Xunit;

public class PurchaseOrderItemPaymentStatusTests
{
    [Fact]
    public void NewPurchaseOrderItem_DefaultsToUnpaid()
    {
        var item = new PurchaseOrderItem();

        item.PaymentStatus.Should().Be(PurchaseOrderItemPaymentStatus.Unpaid);
    }

    [Fact]
    public void FromValue_RoundTripsBothStates()
    {
        PurchaseOrderItemPaymentStatus.FromValue("Unpaid").Should().Be(PurchaseOrderItemPaymentStatus.Unpaid);
        PurchaseOrderItemPaymentStatus.FromValue("Paid").Should().Be(PurchaseOrderItemPaymentStatus.Paid);
    }
}
