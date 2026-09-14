namespace WAMS.Application.Tests.Services.Users;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Companies;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Services.Users;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Exceptions;
using Xunit;

public sealed class UserCompanyServiceTests
{
    private readonly IUserCompanyRepository _repository = Substitute.For<IUserCompanyRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly IRbacService _rbac = Substitute.For<IRbacService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserPermissionInvalidator _invalidator = Substitute.For<IUserPermissionInvalidator>();
    private readonly IAuthRepository _authRepository = Substitute.For<IAuthRepository>();
    private readonly UserCompanyService _sut;

    public UserCompanyServiceTests()
    {
        _sut = new UserCompanyService(
            _repository,
            _userRepository,
            _companyRepository,
            _rbac,
            _hasher,
            _unitOfWork,
            _invalidator,
            _authRepository);
    }

    [Fact]
    public async Task AddMembership_RejectsDuplicateLiveMembership()
    {
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, IsActive = true });
        _repository.GetUserAsync(10, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, IsActive = true });
        _repository.GetLiveAsync(10, 20, Arg.Any<CancellationToken>())
            .Returns(new UserCompany { Id = 7, UserId = 10, CompanyId = 20 });

        var act = () => _sut.AddMembershipAsync(10, 20, 99);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Message.Should().Be(ErrorMessages.User.AlreadyAssignedToCompany);
        await _repository.DidNotReceive().AddAsync(Arg.Any<UserCompany>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddMembership_RejectsGlobalAccessSubjectBecauseMembershipIsNotRequired()
    {
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, IsActive = true });
        _repository.GetUserAsync(10, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, IsActive = true });
        _rbac.HasGlobalAccessAsync(10, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _sut.AddMembershipAsync(10, 20, 99);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Message.Should().Be(ErrorMessages.User.GlobalAccessMembershipNotApplicable);
        await _repository.DidNotReceive().AddAsync(Arg.Any<UserCompany>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddMembership_ExistingIdentityRequiresSystemAccess()
    {
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, IsActive = true });
        _repository.GetUserAsync(10, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, IsActive = true });
        _repository.GetLiveAsync(10, 20, Arg.Any<CancellationToken>()).Returns((UserCompany?)null);
        _repository.GetAnyAsync(10, 20, Arg.Any<CancellationToken>())
            .Returns((UserCompany?)null);
        _rbac.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.AddMembershipAsync(10, 20, 99);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AddMembership_RestoresWithAssignmentsClearedAndBumpsVersion()
    {
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, IsActive = true, Code = "C20", Name = "Company 20" });
        _repository.GetUserAsync(10, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, IsActive = true });
        _repository.GetLiveAsync(10, 20, Arg.Any<CancellationToken>()).Returns((UserCompany?)null);
        var removed = new UserCompany
        {
            Id = 7,
            UserId = 10,
            CompanyId = 20,
            RemovedAt = DateTime.UtcNow.AddMinutes(-1),
            AuthorizationVersion = 3,
            Roles = [new UserCompanyRole { UserCompanyId = 7, RoleId = 4 }],
            Warehouses = [new UserCompanyWarehouse { UserCompanyId = 7, WarehouseId = 5 }],
            Provinces = [new UserCompanyProvince { UserCompanyId = 7, ProvinceId = 6 }]
        };
        _repository.GetAnyAsync(10, 20, Arg.Any<CancellationToken>()).Returns(removed);
        _rbac.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.AddMembershipAsync(10, 20, 99, ct: TestContext.Current.CancellationToken);

        result.Id.Should().Be(7);
        removed.RemovedAt.Should().BeNull();
        removed.AuthorizationVersion.Should().Be(4);
        removed.Roles.Should().BeEmpty();
        removed.Warehouses.Should().BeEmpty();
        removed.Provinces.Should().BeEmpty();
        await _repository.ClearAssignmentsAsync(7, TestContext.Current.CancellationToken);
        await _repository.IncrementAuthorizationVersionAsync(7, TestContext.Current.CancellationToken);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddMembership_RejectsSuperAdminAndForeignWarehouse()
    {
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, IsActive = true });
        _repository.GetUserAsync(10, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 10, IsActive = true });
        _repository.GetLiveAsync(10, 20, Arg.Any<CancellationToken>()).Returns((UserCompany?)null);
        _repository.GetAnyAsync(10, 20, Arg.Any<CancellationToken>()).Returns((UserCompany?)null);
        _rbac.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetRolesByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([new Role { Id = 1, Name = RoleCodes.SuperAdmin, CompanyId = null }]);

        var act = () => _sut.AddMembershipAsync(
            10,
            20,
            99,
            new AddUserCompanyRequest([1], [8], null, null));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task RemoveMembership_AllowsCompanyAdministratorWithinActingCompany()
    {
        var targetMembership = new UserCompany
        {
            Id = 22,
            UserId = 10,
            CompanyId = 20,
            AuthorizationVersion = 3
        };
        var actorMembership = new UserCompany
        {
            Id = 99,
            UserId = 30,
            CompanyId = 20
        };
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, IsActive = true });
        _repository.GetLiveAsync(10, 20, Arg.Any<CancellationToken>()).Returns(targetMembership);
        _repository.GetLiveAsync(30, 20, Arg.Any<CancellationToken>()).Returns(actorMembership);
        _rbac.HasGlobalAccessAsync(30, Arg.Any<CancellationToken>()).Returns(false);
        _rbac.HasPermissionAsync(30, 99, "user", "user", "update", Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.RemoveMembershipAsync(10, 20, 30, TestContext.Current.CancellationToken);

        targetMembership.RemovedAt.Should().NotBeNull();
        await _repository.Received(1).IncrementAuthorizationVersionAsync(22, TestContext.Current.CancellationToken);
        await _repository.Received(1).RevokeRefreshTokensAsync(22, TestContext.Current.CancellationToken);
        await _invalidator.Received(1).InvalidateMembershipAsync(22, TestContext.Current.CancellationToken);
        await _unitOfWork.Received(1).CommitAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RemoveMembership_RejectsGlobalAccessSubjectAndKeepsCompatibilityMembership()
    {
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, IsActive = true });
        _rbac.HasGlobalAccessAsync(10, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _sut.RemoveMembershipAsync(10, 20, 99);

        var exception = await act.Should().ThrowAsync<ConflictException>();
        exception.Which.Message.Should().Be(ErrorMessages.User.GlobalAccessMembershipNotApplicable);
        await _repository.DidNotReceive().IncrementAuthorizationVersionAsync(
            Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUser_AllowsSuperAdministratorWithoutCompanyMembership()
    {
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, Code = "C20", Name = "Company 20", IsActive = true });
        _repository.GetLiveAsync(99, 20, Arg.Any<CancellationToken>()).Returns((UserCompany?)null);
        _rbac.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetUserByEmailAsync("new@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _hasher.Hash("password").Returns("hash");
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(callInfo.Arg<CancellationToken>()));

        var result = await _sut.CreateUserAsync(
            new CreateUserRequest("new@example.com", "password", "New User", null, null, null, null),
            20,
            99,
            TestContext.Current.CancellationToken);

        result.Email.Should().Be("new@example.com");
        await _rbac.DidNotReceive().HasPermissionAsync(
            99,
            Arg.Any<long>(),
            "user",
            "user",
            "create",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignRole_AllowsSeededSharedRoleWithNoCompany()
    {
        var membership = new UserCompany { Id = 22, UserId = 10, CompanyId = 20 };
        var actorMembership = new UserCompany { Id = 99, UserId = 30, CompanyId = 20 };
        var sharedRole = new Role { Id = 7, Name = "WH_MGR", CompanyId = null };
        _companyRepository.GetByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 20, IsActive = true });
        _repository.GetLiveAsync(10, 20, Arg.Any<CancellationToken>()).Returns(membership);
        _repository.GetLiveAsync(30, 20, Arg.Any<CancellationToken>()).Returns(actorMembership);
        _repository.GetRolesByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([sharedRole]);
        _rbac.HasGlobalAccessAsync(30, Arg.Any<CancellationToken>()).Returns(false);
        _rbac.HasPermissionAsync(30, 99, "user", "user", "update", Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.AssignRoleAsync(10, 20, 7, 30, TestContext.Current.CancellationToken);

        membership.Roles.Should().ContainSingle().Which.RoleId.Should().Be(7);
        await _unitOfWork.Received(1).CommitAsync(TestContext.Current.CancellationToken);
    }
}
