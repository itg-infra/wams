namespace WAMS.Application.DTOs.Rfba;

/// <summary>One line of the "Estimasi Biaya &amp; Pekerjaan" table.</summary>
public record RfbaFormRow(
    string Component,
    decimal Quantity,
    string Uom,
    decimal UnitRate,
    decimal Total);

/// <summary>
/// One printed RFBA form: a single Bill of Lading's worth of cost components.
/// Nullable on every field WAMS cannot currently supply (Vessel, payee bank),
/// so the renderer's only job is to lay out whatever it is handed.
/// </summary>
public record RfbaFormPage(
    string? RfbaId,
    string? Produk,
    string? BillOfLading,
    string? Vessel,
    string? AreaGudang,
    DateTime DocDate,
    IReadOnlyList<RfbaFormRow> Rows,
    decimal Total,
    string? PayeeName,
    string? PayeeAccountNumber,
    string? PayeeBank);

// One entry per stage of the plan's approval workflow, in StageOrder, so the printed
// "Disetujui Oleh" block scales with the company's workflow template - same shape as
// PoApprover and RcaSignatures.Approvers. Name/date are null for an unapproved stage,
// which is the normal case here: this form prints mid-workflow plans as DRAFT.
public record RfbaApprover(string? Name, DateTime? ApprovedAt);

/// <summary>A set of RFBA forms printed as one PDF, one page each.</summary>
public record RfbaFormDocument(
    IReadOnlyList<RfbaFormPage> Pages,
    bool IsDraft,
    string? MakerName,
    DateTime? MakerDate,
    IReadOnlyList<RfbaApprover> Approvers);
