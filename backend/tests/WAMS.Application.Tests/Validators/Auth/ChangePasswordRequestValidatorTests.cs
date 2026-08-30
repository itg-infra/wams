namespace WAMS.Application.Tests.Validators;

using FluentAssertions;
using WAMS.Application.DTOs.Auth;
using WAMS.Application.Validators.Auth;
using Xunit;

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyCurrentPassword_Fails()
    {
        var result = _sut.Validate(new ChangePasswordRequest("", "newpassword1"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
    }

    [Fact]
    public void Validate_WithShortNewPassword_Fails()
    {
        var result = _sut.Validate(new ChangePasswordRequest("oldpass1", "short"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        var result = _sut.Validate(new ChangePasswordRequest("oldpass1", "newpassword1", "refresh-token-value"));

        result.IsValid.Should().BeTrue();
    }
}
