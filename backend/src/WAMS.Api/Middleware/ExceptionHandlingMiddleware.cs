namespace WAMS.Api.Middleware;

using System.Text.Json;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Domain.Exceptions;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected/navigated away mid-request
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();

        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Exception after response started (streaming) for request {RequestId}", requestId);
            return;
        }

        var (statusCode, code, message, details) = exception switch
        {
            ValidationException ve => (422, ve.Code, ve.Message, ve.Details ?? ve.Errors),
            SessionIdleTimeoutException site => (401, ErrorCodes.SessionIdleTimeout, site.Message, null),
            UnauthorizedException ue => (401, ErrorCodes.Unauthorized, ue.Message, null),
            ForbiddenException fe => (403, ErrorCodes.Forbidden, fe.Message, null),
            NotFoundException nfe => (404, ErrorCodes.NotFound, nfe.Message, null),
            ConflictException ce => (409, ErrorCodes.Conflict, ce.Message, null),
            _ => (500, ErrorCodes.InternalError, ErrorCodes.InternalErrorMessage, null)
        };

        switch (statusCode)
        {
            case 500:
                _logger.LogError(exception, "Unhandled exception for request {RequestId}", requestId);
                break;
            case >= 400 and < 500:
                _logger.LogWarning(
                    "Client error {StatusCode} {ExceptionType}: {Message} for request {RequestId}",
                    statusCode,
                    exception.GetType().Name,
                    exception.Message,
                    requestId
                );
                break;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var safeMessage = string.IsNullOrWhiteSpace(message) ? DefaultMessage(code) : message;
        var safeDetails = details ?? new Dictionary<string, string[]>();
        var response = new ErrorResponse(false, safeMessage, new ErrorDetail(code, safeDetails), requestId);

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static string DefaultMessage(string code) => code switch
    {
        ErrorCodes.ValidationError => "One or more validation errors occurred.",
        ErrorCodes.Unauthorized => "Authentication is required.",
        ErrorCodes.Forbidden => "You do not have permission to perform this action.",
        ErrorCodes.NotFound => "The requested resource was not found.",
        ErrorCodes.Conflict => "The request conflicts with the current resource state.",
        _ => "An unexpected error occurred."
    };
}
