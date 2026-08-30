namespace WAMS.Application.DTOs.Rca;

public record RcaQuery(
    string WarehouseCode,
    DateOnly DateFrom,
    DateOnly DateTo);

public record RcaLineItem(
    DateOnly ActivityDate,
    string CoaCode,
    string? BillOfLading,
    string PosBiayaCode,
    string TipeOperasional,
    string ProductName,
    decimal Quantity,
    string UomCode,
    string KeteranganPosBiaya,
    string? Notes,
    decimal AmountRupiah);

public record PosBiayaTotal(string Code, string Name, decimal Total);

// Signature blocks for the RCA form. "Approvers" ("Disetujui oleh") is dynamic:
// one entry per workflow stage in the company's workflow engine, so a 1-stage
// company renders one approval column and a 2-stage company renders two.
public record RcaSignatures(
    string? Maker,
    IReadOnlyList<string?> Approvers);

public record RcaRepoData(
    List<RcaLineItem> Lines,
    List<PosBiayaTotal> PosTotals,
    RcaSignatures Signatures,
    string? WarehouseLocation);

public record RcaDocument(
    string RcaId,
    string CompanyName,
    byte[]? LogoData,
    string WarehouseCode,
    string? Area,
    DateOnly DateFrom,
    DateOnly DateTo,
    List<RcaLineItem> Lines,
    List<PosBiayaTotal> PosTotals,
    RcaSignatures Signatures);
