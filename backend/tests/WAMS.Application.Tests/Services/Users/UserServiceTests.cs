namespace WAMS.Application.Tests.Services;

using System.Linq;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.Users;
using WAMS.Application.Tests.Helpers;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Exceptions;
using Xunit;

public class UserServiceTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacRepository _rbacRepo = Substitute.For<IRbacRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly IUserPermissionInvalidator _invalidator = Substitute.For<IUserPermissionInvalidator>();
    private readonly IWarehouseShadowRepository _warehouseRepo = Substitute.For<IWarehouseShadowRepository>();
    private readonly IProvinceRepository _provinceRepo = Substitute.For<IProvinceRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICacheInvalidationService _cacheInvalidationService = Substitute.For<ICacheInvalidationService>();
    private readonly IAuthRepository _authRepo = Substitute.For<IAuthRepository>();
    private readonly IAuditLogWriter _auditLogWriter = Substitute.For<IAuditLogWriter>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepo, _rbacRepo, _rbacService, _invalidator, _warehouseRepo, _provinceRepo, _hasher, _tenant, _uow, _cacheInvalidationService, _authRepo, _auditLogWriter);
    }

    // CreateAsync - duplicate email
    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        _userRepo.GetByEmailAsync("alice@example.com", Arg.Any<CancellationToken>()).Returns(TestBuilders.ActiveUser());
        _tenant.IsSet.Returns(true);
        _tenant.CompanyId.Returns(42L);

        var act = () => _sut.CreateAsync(new CreateUserRequest("Alice@Example.Com", "Pass1234!", "Alice", null), createdBy: 1);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // CreateAsync - tenant context path
    [Fact]
    public async Task CreateAsync_WithTenantContext_UsesContextCompanyId()
    {
        _userRepo.GetByEmailAsync("bob@example.com", Arg.Any<CancellationToken>()).ReturnsNull();
        _tenant.IsSet.Returns(true);
        _tenant.CompanyId.Returns(42L);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        var createdUser = TestBuilders.ActiveUser(id: 10, companyId: 42, email: "bob@example.com");
        _userRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(createdUser);

        var result = await _sut.CreateAsync(new CreateUserRequest("Bob@Example.Com", "Pass1234!", "Bob", null), createdBy: 1, ct: TestContext.Current.CancellationToken);

        result.Email.Should().Be("bob@example.com");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // CreateAsync - warehouse assignment (2-stage commit)
    [Fact]
    public async Task CreateAsync_WithWarehouseIds_AssignsWarehousesAndCommitsTwice()
    {
        _userRepo.GetByEmailAsync("carol@example.com", Arg.Any<CancellationToken>()).ReturnsNull();
        _tenant.IsSet.Returns(true);
        _tenant.CompanyId.Returns(1L);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        var createdUser = TestBuilders.ActiveUser(id: 30, companyId: 1, email: "carol@example.com");
        var withWarehouses = TestBuilders.ActiveUser(id: 30, companyId: 1, email: "carol@example.com");
        _userRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(createdUser);
        _userRepo.GetByIdAsync(30, Arg.Any<CancellationToken>()).Returns(withWarehouses);

        _warehouseRepo.GetCompanyIdsByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([(7L, 1L)]);

        var result = await _sut.CreateAsync(new CreateUserRequest(
            "Carol@Example.Com", 
            "Pass1234!", 
            "Carol", 
            null,
            WarehouseIds: [7],
            PrimaryWarehouseId: 7), 
            createdBy: 1, 
            ct: TestContext.Current.CancellationToken);

        await _userRepo.Received(1).AssignWarehouseAsync(30, 7, true, Arg.Any<CancellationToken>());
        await _uow.Received(2).CommitAsync(Arg.Any<CancellationToken>()); // stage 1 + stage 2
    }

    [Fact]
    public async Task CreateAsync_WithProvinceIds_ReplacesProvincesAndCommitsTwice()
    {
        _tenant.IsSet.Returns(true);
        _tenant.CompanyId.Returns(42L);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(ci => { var u = ci.Arg<User>(); u.Id = 5; return u; });
        _userRepo.GetByIdAsync(5L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 5, Email = "a@b.c", Fullname = "A", CompanyId = 42 });
        _provinceRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<IEnumerable<long>>().Select(id => (id, $"P{id}", $"P{id}", true)).ToList());
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>().Invoke(CancellationToken.None));

        await _sut.CreateAsync(new CreateUserRequest(
            "a@b.c", 
            "pw", 
            "A", 
            EmployeeId: null,
            ProvinceIds: [2, 3]),
            createdBy: 1, 
            ct: TestContext.Current.CancellationToken);

        await _userRepo.Received(1).ReplaceUserProvincesAsync(
            5L, Arg.Is<IReadOnlyCollection<long>>(p => p.SequenceEqual(new long[] { 2, 3 })),
            Arg.Any<CancellationToken>());
        await _uow.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentProvinceId_ThrowsNotFound()
    {
        _tenant.IsSet.Returns(true);
        _tenant.CompanyId.Returns(42L);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _userRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(ci => { var u = ci.Arg<User>(); u.Id = 5; return u; });
        _provinceRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var act = () => _sut.CreateAsync(
            new CreateUserRequest("a@b.c", "pw", "A", EmployeeId: null,
                ProvinceIds: [99]),
            createdBy: 1);

        await act.Should().ThrowAsync<NotFoundException>();
        await _userRepo.DidNotReceive().ReplaceUserProvincesAsync(
            Arg.Any<long>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithWarehouseIds_ValidatesViaSingleBatchedQuery_NotOnePerId()
    {
        // Guards against an N+1 loop creeping back in: N warehouse IDs must resolve
        // via exactly one repository round trip, not N.
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _tenant.IsSet.Returns(true);
        _tenant.CompanyId.Returns(1L);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        var createdUser = TestBuilders.ActiveUser(id: 40, companyId: 1, email: "erin@example.com");
        _userRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(createdUser);
        _userRepo.GetByIdAsync(40, Arg.Any<CancellationToken>()).Returns(createdUser);
        _warehouseRepo.GetCompanyIdsByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([(7L, 1L), (8L, 1L), (9L, 1L)]);

        await _sut.CreateAsync(new CreateUserRequest(
            "Erin@Example.Com", 
            "Pass1234!", 
            "Erin", 
            null,
            WarehouseIds: [7, 8, 9]),
            createdBy: 1, 
            ct: TestContext.Current.CancellationToken);

        await _warehouseRepo.Received(1).GetCompanyIdsByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>());
        await _warehouseRepo.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithWarehouseFromDifferentCompany_ThrowsNotFoundException()
    {
        _userRepo.GetByEmailAsync("dave@example.com", Arg.Any<CancellationToken>()).ReturnsNull();
        _tenant.IsSet.Returns(true);
        _tenant.CompanyId.Returns(1L);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");

        var createdUser = TestBuilders.ActiveUser(id: 31, companyId: 1, email: "dave@example.com");
        _userRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(createdUser);

        // Warehouse belongs to company 99 - not 1
        _warehouseRepo.GetCompanyIdsByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([(8L, 99L)]);

        var act = () => _sut.CreateAsync(
            new CreateUserRequest("Dave@Example.Com", "Pass1234!", "Dave", null,
                WarehouseIds: [8]),
            createdBy: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // UpdateAsync
    [Fact]
    public async Task UpdateAsync_WithUserNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.UpdateAsync(99, new UpdateUserRequest("New Name", null, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithValidUser_UpdatesAndCommits()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.UpdateAsync(1, new UpdateUserRequest("Updated Name", null, false), TestContext.Current.CancellationToken);

        result.Fullname.Should().Be("Updated Name");
        result.IsActive.Should().BeFalse();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_MapsProvinceScopeIntoResponse()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        user.UserProvinces.Add(new UserProvince
        {
            ProvinceId = 3,
            Province = new Province { Id = 3, Name = "LAMPUNG", Display = "Lampung" }
        });
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        result.Scopes.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new UserProvinceInfo(3, "LAMPUNG", "Lampung"));
    }

    [Fact]
    public async Task UpdateAsync_WithProvinceIds_ResponseReflectsReloadedScope()
    {
        var before = TestBuilders.ActiveUser(id: 1); // no provinces yet
        var after = TestBuilders.ActiveUser(id: 1);
        after.UserProvinces.Add(new UserProvince
        {
            ProvinceId = 3,
            Province = new Province { Id = 3, Name = "LAMPUNG", Display = "Lampung" }
        });
        // First load returns the stale (empty) scope; the post-replace reload returns the new set.
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(before, after);
        _provinceRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([(3L, "LAMPUNG", "Lampung", true)]);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>().Invoke(CancellationToken.None));

        var result = await _sut.UpdateAsync(1, new UpdateUserRequest("N", null, null, ProvinceIds: [3]), TestContext.Current.CancellationToken);

        result.Scopes.Should().ContainSingle().Which.ProvinceId.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_WithNullProvinceIds_LeavesProvinceScopeUntouched()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        // ProvinceIds omitted (defaults to null) => scope must not be touched.
        await _sut.UpdateAsync(1, new UpdateUserRequest("Updated Name", null, null), TestContext.Current.CancellationToken);

        await _userRepo.DidNotReceive().ReplaceUserProvincesAsync(
            Arg.Any<long>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithProvinceIds_ValidatesAndReplacesInsideTransaction()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _provinceRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<IEnumerable<long>>().Select(id => (id, $"P{id}", $"P{id}", true)).ToList());
        // Make the mocked transaction actually run the operation it's handed.
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>().Invoke(CancellationToken.None));

        await _sut.UpdateAsync(1, new UpdateUserRequest("N", null, null, ProvinceIds: [2, 3]), TestContext.Current.CancellationToken);

        await _provinceRepo.Received(1).GetByIdsAsync(
            Arg.Is<IEnumerable<long>>(p => p.SequenceEqual(new long[] { 2, 3 })), Arg.Any<CancellationToken>());
        await _uow.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
        await _userRepo.Received(1).ReplaceUserProvincesAsync(
            1L, Arg.Is<IReadOnlyCollection<long>>(p => p.SequenceEqual(new long[] { 2, 3 })),
            Arg.Any<CancellationToken>());
        await _cacheInvalidationService.Received(1).InvalidateWarehouseShadowsForUserAsync(1, Arg.Any<CancellationToken>());
        await _cacheInvalidationService.DidNotReceive().InvalidateWarehouseShadowsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureProvincesExistAsync_IssuesSingleBatchedQuery_NotOnePerId()
    {
        // Guards against an N+1 loop creeping back in: N province IDs must resolve
        // via exactly one repository round trip, not N.
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _provinceRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<IEnumerable<long>>().Select(id => (id, $"P{id}", $"P{id}", true)).ToList());
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>().Invoke(CancellationToken.None));

        await _sut.UpdateAsync(1, new UpdateUserRequest("N", null, null, ProvinceIds: [2, 3, 4, 5, 6]), TestContext.Current.CancellationToken);

        await _provinceRepo.Received(1).GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyProvinceIds_ClearsScope()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>().Invoke(CancellationToken.None));

        // Empty (non-null) list => replace scope with nothing (clear all).
        await _sut.UpdateAsync(1, new UpdateUserRequest("N", null, null, ProvinceIds: []), TestContext.Current.CancellationToken);

        await _userRepo.Received(1).ReplaceUserProvincesAsync(
            1L, Arg.Is<IReadOnlyCollection<long>>(p => p.Count == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentProvinceId_ThrowsNotFoundAndDoesNotReplace()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _provinceRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>()).Returns([]);

        var act = () => _sut.UpdateAsync(1, new UpdateUserRequest("New Name", null, null, ProvinceIds: [99]));

        await act.Should().ThrowAsync<NotFoundException>();
        await _userRepo.DidNotReceive().ReplaceUserProvincesAsync(
            Arg.Any<long>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        // Validation happens before mutation: an invalid province must not leave
        // Fullname/EmployeeId/IsActive partially applied and committed.
        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        user.Fullname.Should().NotBe("New Name");
    }

    [Fact]
    public async Task UpdateAsync_WithInactiveProvinceId_ThrowsValidationAndDoesNotReplace()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _provinceRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([(3L, "LAMPUNG", "Lampung", false)]);

        var act = () => _sut.UpdateAsync(1, new UpdateUserRequest("New Name", null, null, ProvinceIds: [3]));

        await act.Should().ThrowAsync<ValidationException>();
        await _userRepo.DidNotReceive().ReplaceUserProvincesAsync(
            Arg.Any<long>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        user.Fullname.Should().NotBe("New Name");
    }

    // DeleteAsync
    [Fact]
    public async Task DeleteAsync_WithUserNotFound_ThrowsNotFoundException()
    {
        _userRepo.ExistsAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.DeleteAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WithValidUser_CallsSoftDelete()
    {
        _userRepo.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

        await _userRepo.Received(1).SoftDeleteAsync(1, Arg.Any<CancellationToken>());
    }

    // AssignRoleAsync
    [Fact]
    public async Task AssignRoleAsync_WithUserNotFound_ThrowsNotFoundException()
    {
        _userRepo.ExistsAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.AssignRoleAsync(99, new AssignRoleRequest(1));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignRoleAsync_WithRoleNotFound_ThrowsNotFoundException()
    {
        _userRepo.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _rbacRepo.GetRoleByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.AssignRoleAsync(1, new AssignRoleRequest(99));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignRoleAsync_WithValidUserAndRole_AssignsCommitsAndInvalidatesCache()
    {
        _userRepo.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _rbacRepo.GetRoleByIdAsync(5, Arg.Any<CancellationToken>()).Returns(TestBuilders.UserRole(id: 5));
        _rbacRepo.AssignRoleToUserAsync(1, 5, Arg.Any<CancellationToken>()).Returns(true);
        await _sut.AssignRoleAsync(1, new AssignRoleRequest(5), TestContext.Current.CancellationToken);

        await _rbacRepo.Received(1).AssignRoleToUserAsync(1, 5, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _invalidator.Received(1).InvalidateAsync(1, Arg.Any<CancellationToken>());
        // A role change can flip GlobalAccess, so warehouse-shadow visibility must be re-derived too.
        await _cacheInvalidationService.Received(1).InvalidateWarehouseShadowsForUserAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveRoleAsync_WithValidUser_RemovesCommitsAndInvalidatesCache()
    {
        _userRepo.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.RemoveRoleAsync(userId: 1, roleId: 5, ct: TestContext.Current.CancellationToken);

        await _rbacRepo.Received(1).RemoveRoleFromUserAsync(1, 5, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _invalidator.Received(1).InvalidateAsync(1, Arg.Any<CancellationToken>());
        await _cacheInvalidationService.Received(1).InvalidateWarehouseShadowsForUserAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveRoleAsync_WithUserNotFound_ThrowsNotFoundException()
    {
        _userRepo.ExistsAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.RemoveRoleAsync(99, roleId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
        await _invalidator.DidNotReceive().InvalidateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    // AssignWarehouseAsync
    [Fact]
    public async Task AssignWarehouseAsync_WithUserNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.AssignWarehouseAsync(99, new AssignWarehouseRequest(1));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignWarehouseAsync_WithWarehouseNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestBuilders.ActiveUser(id: 1, companyId: 1));
        _warehouseRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.AssignWarehouseAsync(1, new AssignWarehouseRequest(99));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignWarehouseAsync_WithValidInputs_AssignsAndCommits()
    {
        _userRepo.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestBuilders.ActiveUser(id: 1, companyId: 1));
        _warehouseRepo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(TestBuilders.WarehouseShadow(id: 7, companyId: 1));

        await _sut.AssignWarehouseAsync(1, new AssignWarehouseRequest(7, IsPrimary: true), TestContext.Current.CancellationToken);

        await _userRepo.Received(1).AssignWarehouseAsync(1, 7, true, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        // Must invalidate only this user's cache, not the global tag shared by every user/company.
        await _cacheInvalidationService.Received(1).InvalidateWarehouseShadowsForUserAsync(1, Arg.Any<CancellationToken>());
        await _cacheInvalidationService.DidNotReceive().InvalidateWarehouseShadowsAsync(Arg.Any<CancellationToken>());
    }

    // Reproduces the cross-tenant leak: in Super Admin bypass mode the tenant query filter on
    // both Users and WarehouseShadows is disabled, so GetByIdAsync can return a warehouse from a
    // different company than the target user. AssignWarehouseAsync must reject that explicitly
    // instead of silently creating a cross-tenant UserWarehouse row.
    [Fact]
    public async Task AssignWarehouseAsync_WarehouseBelongsToDifferentCompany_ThrowsNotFoundException()
    {
        _userRepo.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestBuilders.ActiveUser(id: 1, companyId: 1));
        _warehouseRepo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(TestBuilders.WarehouseShadow(id: 7, companyId: 2));

        var act = () => _sut.AssignWarehouseAsync(1, new AssignWarehouseRequest(7, IsPrimary: true));

        await act.Should().ThrowAsync<NotFoundException>();
        await _userRepo.DidNotReceive().AssignWarehouseAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    // RemoveWarehouseAsync
    [Fact]
    public async Task RemoveWarehouseAsync_WithValidInputs_RemovesAndInvalidatesUserCacheOnly()
    {
        _userRepo.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.RemoveWarehouseAsync(1, 7, TestContext.Current.CancellationToken);

        await _userRepo.Received(1).RemoveWarehouseAsync(1, 7, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _cacheInvalidationService.Received(1).InvalidateWarehouseShadowsForUserAsync(1, Arg.Any<CancellationToken>());
        await _cacheInvalidationService.DidNotReceive().InvalidateWarehouseShadowsAsync(Arg.Any<CancellationToken>());
    }

    // ResetPasswordAsync
    [Fact]
    public async Task ResetPasswordAsync_WithMissingUser_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.ResetPasswordAsync(99, new ResetPasswordRequest("newpassword1"), actorUserId: 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidTarget_UpdatesHashRevokesAllSessionsAndAudits()
    {
        var target = TestBuilders.ActiveUser(id: 5, email: "target@example.com");
        _userRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(target);
        _hasher.Hash("newpassword1").Returns("new-hashed");

        await _sut.ResetPasswordAsync(5, new ResetPasswordRequest("newpassword1"), actorUserId: 1, ct: TestContext.Current.CancellationToken);

        target.PasswordHash.Should().Be("new-hashed");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _authRepo.Received(1).RevokeAllUserTokensAsync(5, null, Arg.Any<CancellationToken>());
        await _auditLogWriter.Received(1).LogAsync(
            action: "RESET_PASSWORD",
            tableName: "users",
            recordId: 5,
            userId: 1,
            userEmail: Arg.Any<string?>(),
            userFullname: Arg.Any<string?>(),
            companyId: Arg.Any<long?>(),
            ipAddress: Arg.Any<string?>(),
            userAgent: Arg.Any<string?>(),
            oldValues: Arg.Any<string?>(),
            newValues: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    // GetByIdAsync
    [Fact]
    public async Task GetByIdAsync_WithUserNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = () => _sut.GetByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidUser_ReturnsResponse()
    {
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestBuilders.ActiveUser(id: 1));

        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        result.Id.Should().Be(1);
        result.Email.Should().Be("alice@example.com");
    }
}
