namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Services.Rbac;
using WAMS.Application.Tests.Helpers;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Exceptions;
using Xunit;

public class RbacServiceTests
{
    private readonly IRbacRepository _rbacRepo = Substitute.For<IRbacRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RbacService _sut;

    public RbacServiceTests()
    {
        _sut = new RbacService(_rbacRepo, _uow);
    }

    private static UserRbacSnapshot Snapshot(
        IEnumerable<string>? keys = null,
        IEnumerable<UserPermissionOverrideKey>? overrides = null,
        bool globalAccess = false)
        => new(
            new HashSet<string>(keys ?? []),
            (overrides ?? []).ToList(),
            globalAccess);

    // HasPermissionAsync - explicit deny/grant overrides
    [Fact]
    public async Task HasPermissionAsync_WithActiveDenyOverride_ReturnsFalseEvenIfRoleGrants()
    {
        var ct = TestContext.Current.CancellationToken;
        var snapshot = Snapshot(
            keys: ["user.user.read"],
            overrides: [new UserPermissionOverrideKey("user", "user", "read", IsGranted: false)]);
        _rbacRepo.GetUserRbacSnapshotAsync(1, ct).Returns(snapshot);

        var result = await _sut.HasPermissionAsync(1, "user", "user", "read", ct);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_WithActiveGrantOverride_ReturnsTrueEvenIfRoleDoesNotGrant()
    {
        var ct = TestContext.Current.CancellationToken;
        var snapshot = Snapshot(
            overrides: [new UserPermissionOverrideKey("user", "user", "delete", IsGranted: true)]);
        _rbacRepo.GetUserRbacSnapshotAsync(1, ct).Returns(snapshot);

        var result = await _sut.HasPermissionAsync(1, "user", "user", "delete", ct);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WithExpiredDenyOverride_FallsThroughToRoleGrant()
    {
        // Repo excludes expired overrides from the snapshot - active overrides list is empty
        var ct = TestContext.Current.CancellationToken;
        var snapshot = Snapshot(keys: ["user.user.delete"]);
        _rbacRepo.GetUserRbacSnapshotAsync(1, ct).Returns(snapshot);

        var result = await _sut.HasPermissionAsync(1, "user", "user", "delete", ct);

        result.Should().BeTrue();
    }

    // HasPermissionAsync - role-level wildcard matching
    [Theory]
    [InlineData("*.*.*", "stock", "item", "read", true)]
    [InlineData("stock.*.*", "stock", "item", "read", true)]
    [InlineData("stock.*.*", "user", "item", "read", false)]
    [InlineData("stock.item.*", "stock", "item", "delete", true)]
    [InlineData("*.item.read", "user", "item", "read", true)]
    [InlineData("stock.item.read", "stock", "item", "read", true)]
    [InlineData("stock.item.read", "stock", "item", "write", false)]
    [InlineData("*.*.read", "stock", "item", "read", true)]
    [InlineData("stock.*.read", "stock", "item", "read", true)]
    [InlineData("stock.*.read", "stock", "item", "delete", false)]
    public async Task HasPermissionAsync_WildcardRoleKeys_ReturnsExpected(
        string roleKey, string module, string resource, string action, bool expected)
    {
        var ct = TestContext.Current.CancellationToken;
        var snapshot = Snapshot(keys: [roleKey]);
        _rbacRepo.GetUserRbacSnapshotAsync(Arg.Any<long>(), ct).Returns(snapshot);

        var result = await _sut.HasPermissionAsync(1, module, resource, action, ct);

        result.Should().Be(expected, because: $"roleKey={roleKey}, action={module}.{resource}.{action}");
    }

    [Fact]
    public async Task HasPermissionAsync_NoOverridesNoRoleKeys_ReturnsFalse()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetUserRbacSnapshotAsync(1, ct).Returns(Snapshot());

        var result = await _sut.HasPermissionAsync(1, "stock", "item", "read", ct);

        result.Should().BeFalse();
    }

    // CreateRoleAsync
    [Fact]
    public async Task CreateRoleAsync_WithDuplicateName_ThrowsConflictException()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetRoleByNameAsync("ADMIN", ct).Returns(TestBuilders.UserRole());

        var act = () => _sut.CreateRoleAsync(new CreateRoleRequest("ADMIN", null, null), ct);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateRoleAsync_WithUniqueName_CreatesAndReturnsResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = new Role { Id = 5, Name = "MANAGER", IsSystem = false, RolePermissions = [] };
        _rbacRepo.GetRoleByNameAsync("MANAGER", ct).ReturnsNull();
        _rbacRepo.CreateRoleAsync(Arg.Any<Role>(), ct).Returns(created);

        var result = await _sut.CreateRoleAsync(new CreateRoleRequest("manager", "Manager", null), ct);

        result.Name.Should().Be("MANAGER");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // UpdateRoleAsync - system role guard
    [Fact]
    public async Task UpdateRoleAsync_OnSystemRole_ThrowsForbiddenException()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetRoleByIdAsync(1, ct).Returns(TestBuilders.SystemRole());

        var act = () => _sut.UpdateRoleAsync(1, new UpdateRoleRequest("NewDisplay", null, null), ct);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateRoleAsync_OnNonSystemRole_UpdatesAndCommits()
    {
        var ct = TestContext.Current.CancellationToken;
        var role = TestBuilders.UserRole(id: 10, isSystem: false);
        _rbacRepo.GetRoleByIdAsync(10, ct).Returns(role);

        var result = await _sut.UpdateRoleAsync(10, new UpdateRoleRequest("New Display", null, null), ct);

        result.DisplayName.Should().Be("New Display");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRoleAsync_WithRoleNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetRoleByIdAsync(99, ct).ReturnsNull();

        var act = () => _sut.UpdateRoleAsync(99, new UpdateRoleRequest(null, null, null), ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // DeleteRoleAsync - system role guard
    [Fact]
    public async Task DeleteRoleAsync_OnSystemRole_ThrowsForbiddenException()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetRoleByIdAsync(1, ct).Returns(TestBuilders.SystemRole());

        var act = () => _sut.DeleteRoleAsync(1, ct);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteRoleAsync_OnNonSystemRole_CallsRepository()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetRoleByIdAsync(10, ct).Returns(TestBuilders.UserRole(id: 10));

        await _sut.DeleteRoleAsync(10, ct);

        await _rbacRepo.Received(1).DeleteRoleAsync(10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRoleAsync_WithRoleNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetRoleByIdAsync(99, ct).ReturnsNull();

        var act = () => _sut.DeleteRoleAsync(99, ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // AssignPermissionAsync
    [Fact]
    public async Task AssignPermissionAsync_WithPermissionNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetRoleByIdAsync(10, ct).Returns(TestBuilders.UserRole(id: 10));
        _rbacRepo.GetAllPermissionsAsync(ct).Returns(new List<Permission>());

        var act = () => _sut.AssignPermissionAsync(10, new AssignPermissionRequest(99), null, ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignPermissionAsync_WithValidRoleAndPermission_AssignsAndCommits()
    {
        var ct = TestContext.Current.CancellationToken;
        var perm = TestBuilders.PermissionEntity(id: 5);
        _rbacRepo.GetRoleByIdAsync(10, ct).Returns(TestBuilders.UserRole(id: 10));
        _rbacRepo.GetAllPermissionsAsync(ct).Returns([perm]);

        await _sut.AssignPermissionAsync(10, new AssignPermissionRequest(5), grantedBy: 1, ct: ct);

        await _rbacRepo.Received(1).AssignPermissionToRoleAsync(10, 5, 1, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // GetRoleByIdAsync
    [Fact]
    public async Task GetRoleByIdAsync_WithExistingId_ReturnsResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        var role = TestBuilders.UserRole(id: 5);
        _rbacRepo.GetRoleByIdAsync(5, ct).Returns(role);

        var result = await _sut.GetRoleByIdAsync(5, ct);

        result.Id.Should().Be(5);
    }

    [Fact]
    public async Task GetRoleByIdAsync_WithMissingId_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacRepo.GetRoleByIdAsync(99, ct).ReturnsNull();

        var act = () => _sut.GetRoleByIdAsync(99, ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // GrantUserPermissionAsync / DenyUserPermissionAsync
    [Fact]
    public async Task GrantUserPermissionAsync_WithValidPermission_UpsertsAndCommits()
    {
        var ct = TestContext.Current.CancellationToken;
        var perm = TestBuilders.PermissionEntity(id: 1);
        _rbacRepo.GetAllPermissionsAsync(ct).Returns([perm]);

        await _sut.GrantUserPermissionAsync(1, 1, new UserPermissionOverrideRequest(null, null), grantedBy: 99, ct: ct);

        await _rbacRepo.Received(1).UpsertUserPermissionAsync(
            Arg.Is<UserPermission>(up => up.IsGranted && up.UserId == 1),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DenyUserPermissionAsync_WithValidPermission_UpsertsWithIsGrantedFalse()
    {
        var ct = TestContext.Current.CancellationToken;
        var perm = TestBuilders.PermissionEntity(id: 1);
        _rbacRepo.GetAllPermissionsAsync(ct).Returns([perm]);

        await _sut.DenyUserPermissionAsync(1, 1, new UserPermissionOverrideRequest(null, null), grantedBy: 99, ct: ct);

        await _rbacRepo.Received(1).UpsertUserPermissionAsync(
            Arg.Is<UserPermission>(up => !up.IsGranted && up.UserId == 1),
            Arg.Any<CancellationToken>());
    }
}
