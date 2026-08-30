namespace WAMS.Application.DTOs.ActivityTypes;

public record CreateActivityTypeRequest(string Code, string Name);
public record UpdateActivityTypeRequest(string? Code, string? Name, bool? IsActive);
public record ActivityTypeResponse(long Id, string Code, string Name, bool IsActive);
