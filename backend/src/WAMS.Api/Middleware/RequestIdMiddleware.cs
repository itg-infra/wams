namespace WAMS.Api.Middleware;

internal static class RequestIdConstants
{
    public const string HeaderName = "X-Request-ID";
    public const string HttpContextItemKey = "RequestId";
}

public class RequestIdMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.TraceIdentifier;

        if (TryGetRequestIdFromHeader(context, out var headerRequestId))
        {
            requestId = headerRequestId;
        }

        context.Items[RequestIdConstants.HttpContextItemKey] = requestId;
        context.Response.Headers[RequestIdConstants.HeaderName] = requestId;

        await _next(context);
    }

    private static bool TryGetRequestIdFromHeader(HttpContext context, out string? requestId)
    {
        requestId = null;

        if (context.Request.Headers.TryGetValue(RequestIdConstants.HeaderName, out var headerValues))
        {
            requestId = headerValues.FirstOrDefault()?.Trim();

            if (!string.IsNullOrEmpty(requestId))
            {
                return true;
            }
        }

        return false;
    }
}
