namespace WAMS.Application.DTOs.Common;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message,
    string? RequestId = null
);

public record PaginatedResponse<T>(
    bool Success,
    List<T> Data,
    PaginationMeta Meta,
    string? RequestId = null
);

public record PaginationMeta(
    int Page,
    int Limit,
    int Total,
    int TotalPages
);

public record ErrorResponse(
    bool Success,
    string Message,
    ErrorDetail? Error = null,
    string? RequestId = null
);

public record ErrorDetail(
    string Code,
    object? Details = null
);
