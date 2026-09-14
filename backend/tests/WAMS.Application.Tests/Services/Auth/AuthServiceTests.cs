namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.DTOs.Auth;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Services.Auth;
using WAMS.Application.Tests.Helpers;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Auth;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Exceptions;
using Xunit;

public class AuthServiceTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IAuthRepository _authRepo = Substitute.For<IAuthRepository>();
    private readonly IRbacRepository _rbacRepo = Substitute.For<IRbacRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ITokenService _tokenSvc = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogWriter _auditLogWriter = Substitute.For<IAuditLogWriter>();
    private readonly IWamsMetrics _metrics = Substitute.For<IWamsMetrics>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ExpirationMinutes"] = "15",
                ["Jwt:RefreshExpirationMinutes"] = "10080"
            })
            .Build();
        _companyRepo.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new Company { Id = callInfo.Arg<long>(), IsActive = true });
        _sut = new AuthService(_userRepo, _authRepo, _rbacRepo, _hasher, _companyRepo, _tenantContext, _tokenSvc, _uow, config, _auditLogWriter, _metrics);
    }

    // LoginAsync
    [Fact]
    public async Task LoginAsync_WithUserNotFound_ThrowsUnauthorizedException()
    {
        _userRepo.GetByEmailWithRolesAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();

        var act = () => _sut.LoginAsync(new LoginRequest("x@x.com", "pass", 1), null, null);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ThrowsUnauthorizedException()
    {
        _userRepo.GetByEmailWithRolesAsync("inactive@example.com", TestContext.Current.CancellationToken).Returns(TestBuilders.InactiveUser());

        var act = () => _sut.LoginAsync(new LoginRequest("INACTIVE@EXAMPLE.COM", "pass", 1), null, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*inactive*");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedException()
    {
        _userRepo.GetByEmailWithRolesAsync("alice@example.com", TestContext.Current.CancellationToken).Returns(TestBuilders.ActiveUser());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequest("ALICE@EXAMPLE.COM", "wrong", 1), null, null);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_WithMismatchedCompanyForNonWildcardUser_ThrowsUnauthorizedException()
    {
        var user = TestBuilders.ActiveUser(companyId: 1);
        _userRepo.GetByEmailWithRolesAsync("alice@example.com", TestContext.Current.CancellationToken).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var act = () => _sut.LoginAsync(new LoginRequest("ALICE@EXAMPLE.COM", "pass", 2), null, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponseAndCommits()
    {
        var user = TestBuilders.ActiveUser();
        _userRepo.GetByEmailWithRolesAsync("alice@example.com", TestContext.Current.CancellationToken).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("access-token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh-token");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        var result = await _sut.LoginAsync(new LoginRequest("ALICE@EXAMPLE.COM", "pass", 1), "127.0.0.1", "chrome", TestContext.Current.CancellationToken);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.ExpiresIn.Should().Be(900);
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _auditLogWriter.Received(1).LogAsync(
            action: "LOGIN",
            tableName: "users",
            recordId: user.Id,
            userId: user.Id,
            userEmail: user.Email,
            userFullname: user.Fullname,
            companyId: user.CompanyId,
            ipAddress: "127.0.0.1",
            userAgent: "chrome",
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_SuperAdminChoosingDifferentActiveCompany_ScopesTokenToChosenCompany()
    {
        var sa = TestBuilders.SuperAdminUser(companyId: 1); // home company 1
        _userRepo.GetByEmailWithRolesAsync("sa@example.com", TestContext.Current.CancellationToken).Returns(sa);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _companyRepo.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(TestBuilders.Company(id: 2, isActive: true));
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("sa-token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        await _sut.LoginAsync(new LoginRequest("SA@EXAMPLE.COM", "pass", 2), null, null, TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), 2L, true);
        await _authRepo.Received(1).CreateRefreshTokenAsync(
            Arg.Is<RefreshToken>(rt => rt.CompanyId == 2L), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_SuperAdminChoosingInactiveCompany_ThrowsUnauthorizedException()
    {
        var sa = TestBuilders.SuperAdminUser(companyId: 1);
        _userRepo.GetByEmailWithRolesAsync("sa@example.com", TestContext.Current.CancellationToken).Returns(sa);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _companyRepo.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(TestBuilders.Company(id: 3, isActive: false));

        var act = () => _sut.LoginAsync(new LoginRequest("SA@EXAMPLE.COM", "pass", 3), null, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_SuperAdminChoosingNonexistentCompany_ThrowsUnauthorizedException()
    {
        var sa = TestBuilders.SuperAdminUser(companyId: 1);
        _userRepo.GetByEmailWithRolesAsync("sa@example.com", TestContext.Current.CancellationToken).Returns(sa);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _companyRepo.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var act = () => _sut.LoginAsync(new LoginRequest("SA@EXAMPLE.COM", "pass", 999), null, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    // RefreshAsync
    [Fact]
    public async Task RefreshAsync_WithInvalidToken_ThrowsUnauthorizedException()
    {
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();

        var act = () => _sut.RefreshAsync(new RefreshRequest("badtoken"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredToken_ThrowsUnauthorizedException()
    {
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(TestBuilders.ExpiredRefreshToken());

        var act = () => _sut.RefreshAsync(new RefreshRequest("sometoken"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedToken_ThrowsUnauthorizedException()
    {
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(TestBuilders.RevokedRefreshToken());

        var act = () => _sut.RefreshAsync(new RefreshRequest("sometoken"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*revoked*");
    }

    [Fact]
    public async Task RefreshAsync_WithIdleTokenPastWindow_ThrowsSessionIdleTimeoutExceptionAndRevokes()
    {
        var stored = TestBuilders.ActiveRefreshToken(userId: 1);
        stored.CreatedAt = DateTime.UtcNow.AddMinutes(-31);
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);

        var act = () => _sut.RefreshAsync(new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<SessionIdleTimeoutException>()
            .WithMessage("*inactivity*");
        await _authRepo.Received(1).RevokeRefreshTokenAsync(stored.Id, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WithTokenWithinIdleWindow_Succeeds()
    {
        var stored = TestBuilders.ActiveRefreshToken(userId: 1);
        stored.CreatedAt = DateTime.UtcNow.AddMinutes(-29);
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("new-access");
        _tokenSvc.GenerateRefreshToken().Returns("new-refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        var result = await _sut.RefreshAsync(new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        result.AccessToken.Should().Be("new-access");
    }

    [Fact]
    public async Task RefreshAsync_WithValidToken_RotatesTokenAndCommits()
    {
        var stored = TestBuilders.ActiveRefreshToken(userId: 1);
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("new-access");
        _tokenSvc.GenerateRefreshToken().Returns("new-refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        var result = await _sut.RefreshAsync(new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        result.AccessToken.Should().Be("new-access");
        await _authRepo.Received(1).RevokeRefreshTokenAsync(stored.Id, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WithZeroCompanyId_ThrowsUnauthorizedExceptionAndDoesNotRevoke()
    {
        var stored = TestBuilders.ActiveRefreshToken(userId: 1, companyId: 0);
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);

        var act = () => _sut.RefreshAsync(new RefreshRequest("stale-token"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
        await _authRepo.DidNotReceive().RevokeRefreshTokenAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_WithInactiveUser_ThrowsUnauthorizedExceptionAndDoesNotRevoke()
    {
        var stored = TestBuilders.ActiveRefreshToken(userId: 1);
        stored.User.IsActive = false;
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);

        var act = () => _sut.RefreshAsync(new RefreshRequest("inactive-user-token"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
        await _authRepo.DidNotReceive().RevokeRefreshTokenAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        _tokenSvc.DidNotReceive().GenerateAccessToken(
            Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task RefreshAsync_WithInactiveActingCompany_ThrowsUnauthorizedExceptionAndDoesNotRevoke()
    {
        var stored = TestBuilders.SuperAdminRefreshToken(userId: 99, companyId: 2);
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);
        _companyRepo.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(new Company { Id = 2, IsActive = false });

        var act = () => _sut.RefreshAsync(new RefreshRequest("inactive-company-token"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
        await _authRepo.DidNotReceive().RevokeRefreshTokenAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        _tokenSvc.DidNotReceive().GenerateAccessToken(
            Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task RefreshAsync_SuperAdminActingAsNonHomeCompany_PreservesActingCompanyAcrossRotation()
    {
        var stored = TestBuilders.SuperAdminRefreshToken(userId: 99, companyId: 1);
        stored.CompanyId = 2; // acting as company 2, home company is 1
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("sa-new-access");
        _tokenSvc.GenerateRefreshToken().Returns("new-refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        await _sut.RefreshAsync(new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), 2L, true);
        await _authRepo.Received(1).CreateRefreshTokenAsync(
            Arg.Is<RefreshToken>(rt => rt.CompanyId == 2L), Arg.Any<CancellationToken>());
    }

    // LogoutAsync
    [Fact]
    public async Task LogoutAsync_WithValidToken_BlacklistsJtiAndRevokesRefreshToken()
    {
        var stored = TestBuilders.ActiveRefreshToken(userId: 1);
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);

        await _sut.LogoutAsync(1, "jti-xyz", "raw-refresh-token", TestContext.Current.CancellationToken);

        await _tokenSvc.Received(1).BlacklistTokenAsync("jti-xyz", Arg.Is<TimeSpan>(ts => ts.TotalMinutes == 15));
        await _authRepo.Received(1).RevokeRefreshTokenAsync(stored.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutAsync_WithNoStoredRefreshToken_OnlyBlacklists()
    {
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).ReturnsNull();

        await _sut.LogoutAsync(1, "jti-xyz", "unknown-token", TestContext.Current.CancellationToken);

        await _tokenSvc.Received(1).BlacklistTokenAsync("jti-xyz", Arg.Any<TimeSpan>());
        await _authRepo.DidNotReceive().RevokeRefreshTokenAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    // ChangePasswordAsync
    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ThrowsUnauthorizedException()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var act = () => _sut.ChangePasswordAsync(1, new ChangePasswordRequest("wrong", "newpassword1"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_WithEmail_VerifiesCurrentPasswordNormalizesAndAudits()
    {
        var user = TestBuilders.ActiveUser(id: 1, email: "old@example.com");
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.GetByIdUnfilteredReadOnlyAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _userRepo.GetByEmailAsync("new@example.com", Arg.Any<CancellationToken>()).ReturnsNull();
        _hasher.Verify("oldpass1", "hashed").Returns(true);
        _rbacRepo.GetUserPermissionKeysAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.UpdateProfileAsync(
            1,
            new UpdateProfileRequest("Alice Updated", "  NEW@example.com ", "oldpass1"),
            TestContext.Current.CancellationToken);

        result.Email.Should().Be("new@example.com");
        result.Fullname.Should().Be("Alice Updated");
        user.Email.Should().Be("new@example.com");
        user.Fullname.Should().Be("Alice Updated");
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _auditLogWriter.Received(1).LogAsync(
            action: "CHANGE_EMAIL",
            tableName: "users",
            recordId: 1,
            userId: 1,
            userEmail: "new@example.com",
            userFullname: "Alice Updated",
            companyId: 1,
            ipAddress: Arg.Any<string?>(),
            userAgent: Arg.Any<string?>(),
            oldValues: Arg.Any<string?>(),
            newValues: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectPasswordAndNoRefreshToken_RevokesAllTokensAndCommits()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("oldpass1", "hashed").Returns(true);
        _hasher.Hash("newpassword1").Returns("new-hashed");

        await _sut.ChangePasswordAsync(1, new ChangePasswordRequest("oldpass1", "newpassword1"), TestContext.Current.CancellationToken);

        user.PasswordHash.Should().Be("new-hashed");
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _authRepo.Received(1).RevokeAllUserTokensAsync(1, null, Arg.Any<CancellationToken>());
        await _auditLogWriter.Received(1).LogAsync(
            action: "CHANGE_PASSWORD",
            tableName: "users",
            recordId: 1,
            userId: 1,
            userEmail: user.Email,
            userFullname: user.Fullname,
            companyId: user.CompanyId,
            ipAddress: Arg.Any<string?>(),
            userAgent: Arg.Any<string?>(),
            oldValues: Arg.Any<string?>(),
            newValues: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WithRefreshTokenProvided_RevokesEverySession()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        await _sut.ChangePasswordAsync(1, new ChangePasswordRequest("oldpass1", "newpassword1", "current-refresh"), TestContext.Current.CancellationToken);

        await _authRepo.DidNotReceive().GetRefreshTokenByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _authRepo.Received(1).RevokeAllUserTokensAsync(1, null, Arg.Any<CancellationToken>());
        await _userRepo.Received(1).IncrementSessionVersionAsync(1, Arg.Any<CancellationToken>());
    }

    // GetCurrentUserAsync
    [Fact]
    public async Task GetCurrentUserAsync_WithUserNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdUnfilteredReadOnlyAsync(99, TestContext.Current.CancellationToken).ReturnsNull();

        var act = () => _sut.GetCurrentUserAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithValidUser_ReturnsMeResponse()
    {
        var user = TestBuilders.ActiveUser(id: 1);
        _userRepo.GetByIdUnfilteredReadOnlyAsync(1, TestContext.Current.CancellationToken).Returns(user);
        _rbacRepo.GetUserPermissionKeysAsync(1, Arg.Any<CancellationToken>()).Returns(["user.user.read"]);

        var result = await _sut.GetCurrentUserAsync(1, TestContext.Current.CancellationToken);

        result.Id.Should().Be(1);
        result.Email.Should().Be("alice@example.com");
        result.IsActive.Should().BeTrue();
        result.Permissions.Should().Contain("user.user.read");
        result.PermissionMap["user"]["user"].Should().Contain("read");
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsProvinceScope()
    {
        var user = new User
        {
            Id = 7, Email = "u@t.c", Fullname = "U", IsActive = true,
            Company = new Company { Id = 1, Name = "C", Code = "C001" },
            UserProvinces =
            {
                new UserProvince { ProvinceId = 3, Province = new Province { Id = 3, Name = "LAMPUNG", Display = "Lampung" } }
            }
        };
        _userRepo.GetByIdUnfilteredReadOnlyAsync(7, Arg.Any<CancellationToken>()).Returns(user);
        _rbacRepo.GetUserPermissionKeysAsync(7, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.GetCurrentUserAsync(7, TestContext.Current.CancellationToken);

        result.Scopes.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MeProvinceResponse(3, "LAMPUNG", "Lampung"));
    }

    [Fact]
    public async Task GetCurrentUserAsync_SuperAdminActingAsDifferentCompany_ReturnsActingCompanyNotHomeCompany()
    {
        var user = TestBuilders.ActiveUser(id: 1, companyId: 1); // home company 1
        _userRepo.GetByIdUnfilteredReadOnlyAsync(1, TestContext.Current.CancellationToken).Returns(user);
        _rbacRepo.GetUserPermissionKeysAsync(1, Arg.Any<CancellationToken>()).Returns([]);
        _tenantContext.CompanyId.Returns(2L); // acting as company 2
        _companyRepo.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(TestBuilders.Company(id: 2, code: "OTHER"));

        var result = await _sut.GetCurrentUserAsync(1, TestContext.Current.CancellationToken);

        result.CompanyId.Should().Be(2);
        result.CompanyCode.Should().Be("OTHER");
    }

    // HashToken (static)
    [Fact]
    public void HashToken_WithSameInput_ProducesSameOutput()
    {
        var hash1 = AuthService.HashToken("my-secret-token");
        var hash2 = AuthService.HashToken("my-secret-token");

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64); // SHA-256 hex = 64 chars
    }

    [Fact]
    public void HashToken_WithDifferentInputs_ProducesDifferentHashes()
    {
        var h1 = AuthService.HashToken("token-a");
        var h2 = AuthService.HashToken("token-b");

        h1.Should().NotBe(h2);
    }

    // Wildcard / hasWildcard flag propagation
    [Fact]
    public async Task LoginAsync_WithSuperAdminUser_PassesHasWildcardTrueToTokenService()
    {
        var sa = TestBuilders.SuperAdminUser();
        _userRepo.GetByEmailWithRolesAsync("sa@example.com", TestContext.Current.CancellationToken).Returns(sa);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _companyRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestBuilders.Company(id: 1, isActive: true));
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("sa-token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        await _sut.LoginAsync(new LoginRequest("SA@EXAMPLE.COM", "pass", 1), null, null, TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), true);
    }

    [Fact]
    public async Task LoginAsync_WithRegularUser_PassesHasWildcardFalseToTokenService()
    {
        var user = TestBuilders.ActiveUser();
        _userRepo.GetByEmailWithRolesAsync("alice@example.com", TestContext.Current.CancellationToken).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        await _sut.LoginAsync(new LoginRequest("ALICE@EXAMPLE.COM", "pass", 1), null, null, TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), false);
    }

    [Fact]
    public async Task RefreshAsync_WithSuperAdminUser_PassesHasWildcardTrueToTokenService()
    {
        var stored = TestBuilders.SuperAdminRefreshToken();
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("sa-new-access");
        _tokenSvc.GenerateRefreshToken().Returns("new-refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        await _sut.RefreshAsync(new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), true);
    }

    [Fact]
    public async Task LoginAsync_WithGlobalAccessUser_PassesHasWildcardFalseToTokenService()
    {
        var user = TestBuilders.GlobalAccessUser();
        _userRepo.GetByEmailWithRolesAsync("ho-spv@example.com", TestContext.Current.CancellationToken).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        await _sut.LoginAsync(new LoginRequest("HO-SPV@EXAMPLE.COM", "pass", 1), null, null, TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), false);
    }

    [Fact]
    public async Task RefreshAsync_WithGlobalAccessUser_PassesHasWildcardFalseToTokenService()
    {
        var stored = TestBuilders.GlobalAccessRefreshToken();
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        await _sut.RefreshAsync(new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), false);
    }

    [Fact]
    public async Task RefreshAsync_WithRegularUser_PassesHasWildcardFalseToTokenService()
    {
        var stored = TestBuilders.ActiveRefreshToken();
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), TestContext.Current.CancellationToken).Returns(stored);
        _tokenSvc.GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), Arg.Any<bool>()).Returns("token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), TestContext.Current.CancellationToken).Returns(new RefreshToken());

        await _sut.RefreshAsync(new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(Arg.Any<User>(), Arg.Any<List<string>>(), Arg.Any<long>(), false);
    }

    [Fact]
    public async Task LoginAsync_AllowsSameIdentityInTwoMembershipCompanies()
    {
        var user = TestBuilders.ActiveUser(id: 7, companyId: 1, email: "shared@example.com");
        var membership = new UserCompany
        {
            Id = 22,
            UserId = user.Id,
            CompanyId = 2,
            AuthorizationVersion = 4,
            Roles =
            [
                new UserCompanyRole
                {
                    UserCompanyId = 22,
                    RoleId = 30,
                    Role = new Role { Id = 30, Name = "VIEWER" }
                }
            ]
        };
        _userRepo.GetLoginIdentityAsync("shared@example.com", 2, Arg.Any<CancellationToken>())
            .Returns((user, (UserCompany?)membership));
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenSvc.GenerateAccessToken(
                Arg.Any<User>(), Arg.Any<IReadOnlyCollection<string>>(), 2, 22, 4, false)
            .Returns("membership-token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshToken());

        var result = await _sut.LoginAsync(
            new LoginRequest("SHARED@EXAMPLE.COM", "pass", 2), null, null,
            TestContext.Current.CancellationToken);

        result.AccessToken.Should().Be("membership-token");
        _tokenSvc.Received(1).GenerateAccessToken(
            user,
            Arg.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(new[] { "VIEWER" })),
            2,
            22,
            4,
            false);
    }

    [Fact]
    public async Task LoginAsync_RejectsRegularUserWithoutMembershipUsingGenericCredentialsError()
    {
        var user = TestBuilders.ActiveUser(id: 7, companyId: 1, email: "shared@example.com");
        _userRepo.GetLoginIdentityAsync("shared@example.com", 2, Arg.Any<CancellationToken>())
            .Returns((user, (UserCompany?)null));
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var act = () => _sut.LoginAsync(
            new LoginRequest("SHARED@EXAMPLE.COM", "pass", 2), null, null,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage(ErrorMessages.Auth.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_UsesOnlySelectedMembershipRoles()
    {
        var user = TestBuilders.ActiveUser(id: 7, companyId: 1, email: "shared@example.com");
        user.UserRoles =
        [
            new UserRole { UserId = user.Id, RoleId = 10, Role = new Role { Id = 10, Name = "COMPANY_A_ADMIN" } }
        ];
        var membership = new UserCompany
        {
            Id = 23,
            UserId = user.Id,
            CompanyId = 2,
            Roles =
            [new UserCompanyRole
            {
                UserCompanyId = 23,
                RoleId = 11,
                Role = new Role { Id = 11, Name = "COMPANY_B_VIEWER" }
            }]
        };
        _userRepo.GetLoginIdentityAsync("shared@example.com", 2, Arg.Any<CancellationToken>())
            .Returns((user, (UserCompany?)membership));
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenSvc.GenerateAccessToken(
                Arg.Any<User>(), Arg.Any<IReadOnlyCollection<string>>(), 2, 23, Arg.Any<int?>(), false)
            .Returns("token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshToken());

        await _sut.LoginAsync(
            new LoginRequest("SHARED@EXAMPLE.COM", "pass", 2), null, null,
            TestContext.Current.CancellationToken);

        _tokenSvc.Received(1).GenerateAccessToken(
            Arg.Any<User>(),
            Arg.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(new[] { "COMPANY_B_VIEWER" })),
            2,
            23,
            Arg.Any<int?>(),
            false);
    }

    [Fact]
    public async Task LoginAsync_AllowsSuperAdminWithoutMembership()
    {
        var user = TestBuilders.SuperAdminUser(companyId: 1);
        _userRepo.GetLoginIdentityAsync("sa@example.com", 2, Arg.Any<CancellationToken>())
            .Returns((user, (UserCompany?)null));
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _companyRepo.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(TestBuilders.Company(id: 2, isActive: true));
        _tokenSvc.GenerateAccessToken(
                Arg.Any<User>(), Arg.Any<IReadOnlyCollection<string>>(), 2, null, null, true)
            .Returns("sa-token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshToken());

        var result = await _sut.LoginAsync(
            new LoginRequest("SA@EXAMPLE.COM", "pass", 2), null, null,
            TestContext.Current.CancellationToken);

        result.AccessToken.Should().Be("sa-token");
        _tokenSvc.Received(1).GenerateAccessToken(
            user, Arg.Any<IReadOnlyCollection<string>>(), 2, null, null, true);
    }

    [Fact]
    public async Task LoginAsync_SuperAdminWithMigratedMembership_UsesGlobalSystemRolesAndNoMembershipClaim()
    {
        var user = TestBuilders.SuperAdminUser(companyId: 1);
        var membership = new UserCompany
        {
            Id = 55,
            UserId = user.Id,
            CompanyId = 2,
            AuthorizationVersion = 1,
            Roles = []
        };
        _userRepo.GetLoginIdentityAsync("sa@example.com", 2, Arg.Any<CancellationToken>())
            .Returns((user, (UserCompany?)membership));
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _companyRepo.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(TestBuilders.Company(id: 2, isActive: true));
        _tokenSvc.GenerateAccessToken(
                Arg.Any<User>(), Arg.Any<IReadOnlyCollection<string>>(), 2, null, null, true)
            .Returns("sa-token");
        _tokenSvc.GenerateRefreshToken().Returns("refresh");
        _authRepo.CreateRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshToken());

        var result = await _sut.LoginAsync(
            new LoginRequest("SA@EXAMPLE.COM", "pass", 2), null, null,
            TestContext.Current.CancellationToken);

        result.AccessToken.Should().Be("sa-token");
        _tokenSvc.Received(1).GenerateAccessToken(
            user,
            Arg.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(new[] { "SUPERADMIN" })),
            2,
            null,
            null,
            true);
        await _authRepo.Received(1).CreateRefreshTokenAsync(
            Arg.Is<RefreshToken>(token => token.CompanyId == 2 && token.UserCompanyId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_RejectsRemovedOrVersionChangedMembership()
    {
        var stored = TestBuilders.ActiveRefreshToken(userId: 7, companyId: 2);
        stored.UserCompanyId = 42;
        stored.MembershipAuthorizationVersion = 3;
        stored.User.UserCompanies =
        [new UserCompany
        {
            Id = 42,
            UserId = 7,
            CompanyId = 2,
            AuthorizationVersion = 4,
            RemovedAt = null
        }];
        _authRepo.GetRefreshTokenByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(stored);

        var act = () => _sut.RefreshAsync(
            new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage(ErrorMessages.Auth.InvalidRefreshToken);
        await _authRepo.DidNotReceive().RevokeRefreshTokenAsync(
            Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCurrentUserAsync_SuperAdminWithMigratedMembership_UsesSystemRoleAndNoMembershipScope()
    {
        var user = TestBuilders.SuperAdminUser(companyId: 1);
        user.UserCompanies =
        [new UserCompany
        {
            Id = 55,
            UserId = user.Id,
            CompanyId = 1,
            Roles = []
        }];
        _userRepo.GetByIdUnfilteredReadOnlyAsync(99, Arg.Any<CancellationToken>()).Returns(user);
        _tenantContext.UserCompanyId.Returns(55L);
        _rbacRepo.GetUserPermissionKeysAsync(99, Arg.Any<CancellationToken>())
            .Returns(["*.*.*"]);

        var result = await _sut.GetCurrentUserAsync(99, TestContext.Current.CancellationToken);

        result.Roles.Should().Contain("SUPERADMIN");
        result.Permissions.Should().Contain("*.*.*");
        result.HasGlobalAccess.Should().BeTrue();
        result.UserCompanyId.Should().BeNull();
    }
}
