namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Companies;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Files;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Services.Companies;
using WAMS.Application.Tests.Helpers;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Exceptions;
using Xunit;

public class CompanyServiceTests
{
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IFileAttachmentStorage _storage = Substitute.For<IFileAttachmentStorage>();
    private readonly IFileMimeDetector _mimeDetector = Substitute.For<IFileMimeDetector>();
    private readonly CompanyService _sut;

    public CompanyServiceTests()
    {
        _sut = new CompanyService(
            _companyRepo,
            _userRepo,
            NullLogger<CompanyService>.Instance,
            _uow,
            _tenantContext,
            _storage,
            _mimeDetector);
    }

    // CreateAsync
    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ThrowsConflictException()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.CodeExistsAsync("ACME", ct).Returns(true);

        var act = () => _sut.CreateAsync(new CreateCompanyRequest("ACME", "Acme Corp", null, null, null), ct);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateAsync_WithUniqueCode_CreatesAndCommits()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.CodeExistsAsync("NEWCO", ct).Returns(false);
        var company = TestBuilders.Company(id: 5, code: "NEWCO");
        _companyRepo.CreateAsync(Arg.Any<Company>(), ct).Returns(company);

        var result = await _sut.CreateAsync(new CreateCompanyRequest("newco", "New Company", null, null, null), ct);

        result.Code.Should().Be("NEWCO");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // GetByIdAsync
    [Fact]
    public async Task GetByIdAsync_WithCompanyNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.GetByIdWithCountsAsync(99, ct).ReturnsNull();

