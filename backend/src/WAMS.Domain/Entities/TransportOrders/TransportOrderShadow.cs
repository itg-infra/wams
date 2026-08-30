namespace WAMS.Domain.Entities.TransportOrders;

using WAMS.Domain.Entities.Companies;

public class TransportOrderShadow
{
    public long Id { get; set; }
    public long CompanyId { get; set; }

    // ERP fields - map directly from GET /WAMS/LkTOMOLOPMS
    public string DocNo { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;       // SAP doc-type code, e.g. MO / LO
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string VehicleNo { get; set; } = string.Empty;  // sourced from ERP's vehiclePlate
    public string VehicleType { get; set; } = string.Empty;
    public string BlNo { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string UoM { get; set; } = string.Empty;
    public string WhsCode { get; set; } = string.Empty;
    public string WhsName { get; set; } = string.Empty;
    public string DocStatus { get; set; } = string.Empty;  // O = Open, C = Closed

    // Shadow tracking
    public bool IsActive { get; set; } = true;
    public DateTime FirstSeenAt { get; set; }
    public DateTime SyncedAt { get; set; }

    public Company Company { get; set; } = null!;
}
