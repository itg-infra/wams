namespace WAMS.Application.Tests.Validators;

using FluentAssertions;
using WAMS.Application.DTOs.Auth;
using WAMS.Application.Validators.Auth;
using Xunit;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Theory]
    [InlineData("", "Pass1234", 1, false, "empty email")]
    [InlineData("not-an-email", "Pass1234", 1, false, "invalid email format")]
    [InlineData("x@x.com", "", 1, false, "empty password")]
    [InlineData("x@x.com", "Pass1234", 0, false, "companyId is zero")]
    [InlineData("x@x.com", "Pass1234", -1, false, "negative companyId")]
    [InlineData("x@x.com", "Pass1234", 1, true, "valid request")]
    [InlineData("USER@CORP.COM", "secret", 42, true, "uppercase email is valid")]
    public void Validate_AllScenarios_ReturnsExpectedValidity(
        string email, string password, long companyId, bool isValid, string because)
    {
        var request = new LoginRequest(email, password, companyId);

        var result = _validator.Validate(request);

        result.IsValid.Should().Be(isValid, because: because);
    }

    [Fact]
    public void Validate_EmptyEmail_ContainsEmailRequiredError()
    {
        var result = _validator.Validate(new LoginRequest("", "pass", 1));

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_ZeroCompanyId_ContainsCompanyIdError()
    {
        var result = _validator.Validate(new LoginRequest("a@b.com", "pass", 0));

        result.Errors.Should().Contain(e => e.PropertyName == "CompanyId");
    }
}
