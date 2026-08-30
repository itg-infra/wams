namespace WAMS.Application.DTOs.Uoms;

public record UomResponse(long Id, string Code, string Name, bool IsActive);
public record CreateUomRequest(string Code, string Name);
public record UpdateUomRequest(string Name, bool IsActive);
