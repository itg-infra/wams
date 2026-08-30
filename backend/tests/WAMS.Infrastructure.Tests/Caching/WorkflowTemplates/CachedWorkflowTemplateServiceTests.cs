namespace WAMS.Infrastructure.Tests.Caching;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.WorkflowTemplates;
using WAMS.Application.Interfaces.WorkflowTemplates;
using WAMS.Infrastructure.Caching.WorkflowTemplates;
using Xunit;

public sealed class CachedWorkflowTemplateServiceTests : IDisposable
{
    private readonly CacheTestFixture _fx = new();
    private readonly IWorkflowTemplateService _inner = Substitute.For<IWorkflowTemplateService>();
    private readonly CachedWorkflowTemplateService _sut;

    private static WorkflowTemplateResponse MakeTemplate(string name)
        => new(1, "PO", name, 10L, true, [], DateTime.UtcNow, null);

    public CachedWorkflowTemplateServiceTests()
    {
        _sut = new CachedWorkflowTemplateService(_inner, _fx.Cache, _fx.Options);
    }

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task GetByIdAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Template A"));

        await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);
        await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);

        await _inner.Received(1).GetByIdAsync(1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_InvalidatesCompanyCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Template A"));
        _inner.CreateAsync(10, Arg.Any<CreateWorkflowTemplateRequest>(), Arg.Any<CancellationToken>())
            .Returns(MakeTemplate("Template B"));
        await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);

        await _sut.CreateAsync(companyId: 10, new CreateWorkflowTemplateRequest("PO", "Template B", true, []), TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Template B"));
        var result = await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Template B", "cache cleared after Create");
        await _inner.Received(2).GetByIdAsync(1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCompanyCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Template A"));
        _inner.UpdateAsync(1, 10, Arg.Any<UpdateWorkflowTemplateRequest>(), Arg.Any<CancellationToken>())
            .Returns(MakeTemplate("Template Updated"));
        await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);

        await _sut.UpdateAsync(1, companyId: 10, new UpdateWorkflowTemplateRequest("Template Updated", null, null), TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Template Updated"));
        var result = await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Template Updated", "cache cleared after Update");
        await _inner.Received(2).GetByIdAsync(1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateAsync_InvalidatesCompanyCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Template A"));
        await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);

        await _sut.ActivateAsync(1, companyId: 10, TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Template Activated"));
        var result = await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Template Activated", "cache cleared after Activate");
        await _inner.Received(2).GetByIdAsync(1, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesOnlyOwningCompanyCache()
    {
        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Template A"));
        _inner.GetByIdAsync(2, 20, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Company B Template"));
        await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);
        await _sut.GetByIdAsync(2, companyId: 20, TestContext.Current.CancellationToken);

        await _sut.DeleteAsync(1, companyId: 10, TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, 10, Arg.Any<CancellationToken>()).Returns(MakeTemplate("New Template"));
        _inner.GetByIdAsync(2, 20, Arg.Any<CancellationToken>()).Returns(MakeTemplate("Should Not Change"));

        var company10Result = await _sut.GetByIdAsync(1, companyId: 10, TestContext.Current.CancellationToken);
        var company20Result = await _sut.GetByIdAsync(2, companyId: 20, TestContext.Current.CancellationToken);

        company10Result.Name.Should().Be("New Template", "company 10 cache was cleared");
        company20Result.Name.Should().Be("Company B Template", "company 20 cache was unaffected");
    }
}
