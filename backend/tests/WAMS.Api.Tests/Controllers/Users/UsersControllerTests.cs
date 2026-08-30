namespace WAMS.Api.Tests.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using WAMS.Api.Controllers.Users;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using Xunit;

public class UsersControllerTests
{
    private readonly IUserService _userSvc = Substitute.For<IUserService>();
    private readonly IRbacService _rbacSvc = Substitute.For<IRbacService>();
    private readonly IValidator<CreateUserRequest> _validator = Substitute.For<IValidator<CreateUserRequest>>();
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator = Substitute.For<IValidator<ResetPasswordRequest>>();
    private readonly IExportService _exportService = Substitute.For<IExportService>();
    private readonly IOptions<ExportOptions> _exportOptions = Substitute.For<IOptions<ExportOptions>>();
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _sut = new UsersController(_userSvc, _rbacSvc, _validator, _resetPasswordValidator, _exportService, _exportOptions);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = BuildUser(1) }
        };
        _resetPasswordValidator.ValidateAsync(Arg.Any<ResetPasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
    }

    private static ClaimsPrincipal BuildUser(long userId) =>
        new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        ], authenticationType: "jwt"));

    private static UserResponse FakeUser(long id = 1) =>
        new(id, "a@b.com", "Alice", null, true, DateTime.UtcNow, [], [], []);

    // GetAll
    [Fact]
    public async Task GetAll_Returns200WithPaginatedList()
    {
        var query = new DataTableQuery { Page = 1, Limit = 20 };
        var meta = new PaginationMeta(1, 20, 1, 1);
        var paginated = new PaginatedResponse<UserResponse>(true, [FakeUser()], meta);
        _userSvc.GetAllAsync(query, Arg.Any<CancellationToken>()).Returns(paginated);

        var result = await _sut.GetAll(query);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // GetById
    [Fact]
    public async Task GetById_WithValidId_Returns200()
    {
        _userSvc.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(FakeUser());

        var result = await _sut.GetById(1);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // Create
    [Fact]
    public async Task Create_WithValidationFailure_ThrowsValidationException()
    {
        _validator.ValidateAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Email", "Required")]));

        var act = async () => await _sut.Create(new CreateUserRequest("bad", "pass", "Alice", null));

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Create_WithValidRequest_Returns201()
    {
        _validator.ValidateAsync(Arg.Any<CreateUserRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        _userSvc.CreateAsync(Arg.Any<CreateUserRequest>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(FakeUser(id: 5));

        var result = await _sut.Create(new CreateUserRequest("a@b.com", "Pass1234!", "Alice", null));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
    }

    // Delete
    [Fact]
    public async Task Delete_WithValidId_Returns200AndCallsService()
    {
        var result = await _sut.Delete(1);

        result.Should().BeOfType<OkObjectResult>();
        await _userSvc.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
    }

    // Update
    [Fact]
    public async Task Update_WithValidRequest_Returns200()
    {
        _userSvc.UpdateAsync(1, Arg.Any<UpdateUserRequest>(), Arg.Any<CancellationToken>()).Returns(FakeUser());

        var result = await _sut.Update(1, new UpdateUserRequest("New Name", null, null));

        result.Should().BeOfType<OkObjectResult>();
    }

    // ResetPassword
    [Fact]
    public async Task ResetPassword_WithValidationFailure_ThrowsValidationException()
    {
        _resetPasswordValidator.ValidateAsync(Arg.Any<ResetPasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("NewPassword", "New password must be at least 8 characters")]));

        var act = async () => await _sut.ResetPassword(5, new ResetPasswordRequest("short"));

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _userSvc.DidNotReceive().ResetPasswordAsync(Arg.Any<long>(), Arg.Any<ResetPasswordRequest>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPassword_WithValidRequest_Returns200AndPassesCallerAsActor()
    {
        _sut.ControllerContext.HttpContext.User = BuildUser(9);

        var result = await _sut.ResetPassword(5, new ResetPasswordRequest("newpassword1"));

        result.Should().BeOfType<OkObjectResult>();
        await _userSvc.Received(1).ResetPasswordAsync(5, Arg.Any<ResetPasswordRequest>(), 9, Arg.Any<CancellationToken>());
    }

    // AssignRole
    [Fact]
    public async Task AssignRole_CallsServiceAndReturns200()
    {
        var result = await _sut.AssignRole(id: 1, roleId: 5);

        result.Should().BeOfType<OkObjectResult>();
        await _userSvc.Received(1).AssignRoleAsync(1, Arg.Is<AssignRoleRequest>(r => r.RoleId == 5), Arg.Any<CancellationToken>());
    }

    // RemoveRole
    [Fact]
    public async Task RemoveRole_CallsServiceAndReturns200()
    {
        var result = await _sut.RemoveRole(id: 1, roleId: 5);

        result.Should().BeOfType<OkObjectResult>();
        await _userSvc.Received(1).RemoveRoleAsync(1, 5, Arg.Any<CancellationToken>());
    }
}
