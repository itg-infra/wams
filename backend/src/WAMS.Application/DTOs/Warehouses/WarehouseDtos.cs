namespace WAMS.Application.DTOs.Warehouses;

using WAMS.Application.Common;

public record WarehouseQuery : DataTableQuery
{
    public long? ProvinceId { get; init; }
}

public record WarehouseResponse(
    long Id,
    string Code,
    string Name,
    string? Location,
    bool IsActive,
    DateTime FirstSeenAt,
    DateTime SyncedAt,
    long? ProvinceId = null,
    string? ProvinceName = null,
    string? ProvinceDisplay = null
);

public record MeWarehouseResponse(
    long Id,
    string Code,
    string Name,
    string? Location,
    bool IsPrimary
);

public record MeProvinceResponse(
    long Id,
    string Name,
    string Display
);

public record ProvinceOption(long Id, string Name, string Display);

public record LocationListResponse(List<ProvinceOption> Locations);
