namespace WAMS.Application.DTOs.TransportOrders;

using WAMS.Application.Common;

public record TransportOrderQuery : DataTableQuery
{
    public long? BudgetPlanId { get; init; }
    public string? DocNo { get; init; }
    public string? Type { get; init; }
    public string? WhsCode { get; init; }
    public string? DocStatus { get; init; } // O / C - defaults to O in repo
}

public record TransportOrderShadowResponse(
    long Id,
    string DocNo,
    string Type,
    string CardCode,
    string CardName,
    string VehicleNo,
    string VehicleType,
    string BlNo,
    string ItemCode,
    string ItemName,
    decimal? Quantity,
    string UoM,
    string WhsCode,
    string WhsName,
    string DocStatus);
