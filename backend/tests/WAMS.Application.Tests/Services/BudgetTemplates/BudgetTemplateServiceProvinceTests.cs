namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
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

/// <summary>
/// Tests that BudgetTemplateService validates a request's ProvinceId directly against
/// IProvinceRepository, instead of resolving free-text location on the server.
/// </summary>
public class BudgetTemplateServiceProvinceTests
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

    public BudgetTemplateServiceProvinceTests()
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
    public async Task CreateAsync_KnownActiveProvinceId_SetsProvinceIdOnEntity()
    {
        var activityType = new ActivityType { Id = 2, Code = "AT", Name = "Activity", IsActive = true };
        _activityTypeRepo.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(activityType);
        _itemRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ItemShadow>());
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _budgetTemplateRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(BuildTemplateWithNav(BudgetTemplateStatus.Draft, provinceId: 5));
        _provinceRepo.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new Province { Id = 5, Name = "Jawa Timur", IsActive = true });

        var request = new CreateBudgetTemplateRequest(ProvinceId: 5, Items: []);

        await _sut.CreateAsync(userId: 99, request, TestContext.Current.CancellationToken);

        await _budgetTemplateRepo.Received(1).CreateAsync(
            Arg.Is<BudgetTemplate>(t => t.ProvinceId == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_UnknownProvinceId_ThrowsNotFoundException()
    {
        var activityType = new ActivityType { Id = 2, Code = "AT", Name = "Activity", IsActive = true };
        _activityTypeRepo.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(activityType);
        _itemRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ItemShadow>());
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _provinceRepo.GetByIdAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();

        var request = new CreateBudgetTemplateRequest(ProvinceId: 999, Items: []);

        var act = async () => await _sut.CreateAsync(userId: 99, request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task CreateAsync_InactiveProvinceId_ThrowsValidationException()
    {
        var activityType = new ActivityType { Id = 2, Code = "AT", Name = "Activity", IsActive = true };
        _activityTypeRepo.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(activityType);
        _itemRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ItemShadow>());
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _provinceRepo.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new Province { Id = 5, Name = "Jawa Timur", IsActive = false });

        var request = new CreateBudgetTemplateRequest(ProvinceId: 5, Items: []);

        var act = async () => await _sut.CreateAsync(userId: 99, request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Jawa Timur*");
    }

    [Fact]
    public async Task CreateAsync_NullProvinceId_DoesNotCallProvinceRepo_AndProvinceIdIsNull()
    {
        var activityType = new ActivityType { Id = 2, Code = "AT", Name = "Activity", IsActive = true };
        _activityTypeRepo.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(activityType);
        _itemRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ItemShadow>());
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _budgetTemplateRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(BuildTemplateWithNav(BudgetTemplateStatus.Draft, provinceId: null));

        var request = new CreateBudgetTemplateRequest(ProvinceId: null, Items: []);

        await _sut.CreateAsync(userId: 99, request, TestContext.Current.CancellationToken);

        await _provinceRepo.DidNotReceiveWithAnyArgs().GetByIdAsync(default, TestContext.Current.CancellationToken);

        await _budgetTemplateRepo.Received(1).CreateAsync(
            Arg.Is<BudgetTemplate>(t => t.ProvinceId == null),
            Arg.Any<CancellationToken>());
    }

    private static BudgetTemplate BuildTemplateWithNav(BudgetTemplateStatus status, long? provinceId) => new()
    {
        Id = 1,
        CompanyId = 1,
        Code = "T.260400001",
        Status = status,
        CreatedByUserId = 99,
        CreatedBy = TestBuilders.ActiveUser(id: 99),
        ProvinceId = provinceId,
        Province = provinceId.HasValue
            ? new Province { Id = provinceId.Value, Name = "Jawa Timur", IsActive = true }
            : null,
        SubmittedBy = null,
        Items = []
    };
}
