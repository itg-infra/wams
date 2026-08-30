namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.BudgetTemplates;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Application.Interfaces.BudgetTemplates;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Items;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Services.BudgetTemplates;
using WAMS.Application.Tests.Helpers;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using Xunit;

public class BudgetTemplateServiceTests
{
    private readonly IBudgetTemplateRepository _budgetTemplateRepo = Substitute.For<IBudgetTemplateRepository>();
    private readonly IActivityTypeRepository _activityTypeRepo = Substitute.For<IActivityTypeRepository>();
    private readonly IItemShadowRepository _itemRepo = Substitute.For<IItemShadowRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IProvinceRepository _provinceRepo = Substitute.For<IProvinceRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ICodeCounterRepository _codeCounterRepo = Substitute.For<ICodeCounterRepository>();
    private readonly BudgetTemplateService _sut;

    public BudgetTemplateServiceTests()
    {
        _sut = new BudgetTemplateService(
            _budgetTemplateRepo,
            _activityTypeRepo,
            _itemRepo,
            _uow,
            _provinceRepo,
            _rbacService,
            _userService,
            _codeCounterRepo);
    }

    [Fact]
    public async Task SubmitAsync_Transitions_DraftToSubmitted()
    {
        var template = BuildTemplate(BudgetTemplateStatus.Draft);
        _budgetTemplateRepo.GetTrackedAsync(1, Arg.Any<CancellationToken>()).Returns(template);

        await _sut.SubmitAsync(1, 99, TestContext.Current.CancellationToken);

        template.Status.Should().Be(BudgetTemplateStatus.Submitted);
        template.SubmittedByUserId.Should().Be(99);
        template.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitAsync_Throws_WhenAlreadySubmitted()
    {
        var template = BuildTemplate(BudgetTemplateStatus.Submitted);
        _budgetTemplateRepo.GetTrackedAsync(1, Arg.Any<CancellationToken>()).Returns(template);

        var act = async () => await _sut.SubmitAsync(1, 99);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenSubmitted()
    {
        var template = BuildTemplate(BudgetTemplateStatus.Submitted);
        _budgetTemplateRepo.GetTrackedAsync(1, Arg.Any<CancellationToken>()).Returns(template);

        var act = async () => await _sut.DeleteAsync(1);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTemplates_WithProvinceFields()
    {
        var templates = new List<BudgetTemplate> { BuildTemplateWithNav(BudgetTemplateStatus.Draft) };
        _budgetTemplateRepo.GetAllAsync(null, Arg.Any<BudgetTemplateQuery>(), Arg.Any<List<long>?>(), Arg.Any<CancellationToken>())
            .Returns((templates, 1));
        // Simulate global user so no province filter is applied
        _rbacService.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(true);

        var (items, total) = await _sut.GetAllAsync(null, new BudgetTemplateQuery { Page = 1, Limit = 20 }, userId: 99, ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        total.Should().Be(1);
        items[0].ProvinceId.Should().Be(7);
        items[0].ProvinceName.Should().Be("Lampung");
        await _budgetTemplateRepo.Received(1).GetAllAsync(null, Arg.Any<BudgetTemplateQuery>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProvinceFields()
    {
        var template = BuildTemplateWithNav(BudgetTemplateStatus.Draft);
        _budgetTemplateRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(template);

        var result = await _sut.GetByIdAsync(1, userId: 99, ct: TestContext.Current.CancellationToken);

        result.ProvinceId.Should().Be(7);
        result.ProvinceName.Should().Be("Lampung");
    }

    [Fact]
    public async Task CreateAsync_SetsProvinceIdFromRequest()
    {
        var activityType = new ActivityType { Id = 2, Code = "AT", Name = "Activity", IsActive = true };
        _activityTypeRepo.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(activityType);
        _itemRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>()).Returns(new List<ItemShadow>());
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _budgetTemplateRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(BuildTemplateWithNav(BudgetTemplateStatus.Draft));
        _provinceRepo.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new Province { Id = 7, Name = "Lampung", IsActive = true });

        var request = new CreateBudgetTemplateRequest(ProvinceId: 7, Items: []);
        var result = await _sut.CreateAsync(userId: 99, request, TestContext.Current.CancellationToken);

        await _budgetTemplateRepo.Received(1).CreateAsync(
            Arg.Is<BudgetTemplate>(t => t.ProvinceId == 7 && t.Code.StartsWith("BT-")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PersistsPerItemActivityType()
    {
        _activityTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ActivityType>
            {
                new() { Id = 3, Code = "K.BONGKAR", Name = "Bongkar", IsActive = true },
                new() { Id = 4, Code = "K.MUAT", Name = "Muat", IsActive = true },
            });
        _itemRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ItemShadow>
            {
                new() { Id = 10, ItemCode = "I10", ItemName = "Item 10", AcctCode = "A", AcctName = "AN" },
                new() { Id = 11, ItemCode = "I11", ItemName = "Item 11", AcctCode = "A", AcctName = "AN" },
            });
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _budgetTemplateRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(BuildTemplateWithNav(BudgetTemplateStatus.Draft));

        var request = new CreateBudgetTemplateRequest(
            ProvinceId: null,
            Items: [new(ItemShadowId: 10, ActivityTypeId: 3), new(ItemShadowId: 11, ActivityTypeId: 4)]);

        await _sut.CreateAsync(userId: 99, request, TestContext.Current.CancellationToken);

        await _budgetTemplateRepo.Received(1).CreateAsync(
            Arg.Is<BudgetTemplate>(t =>
                t.Items.Count == 2 &&
                t.Items.Any(i => i.ItemShadowId == 10 && i.ActivityTypeId == 3) &&
                t.Items.Any(i => i.ItemShadowId == 11 && i.ActivityTypeId == 4)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenItemActivityTypeIdIsMissing()
    {
        _itemRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ItemShadow>
            {
                new() { Id = 10, ItemCode = "I10", ItemName = "Item 10", AcctCode = "A", AcctName = "AN" },
            });

        var request = new CreateBudgetTemplateRequest(
            ProvinceId: null,
            Items: [new(ItemShadowId: 10, ActivityTypeId: 0)]);

        var act = async () => await _sut.CreateAsync(userId: 99, request, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
        await _activityTypeRepo.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>());
    }

    private static BudgetTemplate BuildTemplate(BudgetTemplateStatus status) => new()
    {
        Id = 1,
        CompanyId = 1,
        Code = "T.260400001",
        Status = status,
        CreatedByUserId = 99,
        CreatedBy = TestBuilders.ActiveUser(id: 99),
        ProvinceId = 7,
        Province = new Province { Id = 7, Name = "Lampung", IsActive = true },
        Items = []
    };

    private static BudgetTemplate BuildTemplateWithNav(BudgetTemplateStatus status) => new()
    {
        Id = 1,
        CompanyId = 1,
        Code = "T.260400001",
        Status = status,
        CreatedByUserId = 99,
        CreatedBy = TestBuilders.ActiveUser(id: 99),
        ProvinceId = 7,
        Province = new Province { Id = 7, Name = "Lampung", IsActive = true },
        SubmittedBy = null,
        Items = []
    };
}