        var act = () => _sut.GetByIdAsync(99, ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCompanyResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.GetByIdWithCountsAsync(1, ct).Returns(TestBuilders.CompanyResponse(id: 1));

        var result = await _sut.GetByIdAsync(1, ct);

        result.Id.Should().Be(1);
        result.Code.Should().Be("ACME");
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCorrectUserAndWarehouseCounts()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.GetByIdWithCountsAsync(1, ct).Returns(TestBuilders.CompanyResponse(id: 1, userCount: 5, warehouseCount: 3));

        var result = await _sut.GetByIdAsync(1, ct);

        result.UserCount.Should().Be(5);
        result.WarehouseCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsUserAndWarehouseCounts()
    {
        var responses = new List<CompanyResponse>
        {
            TestBuilders.CompanyResponse(id: 1, userCount: 4, warehouseCount: 2),
            TestBuilders.CompanyResponse(id: 2, userCount: 0, warehouseCount: 1),
        };
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.GetAllAsync(Arg.Any<DataTableQuery>(), ct).Returns((responses, 2));

        var result = await _sut.GetAllAsync(new DataTableQuery { Page = 1, Limit = 10 }, ct);

        result.Data[0].UserCount.Should().Be(4);
        result.Data[0].WarehouseCount.Should().Be(2);
        result.Data[1].UserCount.Should().Be(0);
    }

    // DeactivateAsync
    [Fact]
    public async Task DeactivateAsync_WithDefaultCompany_ThrowsForbiddenException()
    {
        var ct = TestContext.Current.CancellationToken;
        var defaultCompany = TestBuilders.Company(code: "DEFAULT");
        _companyRepo.GetByIdAsync(1, ct).Returns(defaultCompany);

        var act = () => _sut.DeactivateAsync(1, ct);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*default*");
    }

    [Fact]
    public async Task DeactivateAsync_WithCompanyNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.GetByIdAsync(99, ct).ReturnsNull();

        var act = () => _sut.DeactivateAsync(99, ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_WithValidCompany_SetsInactiveAndCommits()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5, code: "ACME", isActive: true);
        _companyRepo.GetByIdAsync(5, ct).Returns(company);

        await _sut.DeactivateAsync(5, ct);

        company.IsActive.Should().BeFalse();
        await _companyRepo.Received(1).UpdateAsync(company, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // AssignUserToCompanyAsync
    [Fact]
    public async Task AssignUserToCompanyAsync_WithCompanyNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.ExistsAsync(99, ct).Returns(false);

        var act = () => _sut.AssignUserToCompanyAsync(userId: 1, companyId: 99, ct: ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignUserToCompanyAsync_WithUserNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.ExistsAsync(5, ct).Returns(true);
        _userRepo.GetByIdUnfilteredAsync(99, ct).ReturnsNull();

        var act = () => _sut.AssignUserToCompanyAsync(userId: 99, companyId: 5, ct: ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignUserToCompanyAsync_WhenUserAlreadyInTargetCompany_ThrowsConflictException()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = TestBuilders.ActiveUser(id: 1, companyId: 5);
        _companyRepo.ExistsAsync(5, ct).Returns(true);
        _userRepo.GetByIdUnfilteredAsync(1, ct).Returns(user);

        var act = () => _sut.AssignUserToCompanyAsync(userId: 1, companyId: 5, ct: ct);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AssignUserToCompanyAsync_WithValidMigration_ClearsWarehousesAndCommits()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = TestBuilders.ActiveUser(id: 1, companyId: 1); // was in company 1
        _companyRepo.ExistsAsync(5, ct).Returns(true);
        _userRepo.GetByIdUnfilteredAsync(1, ct).Returns(user);

        await _sut.AssignUserToCompanyAsync(userId: 1, companyId: 5, ct: ct);

        user.CompanyId.Should().Be(5);
        await _userRepo.Received(1).ClearWarehouseAssignmentsAsync(1, Arg.Any<CancellationToken>());
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // UpdateAsync
    [Fact]
    public async Task UpdateAsync_WithCompanyNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.GetByIdAsync(99, ct).ReturnsNull();

        var act = () => _sut.UpdateAsync(99, new UpdateCompanyRequest("New Name", null, null, null, null), ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithValidCompany_UpdatesAndCommits()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 1);
        _companyRepo.GetByIdAsync(1, ct).Returns(company);

        var result = await _sut.UpdateAsync(1, new UpdateCompanyRequest("Updated Name", null, null, null, false), ct);

        result.Name.Should().Be("Updated Name");
        result.IsActive.Should().BeFalse();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // GetLogoAsync 

    [Fact]
    public async Task GetLogoAsync_CompanyNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.GetByIdAsync(99, ct).ReturnsNull();

        var act = () => _sut.GetLogoAsync(99, ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetLogoAsync_NoLogo_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        company.LogoStorageKey = null;
        _companyRepo.GetByIdAsync(5, ct).Returns(company);

        var act = () => _sut.GetLogoAsync(5, ct);

        await act.Should().ThrowAsync<NotFoundException>();
        await _storage.DidNotReceive().OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLogoAsync_WithPngLogo_ReturnsStreamAndPngContentType()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        company.LogoStorageKey = "logos/5/abc.png";
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        var fakeStream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
        _storage.OpenReadAsync("logos/5/abc.png", Arg.Any<CancellationToken>())
            .Returns(new StoredFileStream(fakeStream, null));

        var (content, contentType) = await _sut.GetLogoAsync(5, ct);

        content.Should().BeSameAs(fakeStream);
        contentType.Should().Be("image/png");
    }

    // Logo tests 

    [Fact]
    public async Task UploadLogoAsync_ScopedUser_OwnCompany_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(5L);
        _mimeDetector.Detect(Arg.Any<byte[]>(), Arg.Any<int>())
            .Returns("image/png");

        await _sut.UploadLogoAsync(5, new MemoryStream([0x89, 0x50, 0x4E, 0x47]), "image/png", ct);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogoAsync_ScopedUser_WrongCompany_ThrowsForbiddenException()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(99L); // different company

        var act = () => _sut.UploadLogoAsync(5, new MemoryStream(), "image/png", ct);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogoAsync_CompanyNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _companyRepo.GetByIdAsync(99, ct).Returns((Company?)null);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns((long?)null);

        var act = () => _sut.UploadLogoAsync(99, new MemoryStream(), "image/png", ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UploadLogoAsync_InvalidSignature_ThrowsValidationException()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(5L);
        _mimeDetector.Detect(Arg.Any<byte[]>(), Arg.Any<int>())
            .Returns((string?)null);

        var act = () => _sut.UploadLogoAsync(5, new MemoryStream([0x00, 0x00, 0x00, 0x00]), "image/png", ct);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UploadLogoAsync_ReplacingExistingLogo_DeletesOldFileAfterCommit()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        company.LogoStorageKey = "logos/5/old.png";
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(5L);
        _mimeDetector.Detect(Arg.Any<byte[]>(), Arg.Any<int>())
            .Returns("image/png");

        await _sut.UploadLogoAsync(5, new MemoryStream([0x89, 0x50, 0x4E, 0x47]), "image/png", ct);

        Received.InOrder(() =>
        {
            _uow.CommitAsync(Arg.Any<CancellationToken>());
            _storage.DeleteAsync("logos/5/old.png", Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RemoveLogoAsync_WithExistingLogo_ClearsKeyAndDeletesFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        company.LogoStorageKey = "logos/5/old.png";
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(5L);

        await _sut.RemoveLogoAsync(5, ct);

        company.LogoStorageKey.Should().BeNull();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _storage.Received(1).DeleteAsync("logos/5/old.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveLogoAsync_WithNoLogo_NoOp_NoStorageDelete()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        company.LogoStorageKey = null;
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(5L);

        await _sut.RemoveLogoAsync(5, ct);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogoAsync_InvalidContentType_ThrowsValidationException()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(5L);

        var act = () => _sut.UploadLogoAsync(5, new MemoryStream(), "image/gif", ct);

        await act.Should().ThrowAsync<ValidationException>();
        await _storage.DidNotReceive().SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogoAsync_OversizeFile_ThrowsValidationException()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(5L);

        // 3 MB stream - over the 2 MB limit
        var oversizeStream = new MemoryStream(new byte[3 * 1024 * 1024]);

        var act = () => _sut.UploadLogoAsync(5, oversizeStream, "image/png", ct);

        await act.Should().ThrowAsync<ValidationException>();
        await _storage.DidNotReceive().SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogoAsync_ActingAsDifferentCompany_ThrowsForbiddenException()
    {
        var ct = TestContext.Current.CancellationToken;
        var company = TestBuilders.Company(id: 5);
        _companyRepo.GetByIdAsync(5, ct).Returns(company);
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(1L); // acting as company 1, not the target company 5

        var act = () => _sut.UploadLogoAsync(5, new MemoryStream([0x89, 0x50, 0x4E, 0x47]), "image/png", ct);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
