namespace WAMS.Application.Tests.Export;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Files;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Exceptions;
using WAMS.Infrastructure.Export;

public class PdfMetadataResolverTests
{
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly IFileAttachmentStorage _storage = Substitute.For<IFileAttachmentStorage>();

    private PdfMetadataResolver BuildSut() =>
        new PdfMetadataResolver(_tenantContext, _companyRepo, _storage, NullLogger<PdfMetadataResolver>.Instance);

    [Fact]
    public async Task ResolveAsync_WithTenantCompany_UsesCompanyName()
    {
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(42L);
        var company = new Company { Id = 42, Name = "Acme Corp" };
        _companyRepo.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(company);

        var result = await BuildSut().ResolveAsync("Items Report", TestContext.Current.CancellationToken);

        result.CompanyName.Should().Be("Acme Corp");
        result.Title.Should().Be("Items Report");
        result.LogoData.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithTenantCompany_ReadsLogoBytesFromStorage()
    {
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(42L);
        var company = new Company { Id = 42, Name = "Acme Corp", LogoStorageKey = "logos/42/abc.png" };
        _companyRepo.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(company);

        var fakeBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var fakeStream = new StoredFileStream(new MemoryStream(fakeBytes), null);
        _storage.OpenReadAsync("logos/42/abc.png", Arg.Any<CancellationToken>()).Returns(fakeStream);

        var result = await BuildSut().ResolveAsync("Items Report", TestContext.Current.CancellationToken);

        result.LogoData.Should().BeEquivalentTo(fakeBytes);
    }

    [Fact]
    public async Task ResolveAsync_WithTenantCompany_LogoNotFoundInStorage_ReturnsNullLogo()
    {
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(42L);
        var company = new Company { Id = 42, Name = "Acme Corp", LogoStorageKey = "logos/42/missing.png" };
        _companyRepo.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(company);
        _storage.OpenReadAsync("logos/42/missing.png", Arg.Any<CancellationToken>())
            .ThrowsAsync(new NotFoundException("Stored file not found"));

        var result = await BuildSut().ResolveAsync("Items Report", TestContext.Current.CancellationToken);

        result.LogoData.Should().BeNull();
        result.CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task ResolveAsync_SuperAdmin_NoTenant_FallsBackToSystem()
    {
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns((long?)null);

        var result = await BuildSut().ResolveAsync("Users Report", TestContext.Current.CancellationToken);

        result.CompanyName.Should().Be("System");
        await _companyRepo.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveAsync_ContextNotSet_FallsBackToSystem()
    {
        _tenantContext.IsSet.Returns(false);

        var result = await BuildSut().ResolveAsync("Audit Logs Report", TestContext.Current.CancellationToken);

        result.CompanyName.Should().Be("System");
    }

    [Fact]
    public async Task ResolveAsync_CompanyNotFound_FallsBackToSystem()
    {
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(99L);
        _companyRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Company?)null);

        var result = await BuildSut().ResolveAsync("Test Report", TestContext.Current.CancellationToken);

        result.CompanyName.Should().Be("System");
    }

    [Fact]
    public async Task ResolveAsync_WithTenantCompany_PassesThroughAddress()
    {
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(42L);
        var company = new Company { Id = 42, Name = "Acme Corp", Address = "123 Main St" };
        _companyRepo.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(company);

        var result = await BuildSut().ResolveAsync("Purchase Order", TestContext.Current.CancellationToken);

        result.Address.Should().Be("123 Main St");
    }

    [Fact]
    public async Task ResolveAsync_WithTenantCompany_NoAddress_ReturnsNull()
    {
        _tenantContext.IsSet.Returns(true);
        _tenantContext.CompanyId.Returns(42L);
        var company = new Company { Id = 42, Name = "Acme Corp", Address = null };
        _companyRepo.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(company);

        var result = await BuildSut().ResolveAsync("Purchase Order", TestContext.Current.CancellationToken);

        result.Address.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_GeneratedAt_IsUtcNow()
    {
        _tenantContext.IsSet.Returns(false);
        var before = DateTime.UtcNow;

        var result = await BuildSut().ResolveAsync("Report", TestContext.Current.CancellationToken);

        result.GeneratedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        result.GeneratedAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}
