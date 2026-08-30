namespace WAMS.Application.Tests.Validators.PurchaseOrders;

using FluentAssertions;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Validators.PurchaseOrders;
using WAMS.Domain.Constants;
using Xunit;

public class CreatePurchaseOrderRequestValidatorTests
{
    [Fact]
    public async Task Validate_EmptyItems_RejectsRequest()
    {
        var result = await new CreatePurchaseOrderRequestValidator()
            .ValidateAsync(
                new CreatePurchaseOrderRequest(1L, null, DateTime.UtcNow, []),
                TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(CreatePurchaseOrderRequest.Items) &&
            error.ErrorMessage == ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);
    }
}
