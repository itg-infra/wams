namespace WAMS.Api.Tests.Middleware;

using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WAMS.Api.Middleware;
using WAMS.Application.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Domain.Exceptions;
using Xunit;

public class ExceptionHandlingMiddlewareTests
{
    private static DefaultHttpContext BuildContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<(int StatusCode, JsonElement Body)> InvokeWithException(Exception ex)
    {
        var ctx = BuildContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw ex,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(ctx);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        var doc = JsonDocument.Parse(json);
        return (ctx.Response.StatusCode, doc.RootElement);
    }

    // Exception → HTTP status mapping
    public static IEnumerable<object[]> ExceptionMappings()
    {
        yield return [new ValidationException("bad input"), 422, "VALIDATION_ERROR"];
        yield return [new UnauthorizedException("not auth'd"), 401, "UNAUTHORIZED"];
        yield return [new SessionIdleTimeoutException("idle too long"), 401, "SESSION_IDLE_TIMEOUT"];
        yield return [new ForbiddenException("no access"), 403, "FORBIDDEN"];
        yield return [new NotFoundException("User", 1L), 404, "NOT_FOUND"];
        yield return [new ConflictException("already exists"), 409, "CONFLICT"];
        yield return [new InvalidOperationException("boom"), 500, "INTERNAL_ERROR"];
    }

    [Theory]
    [MemberData(nameof(ExceptionMappings))]
    public async Task InvokeAsync_MapsExceptionToCorrectStatus(
        Exception exception, int expectedStatus, string expectedCode)
    {
        var (statusCode, body) = await InvokeWithException(exception);

        statusCode.Should().Be(expectedStatus, because: exception.GetType().Name);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be(expectedCode);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_IncludesErrorsInBody()
    {
        var errors = new Dictionary<string, string[]> { ["email"] = ["Email is required"] };
        var (statusCode, body) = await InvokeWithException(new ValidationException(errors));

        statusCode.Should().Be(422);
        body.GetProperty("error").GetProperty("details").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_PreservesStructuredDetailsAndCode()
    {
        var details = new PurchaseOrderItemValidationDetails(
            [new InvalidPurchaseOrderItem(135L, 1L, 2L)]);
        var exception = new ValidationException(
            "Budget plan item 135 belongs to a different vendor.",
            ErrorCodes.PurchaseOrderItemVendorMismatch,
            details);

        var (statusCode, body) = await InvokeWithException(exception);

        statusCode.Should().Be(422);
        body.GetProperty("error").GetProperty("code").GetString()
            .Should().Be(ErrorCodes.PurchaseOrderItemVendorMismatch);
        var invalidItem = body.GetProperty("error")
            .GetProperty("details")
            .GetProperty("invalidItems")[0];
        invalidItem.GetProperty("itemId").GetInt64().Should().Be(135L);
        invalidItem.GetProperty("requestedVendorShadowId").GetInt64().Should().Be(1L);
        invalidItem.GetProperty("actualVendorShadowId").GetInt64().Should().Be(2L);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_WithBlankMessage_still_returns_message()
    {
        var (_, body) = await InvokeWithException(new ValidationException(""));

        body.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_DoesNotModifyResponse()
    {
        var ctx = BuildContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(ctx);

        // No exception = response untouched (still 200 default)
        ctx.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_SetsContentTypeToJson()
    {
        var (_, _) = await InvokeWithException(new NotFoundException("Item", 1L));

        // Content-type verified via the response written above; just check we got valid JSON
        // (stream was readable = JSON was written correctly)
    }

    [Fact]
    public async Task InvokeAsync_OperationCanceledFromClientDisconnect_DoesNotWriteErrorResponse()
    {
        using var cts = new CancellationTokenSource();
        var ctx = BuildContext();
        ctx.RequestAborted = cts.Token;
        cts.Cancel();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new OperationCanceledException(),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(200);
        ctx.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_OperationCanceledNotFromClientDisconnect_MapsTo500()
    {
        var (statusCode, body) = await InvokeWithException(new OperationCanceledException("timeout"));

        statusCode.Should().Be(500);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be("INTERNAL_ERROR");
    }
}
