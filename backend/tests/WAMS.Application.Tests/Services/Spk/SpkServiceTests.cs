namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Spk;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Spk;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.Spk;
using WAMS.Domain.Entities.Spk;
using WAMS.Domain.Exceptions;
using Xunit;

public class SpkServiceTests
{
    private readonly ISpkShadowRepository _spkRepo = Substitute.For<ISpkShadowRepository>();
    private readonly IWarehouseShadowRepository _warehouseRepo = Substitute.For<IWarehouseShadowRepository>();
    private readonly IWarehouseContext _warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly SpkService _sut;

    private static readonly SpkQuery DefaultQuery = new() { Page = 1, Limit = 20 };

    public SpkServiceTests()
    {
        _sut = new SpkService(_spkRepo, _warehouseRepo, _warehouseContext, _userRepo, _rbacService);
    }

    [Fact]
    public async Task GetAllAsync_WithWarehouseHeader_ScopesToSingleWarehouseCode()
    {
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);
        _userRepo.CheckWarehouseAccessAsync(1, 7, Arg.Any<CancellationToken>()).Returns((true, true));
        _warehouseRepo.GetCodesByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 7L })), Arg.Any<CancellationToken>())
            .Returns(["WH-07"]);
        _spkRepo.GetAllAsync(DefaultQuery, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns((new List<SpkShadow>(), 0));

        await _sut.GetAllAsync(DefaultQuery, 1, TestContext.Current.CancellationToken);

        await _spkRepo.Received(1).GetAllAsync(
            DefaultQuery,
            Arg.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "WH-07" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WithWarehouseHeaderButNoAccess_ThrowsForbidden()
    {
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);
        _userRepo.CheckWarehouseAccessAsync(1, 7, Arg.Any<CancellationToken>()).Returns((true, false));

        var act = () => _sut.GetAllAsync(DefaultQuery, 1);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetAllAsync_WithGlobalAccess_PassesNullWhsCodes()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _spkRepo.GetAllAsync(DefaultQuery, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns((new List<SpkShadow>(), 0));

        await _sut.GetAllAsync(DefaultQuery, 1, TestContext.Current.CancellationToken);

        await _spkRepo.Received(1).GetAllAsync(DefaultQuery, null, Arg.Any<CancellationToken>());
        await _userRepo.DidNotReceive().GetUserWarehouseIdsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WithRestrictedUser_ScopesToAssignedWarehouseCodes()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(1, Arg.Any<CancellationToken>()).Returns([3L, 9L]);
        _warehouseRepo.GetCodesByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 3L, 9L })), Arg.Any<CancellationToken>())
            .Returns(["WH-03", "WH-09"]);
        _spkRepo.GetAllAsync(DefaultQuery, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns((new List<SpkShadow>(), 0));

        await _sut.GetAllAsync(DefaultQuery, 1, TestContext.Current.CancellationToken);

        await _spkRepo.Received(1).GetAllAsync(
            DefaultQuery,
            Arg.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "WH-03", "WH-09" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_OutOfScope_ThrowsNotFoundException()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(1, Arg.Any<CancellationToken>()).Returns([3L]);
        _warehouseRepo.GetCodesByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 3L })), Arg.Any<CancellationToken>())
            .Returns(["WH-03"]);
        _spkRepo.GetByIdAsync(42, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns((SpkShadow?)null);

        var act = () => _sut.GetByIdAsync(42, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
