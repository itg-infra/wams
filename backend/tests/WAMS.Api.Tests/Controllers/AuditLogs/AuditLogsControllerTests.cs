namespace WAMS.Api.Tests.Controllers;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using WAMS.Api.Controllers.AuditLogs;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Domain.Exceptions;
using Xunit;
using System.IO;

public class AuditLogsControllerTests
{
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly IExportService _exportService = Substitute.For<IExportService>();
    private readonly IOptions<ExportOptions> _exportOptions = Options.Create(new ExportOptions { MaxRows = 50_000 });
    private readonly AuditLogsController _sut;

    public AuditLogsControllerTests()
    {
        _sut = new AuditLogsController(_auditLogService, _exportService, _exportOptions);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _sut.ControllerContext.HttpContext.Items["RequestId"] = "req-test";
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedResponse()
    {
        var query = new AuditLogQuery { Page = 1, Limit = 20, TableName = "users" };
        var response = new PaginatedResponse<AuditLogResponse>(
            true,
            [BuildResponse(id: 1, action: "CREATE", tableName: "users", recordId: 9)],
            new PaginationMeta(1, 20, 1, 1));
        _auditLogService.GetAllAsync(query, Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.GetAll(query, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<PaginatedResponse<AuditLogResponse>>().Subject;
        payload.Success.Should().BeTrue();
        payload.Meta.Should().BeEquivalentTo(new PaginationMeta(1, 20, 1, 1));
        payload.RequestId.Should().Be("req-test");
        payload.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task GetById_WithExistingAuditLog_ReturnsOkWithResponse()
    {
        _auditLogService.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(id: 7, action: "UPDATE", tableName: "users", recordId: 2));

        var result = await _sut.GetById(7, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ApiResponse<AuditLogResponse>>().Subject;
        payload.Success.Should().BeTrue();
        payload.RequestId.Should().Be("req-test");
        payload.Data!.Id.Should().Be(7);
    }

    [Fact]
    public async Task GetById_WithMissingAuditLog_ThrowsNotFoundException()
    {
        _auditLogService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((AuditLogResponse?)null);

        var act = () => _sut.GetById(99, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*99*");
    }

    [Fact]
    public async Task GetRecordHistory_ReturnsOkWithPaginatedResponse()
    {
        var query = new DataTableQuery { Page = 1, Limit = 10 };
        var response = new PaginatedResponse<AuditLogResponse>(
            true,
            [
                BuildResponse(id: 1, action: "CREATE", tableName: "users", recordId: 8),
                BuildResponse(id: 2, action: "UPDATE", tableName: "users", recordId: 8)
            ],
            new PaginationMeta(1, 10, 2, 1));
        _auditLogService.GetRecordHistoryAsync("users", 8, query, Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _sut.GetRecordHistory("users", 8, query, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<PaginatedResponse<AuditLogResponse>>().Subject;
        payload.RequestId.Should().Be("req-test");
        payload.Data.Select(x => x.Action).Should().ContainInOrder("CREATE", "UPDATE");
    }

    [Fact]
    public async Task Export_CappsQueryLimitToMaxRows()
    {
        var opts = Options.Create(new ExportOptions { MaxRows = 123 });
        var controller = new AuditLogsController(_auditLogService, _exportService, opts);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.ControllerContext.HttpContext.Items["RequestId"] = "req-test";

        _auditLogService.StreamAllAsync(Arg.Any<AuditLogQuery>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(EmptyAsync<AuditLogResponse>());
        _exportService.GetContentType(Arg.Any<ExportFormat>()).Returns("application/octet-stream");
        _exportService.GetFileExtension(Arg.Any<ExportFormat>()).Returns("xlsx");

        var result = await controller.Export(new AuditLogQuery(), ExportFormat.Xlsx, TestContext.Current.CancellationToken);

        result.Should().BeOfType<EmptyResult>();
        _auditLogService.Received(1).StreamAllAsync(
            Arg.Any<AuditLogQuery>(),
            123,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Export_CallsStreamExportAsyncAndReturnsEmptyResult()
    {
        _auditLogService.StreamAllAsync(Arg.Any<AuditLogQuery>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(EmptyAsync<AuditLogResponse>());
        _exportService.GetContentType(Arg.Any<ExportFormat>()).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        _exportService.GetFileExtension(Arg.Any<ExportFormat>()).Returns("xlsx");

        var result = await _sut.Export(new AuditLogQuery(), ExportFormat.Xlsx, TestContext.Current.CancellationToken);

        result.Should().BeOfType<EmptyResult>();
        await _exportService.Received(1).StreamExportAsync(
            Arg.Any<Stream>(),
            ExportFormat.Xlsx,
            Arg.Any<IReadOnlyList<ExportColumnDefinition<AuditLogResponse>>>(),
            Arg.Any<IAsyncEnumerable<AuditLogResponse>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<T> EmptyAsync<T>()
    {
        yield break;
    }

    private static AuditLogResponse BuildResponse(long id, string action, string tableName, long? recordId)
    {
        return new AuditLogResponse(
            id,
            action,
            tableName,
            recordId,
            recordId?.ToString(),
            1,
            "alice@example.com",
            "Alice",
            2,
            null,
            null,
            "req-123",
            "/api/v1/users/8",
            "GET",
            "127.0.0.1",
            "Apidog",
            DateTime.UtcNow);
    }
}
