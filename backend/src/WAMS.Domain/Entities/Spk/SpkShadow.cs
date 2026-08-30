namespace WAMS.Domain.Entities.Spk;

using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;

public class SpkShadow : IShadowEntity
{
    public long Id { get; set; }

    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // ERP fields - map directly from LkMOLOPMS response
    public string Type { get; set; } = string.Empty;        // LO / MO
    public string DocNo { get; set; } = string.Empty;       // SPK number
    public string BaseDoc { get; set; } = string.Empty;     // SO
    public string BaseDocNo { get; set; } = string.Empty;   // Sales Order number
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? DeliveryQty { get; set; }
    public string UoM { get; set; } = string.Empty;
    public string PackType { get; set; } = string.Empty;
    public string WhsCode { get; set; } = string.Empty;
    public string WhsName { get; set; } = string.Empty;
    public string DocStatus { get; set; } = string.Empty;   // O = Open, C = Closed
    public string? BlNo { get; set; }

    // Shadow tracking
    public bool IsActive { get; set; } = true;
    public DateTime FirstSeenAt { get; set; }
    public DateTime SyncedAt { get; set; }
}
