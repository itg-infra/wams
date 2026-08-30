namespace WAMS.Application.DTOs.Companies;

/// <summary>
/// Request to create a new company
/// </summary>
public record CreateCompanyRequest(
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? Email
);

/// <summary>
/// Request to update an existing company
/// </summary>
public record UpdateCompanyRequest(
    string? Name,
    string? Address,
    string? Phone,
    string? Email,
    bool? IsActive
);

/// <summary>
/// Full company response with details
/// </summary>
public record CompanyResponse(
    long Id,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    int WarehouseCount,
    bool HasLogo
);

/// <summary>
/// Public response for company list (used in login dropdown)
/// </summary>
public record CompanyPublicResponse(
    long Id,
    string Code,
    string Name
);

/// <summary>
/// Request to assign a user to a company
/// </summary>
public record AssignUserToCompanyRequest(
    long UserId,
    long CompanyId
);
