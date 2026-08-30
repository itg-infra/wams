namespace WAMS.Api.Tests.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WAMS.Api.Controllers.Auth;
using WAMS.Application.DTOs.Auth;
using WAMS.Application.Interfaces.Auth;
using Xunit;

public class AuthControllerTests
{
    private readonly IAuthService _authSvc = Substitute.For<IAuthService>();
    private readonly IValidator<LoginRequest> _loginValidator = Substitute.For<IValidator<LoginRequest>>();
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator = Substitute.For<IValidator<ChangePasswordRequest>>();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_authSvc, _loginValidator, _changePasswordValidator);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static ValidationResult ValidResult() => new();
    private static ValidationResult InvalidResult(string msg = "Email is required") =>
        new([new ValidationFailure("Email", msg)]);

    // Login
    [Fact]
    public async Task Login_WithValidationFailure_ThrowsValidationException()
    {
        _loginValidator.ValidateAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(InvalidResult());

        var act = async () => await _sut.Login(new LoginRequest("bad", "pass", 1));

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithLoginResponse()
    {
        _loginValidator.ValidateAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>()).Returns(ValidResult());
        var loginResponse = new LoginResponse("access-token", "refresh-token", 86400);
        _authSvc.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(loginResponse);

        var result = await _sut.Login(new LoginRequest("a@b.com", "pass", 1));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    // Refresh
    [Fact]
    public async Task Refresh_WithValidToken_Returns200()
    {
        _authSvc.RefreshAsync(Arg.Any<RefreshRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LoginResponse("new-access", "new-refresh", 86400));

        var result = await _sut.Refresh(new RefreshRequest("old-token"));

        result.Should().BeOfType<OkObjectResult>();
    }

    // Me
    [Fact]
    public async Task Me_WithAuthenticatedUser_Returns200WithMeResponse()
    {
        var me = new MeResponse(1, "a@b.com", "Alice", true, false, 1, "Test Company", "TC001", [], [], [], [], [], DateTime.UtcNow);
        _authSvc.GetCurrentUserAsync(1, Arg.Any<CancellationToken>()).Returns(me);

        _sut.ControllerContext.HttpContext.User = BuildUser(userId: 1);

        var result = await _sut.Me();

        result.Should().BeOfType<OkObjectResult>();
    }

    // Logout
    [Fact]
    public async Task Logout_WithValidUser_Returns200()
    {
        _sut.ControllerContext.HttpContext.User = BuildUser(userId: 1, jti: "jti-abc");

        var result = await _sut.Logout(new LogoutRequest("refresh-token"));

        result.Should().BeOfType<OkObjectResult>();
        await _authSvc.Received(1).LogoutAsync(1, "jti-abc", "refresh-token", Arg.Any<CancellationToken>());
    }

    // ChangePassword
    [Fact]
    public async Task ChangePassword_WithValidationFailure_ThrowsValidationException()
    {
        _changePasswordValidator.ValidateAsync(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("NewPassword", "New password must be at least 8 characters")]));

        var act = async () => await _sut.ChangePassword(new ChangePasswordRequest("old", "short"));

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task ChangePassword_WithValidRequest_Returns200AndDelegatesToServiceWithCallerId()
    {
        _changePasswordValidator.ValidateAsync(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValidResult());
        _sut.ControllerContext.HttpContext.User = BuildUser(userId: 7);

        var result = await _sut.ChangePassword(new ChangePasswordRequest("old", "newpassword1"));

        result.Should().BeOfType<OkObjectResult>();
        await _authSvc.Received(1).ChangePasswordAsync(7, Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>());
    }

    private static ClaimsPrincipal BuildUser(long userId, string jti = "jti-abc")
    {
        var identity = new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
        ], authenticationType: "jwt");
        return new ClaimsPrincipal(identity);
    }
}
