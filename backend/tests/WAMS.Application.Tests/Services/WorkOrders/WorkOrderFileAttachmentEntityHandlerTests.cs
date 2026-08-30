namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Domain.Exceptions;
using WAMS.Infrastructure.Services.WorkOrders;
using Xunit;

/// <summary>
/// FilesController only requires authentication, so every access rule for work order attachments
/// lives in this handler -it gates listing and downloading on warehouse access.
/// </summary>
public class WorkOrderFileAttachmentEntityHandlerTests
{
    private readonly IWorkOrderRepository _woRepo = Substitute.For<IWorkOrderRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly WorkOrderFileAttachmentEntityHandler _sut;

    private const long UserId = 7;
    private const long WorkOrderId = 42;
    private const long WarehouseId = 99;

    public WorkOrderFileAttachmentEntityHandlerTests()
        => _sut = new WorkOrderFileAttachmentEntityHandler(_woRepo, _userRepo);

    private void GivenWorkOrder(bool canBeEdited = true, long createdBy = 5)
        => _woRepo.GetForAttachmentAsync(WorkOrderId, Arg.Any<CancellationToken>())
            .Returns(new WorkOrderAttachmentContext(WorkOrderId, 1, createdBy, canBeEdited, WarehouseId));

    private void GivenWarehouseAccess(bool hasAccess)
        => _userRepo.CheckWarehouseAccessAsync(UserId, WarehouseId, Arg.Any<CancellationToken>())
            .Returns((true, hasAccess));

    [Fact]
    public async Task ResolveAsync_UserLacksWarehouseAccess_ThrowsForbidden()
    {
        GivenWorkOrder();
        GivenWarehouseAccess(false);

        var act = () => _sut.ResolveAsync(UserId, WorkOrderId, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task ResolveAsync_UserHasWarehouseAccess_ReturnsContext()
    {
        GivenWorkOrder(canBeEdited: true, createdBy: 5);
        GivenWarehouseAccess(true);

        var result = await _sut.ResolveAsync(UserId, WorkOrderId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.EntityType.Should().Be("work-orders");
        result.EntityId.Should().Be(WorkOrderId);
        result.OwnerUserId.Should().Be(5);
        result.CanModify.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_WorkOrderNotFound_ReturnsNullWithoutCheckingAccess()
    {
        _woRepo.GetForAttachmentAsync(WorkOrderId, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _sut.ResolveAsync(UserId, WorkOrderId, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        await _userRepo.DidNotReceive().CheckWarehouseAccessAsync(
            Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    // A non-editable work order is still readable - CanModify gates upload/delete downstream,
    // it must not turn into a 403 at resolve time or listing attachments would break.
    [Fact]
    public async Task ResolveAsync_WorkOrderNotEditable_StillResolvesWithCanModifyFalse()
    {
        GivenWorkOrder(canBeEdited: false);
        GivenWarehouseAccess(true);

        var result = await _sut.ResolveAsync(UserId, WorkOrderId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.CanModify.Should().BeFalse();
    }
}
