namespace WAMS.Application.Tests.Validators;

using FluentAssertions;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Validators.Auth;
using Xunit;

public class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _sut = new();

    [Fact]
    public void Validate_WithShortNewPassword_Fails()
    {
        var result = _sut.Validate(new ResetPasswordRequest("short"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        var result = _sut.Validate(new ResetPasswordRequest("newpassword1"));

        result.IsValid.Should().BeTrue();
    }
}
