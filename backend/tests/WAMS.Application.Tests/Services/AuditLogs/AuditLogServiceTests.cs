namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Services.AuditLogs;
using WAMS.Domain.Entities.AuditLogs;
using Xunit;

public class AuditLogServiceTests
{
    private readonly IAuditLogRepository _auditLogRepo = Substitute.For<IAuditLogRepository>();
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        _sut = new AuditLogService(_auditLogRepo);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedPaginatedAuditLogs()
    {
        var query = new AuditLogQuery
        {
            Page = 2,
            Limit = 2,
            TableName = "users",
            Action = "UPDATE"
        };
        var createdAt = new DateTime(2026, 4, 10, 8, 30, 0, DateTimeKind.Utc);
        var auditLog = BuildAuditLog(
            id: 11,
            action: "UPDATE",
            tableName: "users",
            recordId: 7,
            oldValues: """{"fullname":"Old Name"}""",
            newValues: """{"fullname":"New Name"}""",
            createdAt: createdAt);
        _auditLogRepo.GetAllAsync(query, Arg.Any<CancellationToken>())
            .Returns((new List<AuditLog> { auditLog }, 5));

        var result = await _sut.GetAllAsync(query, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.Meta.Should().BeEquivalentTo(new PaginationMeta(2, 2, 5, 3));
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be(11);
        result.Data[0].Action.Should().Be("UPDATE");
        result.Data[0].TableName.Should().Be("users");
        result.Data[0].RecordId.Should().Be(7);
        result.Data[0].UserEmail.Should().Be("alice@example.com");
        result.Data[0].OldValues?.GetProperty("fullname").GetString().Should().Be("Old Name");
        result.Data[0].NewValues?.GetProperty("fullname").GetString().Should().Be("New Name");
        result.Data[0].CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingAuditLog_ReturnsNull()
    {
        _auditLogRepo.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((AuditLog?)null);

        var result = await _sut.GetByIdAsync(99, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingAuditLog_ReturnsMappedResponse()
    {
        _auditLogRepo.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(BuildAuditLog(
                id: 5,
                action: "CREATE",
                tableName: "companies",
                recordId: 3,
                oldValues: null,
                newValues: """{"name":"Acme Corp"}"""));

        var result = await _sut.GetByIdAsync(5, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Action.Should().Be("CREATE");
        result.TableName.Should().Be("companies");
        result.RecordId.Should().Be(3);
        result.OldValues.Should().BeNull();
        result.NewValues?.GetProperty("name").GetString().Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetRecordHistoryAsync_ReturnsMappedPaginatedAuditLogs()
    {
        var query = new DataTableQuery { Page = 1, Limit = 10 };
        _auditLogRepo.GetRecordHistoryAsync("users", 7, query, Arg.Any<CancellationToken>())
            .Returns((
                new List<AuditLog>
                {
                    BuildAuditLog(id: 20, action: "CREATE", tableName: "users", recordId: 7, newValues: """{"fullname":"Alice"}"""),
                    BuildAuditLog(id: 21, action: "UPDATE", tableName: "users", recordId: 7, oldValues: """{"fullname":"Alice"}""", newValues: """{"fullname":"Alice Doe"}""")
                },
                2));

        var result = await _sut.GetRecordHistoryAsync("users", 7, query, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.Meta.Should().BeEquivalentTo(new PaginationMeta(1, 10, 2, 1));
        result.Data.Select(x => x.Action).Should().ContainInOrder("CREATE", "UPDATE");
        result.Data[1].OldValues?.GetProperty("fullname").GetString().Should().Be("Alice");
        result.Data[1].NewValues?.GetProperty("fullname").GetString().Should().Be("Alice Doe");
    }

    [Fact]
    public async Task GetRecordHistorySlimAsync_ReturnsMappedSlimResponse()
    {
        var query = new DataTableQuery { Page = 1, Limit = 10 };
        var createdAt = new DateTime(2026, 6, 23, 10, 0, 0, DateTimeKind.Utc);
        _auditLogRepo.GetRecordHistoryAsync("budget_plans", 10, query, Arg.Any<CancellationToken>())
            .Returns((
                new List<AuditLog>
                {
                    BuildAuditLog(id: 50, action: "CREATE", tableName: "budget_plans", recordId: 10,
                        newValues: """{"status":"Draft"}""", createdAt: createdAt)
                },
                1));

        var result = await _sut.GetRecordHistorySlimAsync("budget_plans", 10, query, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.Meta.Should().BeEquivalentTo(new PaginationMeta(1, 10, 1, 1));
        result.Data.Should().ContainSingle();
        var entry = result.Data[0];
        entry.Id.Should().Be(50);
        entry.Action.Should().Be("CREATE");
        entry.UserId.Should().Be(1);
        entry.UserEmail.Should().Be("alice@example.com");
        entry.UserFullname.Should().Be("Alice");
        entry.NewValues?.GetProperty("status").GetString().Should().Be("Draft");
        entry.OldValues.Should().BeNull();
        entry.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public async Task GetRecordHistorySlimAsync_EmptyResult_ReturnsEmptyPaginatedList()
    {
        var query = new DataTableQuery { Page = 1, Limit = 10 };
        _auditLogRepo.GetRecordHistoryAsync("budget_plans", 999, query, Arg.Any<CancellationToken>())
            .Returns((new List<AuditLog>(), 0));

        var result = await _sut.GetRecordHistorySlimAsync("budget_plans", 999, query, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
        result.Meta.Total.Should().Be(0);
    }

    private static AuditLog BuildAuditLog(
        long id,
        string action,
        string tableName,
        long? recordId,
        string? oldValues = null,
        string? newValues = null,
        DateTime? createdAt = null)
    {
        return new AuditLog
        {
            Id = id,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            RecordKey = recordId?.ToString(),
            UserId = 1,
            UserEmail = "alice@example.com",
            UserFullname = "Alice",
            CompanyId = 2,
            OldValues = oldValues,
            NewValues = newValues,
            RequestId = "req-123",
            RequestPath = "/api/v1/users/7",
            HttpMethod = "PUT",
            IpAddress = "127.0.0.1",
            UserAgent = "Apidog",
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }
}
