namespace WAMS.Application.Tests.Validators.AccountPayables;

using FluentAssertions;
using WAMS.Application.DTOs.AccountPayables;
using WAMS.Application.Validators.AccountPayables;
using WAMS.Domain.Constants;
using Xunit;

public class CreateAccountPayableRequestValidatorTests
{
    [Fact]
    public async Task Validate_EmptyItems_RejectsRequest()
    {
        var result = await new CreateAccountPayableRequestValidator()
            .ValidateAsync(
                new CreateAccountPayableRequest(1L, null, DateTime.UtcNow, []),
                TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(CreateAccountPayableRequest.Items) &&
            error.ErrorMessage == ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);
    }
}
