namespace WAMS.Application.Tests.Validators;

using FluentAssertions;
using WAMS.Application.DTOs.Companies;
using WAMS.Application.Validators.Companies;
using Xunit;

public class CreateCompanyRequestValidatorTests
{
    private readonly CreateCompanyRequestValidator _validator = new();

    [Theory]
    [InlineData("", "Acme", null, false, "empty code")]
    [InlineData("acme", "Acme", null, false, "lowercase code")]
    [InlineData("ACME!", "Acme", null, false, "code with special char")]
    [InlineData("ACME", "", null, false, "empty name")]
    [InlineData("ACME", "Acme", "not-email", false, "invalid email")]
    [InlineData("ACME", "Acme", null, true, "valid minimal")]
    [InlineData("ACME", "Acme", "a@b.com", true, "valid with email")]
    [InlineData("CO_1-X", "Co", null, true, "code with underscore and hyphen")]
    public void Validate_AllScenarios_ReturnsExpectedValidity(
        string code, string name, string? email, bool isValid, string because)
    {
        var request = new CreateCompanyRequest(code, name, null, null, email);

        var result = _validator.Validate(request);

        result.IsValid.Should().Be(isValid, because: because);
    }

    [Fact]
    public void Validate_CodeTooLong_IsInvalid()
    {
        var longCode = new string('A', 51);
        var request = new CreateCompanyRequest(longCode, "Name", null, null, null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_NameTooLong_IsInvalid()
    {
        var longName = new string('x', 256);
        var request = new CreateCompanyRequest("ACME", longName, null, null, null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyEmail_IsValid_BecauseEmailIsOptional()
    {
        // Email field is optional - empty/null should not fail validation
        var request = new CreateCompanyRequest("ACME", "Acme Corp", null, null, null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PhoneTooLong_IsInvalid()
    {
        var longPhone = new string('1', 31);
        var request = new CreateCompanyRequest("ACME", "Acme", null, longPhone, null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
