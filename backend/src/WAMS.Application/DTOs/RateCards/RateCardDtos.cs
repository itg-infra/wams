namespace WAMS.Application.DTOs.RateCards;

using WAMS.Application.DTOs.Items;
using WAMS.Application.DTOs.Uoms;
using WAMS.Application.DTOs.Vendors;

public record RateCardItemTaxResponse(long Id, string Code, decimal Rate);

public record RateCardItemResponse(
    long Id,
    ItemSummaryResponse Item,
    UomResponse Uom,
    decimal CostValue,
    RateCardItemTaxResponse? PpnTaxType,
    RateCardItemTaxResponse? PphTaxType,
    string? CostTreatment);

public record RateCardResponse(
    long Id,
    VendorSummaryResponse Vendor,
    string Status,
    List<RateCardItemResponse> Items,
    DateTime CreatedAt,
    DateTime? SubmittedAt);

public record RateCardSummaryResponse(
    long Id,
    VendorSummaryResponse Vendor,
    string Status,
    int ItemCount,
    DateTime CreatedAt);

public record VendorRateResponse(
    long VendorShadowId,
    string VendorCode,
    string VendorName,
    long UomMasterId,
    string UomCode,
    string UomName,
    decimal CostValue,
    RateCardItemTaxResponse? PpnTaxType,
    RateCardItemTaxResponse? PphTaxType,
    string? CostTreatment);

public record CreateRateCardItemRequest(
    long ItemShadowId,
    long UomMasterId,
    decimal CostValue,
    long? PpnTaxTypeId,
    long? PphTaxTypeId,
    string? CostTreatment = null);

public record CreateRateCardRequest(long VendorShadowId, List<CreateRateCardItemRequest> Items);
public record UpdateRateCardRequest(long? VendorShadowId, List<CreateRateCardItemRequest> Items);
public record RateAvailability(long VendorShadowId, long ItemShadowId, bool Found, bool Submitted);
