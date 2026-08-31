namespace WAMS.Application.DTOs.WorkOrders;

using WAMS.Application.Common;

public record WorkOrderQuery : DataTableQuery
{
    public string? Status { get; init; }
    public long? BudgetPlanId { get; init; }
    public long? BudgetPlanItemId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}

public record ApprovedBpForWoResponse(
    long BudgetPlanId,
    string BudgetPlanCode,
    string TemplateCode,
    long WarehouseShadowId,
    string WarehouseCode,
    string WarehouseName,
    string? Remark,
    bool IsRfba,
    DateTime DocDate,
    string MakerName,
    string? VendorName,
    bool IsLocked,
    bool AllSubmitted,
    List<BpActivityWoStatus> Activities);

public record BpActivityWoStatus(
    long BudgetPlanItemId,
    long ItemShadowId,
    string ItemCode,
    string ActivityName,
    string? ActivityTypeCode,
    string? ActivityTypeDisplay,
    string? CoaName,
    long? WorkOrderId,
    string? WorkOrderCode,
    string? WorkOrderStatus);

public record WorkOrderSummaryResponse(
    long Id,
    string Code,
    long BudgetPlanId,
    string BudgetPlanCode,
    string ActivityTypeCode,
    string ActivityTypeDisplay,
    long ItemShadowId,
    string ActivityName,
    string WarehouseCode,
    string WarehouseName,
    string? PicName,
    bool IsRfba,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    DateTime CreatedAt,
    string CreatedByName,
    string? BlNumber,
    string? ProductName,
    string? VesselName);

public record WorkOrderResponse(
    long Id,
    string Code,
    long BudgetPlanId,
    string BudgetPlanCode,
    string ActivityTypeCode,
    string ActivityTypeDisplay,
    long ItemShadowId,
    string ActivityName,
    long WarehouseShadowId,
    string WarehouseCode,
    string WarehouseName,
    string TemplateCode,
    string? VendorName,
    string? CodeBlock,
    long? PicUserId,
    string? PicName,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsRfba,
    string Status,
    string? Notes,
    GpsLocationResponse? GpsLocation,
    string? ProductName,
    decimal? Quantity,
    string? UomCode,
    string? BlNumber,
    string? VesselName,
    List<TransportOrderRef>? TransportOrders,
    List<WorkOrderUnloadingItemResponse>? UnloadingItems,
    List<WorkOrderLoadingItemResponse>? LoadingItems,
    WorkOrderFumigationDetailResponse? Fumigation,
    WorkOrderStorageDetailResponse? Storage,
    WorkOrderQcDetailResponse? Qc,
    WorkOrderHeavyEquipDetailResponse? HeavyEquipment,
    WorkOrderUnbaggingDetailResponse? Unbagging,
    WorkOrderRebaggingDetailResponse? Rebagging,
    DateTime CreatedAt,
    string CreatedByName,
    DateTime? SubmittedAt,
    string? SubmittedByName);

public record TransportOrderRef(
    long ShadowId,
    string DocNo,
    string Type,
    string VehicleNo,
    string? CardName);

public record WorkOrderUnloadingItemResponse(
    long Id,
    string BlNumber,
    string ProductName,
    decimal Quantity,
    string UomCode,
    string? NoVehicle,
    string? NoContainer,
    string? NoSeal,
    decimal? GrossWeight,
    decimal? FinalWeight,
    decimal? NettWeight,
    int? TotalBag,
    decimal? UnitWeight,
    bool IsChecked,
    int SortOrder);

public record WorkOrderLoadingItemResponse(
    long Id,
    string BlNumber,
    string ProductName,
    decimal Quantity,
    string UomCode,
    string? NoVehicle,
    string? NoContainer,
    string? NoSeal,
    decimal? GrossWeight,
    decimal? FinalWeight,
    decimal? NettWeight,
    int? TotalBag,
    decimal? UnitWeight,
    bool IsChecked,
    int SortOrder);

public record WorkOrderFumigationDetailResponse(
    string? FumiId,
    string? TotalDuration,
    string? BlNumber,
    string? MvName,
    decimal? InitialTemperature,
    decimal? FinalTemperature,
    string? FumigationType,
    decimal? MethylBromideDosage,
    decimal? SulphurFluorideDosage,
    decimal? PhosphineDosage,
    string? Result);

public record WorkOrderStorageDetailResponse(
    bool HasPindahStapel,
    bool HasPembersihan,
    bool HasPerapihan,
    decimal? VolumeWeight,
    int? WorkerOnDuty,
    bool HasMask,
    bool HasSafetyGlasses,
    bool HasHandGloves,
    bool HasHelmet,
    bool HasSafetyShoes,
    bool HasSafetyVest);

public record WorkOrderQcDetailResponse(
    decimal? MoisturePercent,
    decimal? JamurPercent,
    decimal? BauPercent,
    string? QualityStatus);

public record WorkOrderHeavyEquipDetailResponse(
    string? BlNumber,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? StandbyDuration1,
    string? StandbyDuration2,
    string? MinimumDuration,
    decimal? CostPerHour,
    decimal? TotalCost);

public record WorkOrderUnbaggingDetailResponse(
    string? NoVehicle,
    string? NoContainer,
    string? NoSeal,
    decimal? InitialWeight,
    decimal? FinalWeight,
    decimal? UnitWeight,
    decimal? TotalWeight,
    int? TotalBag);

public record WorkOrderRebaggingDetailResponse(
    string? Receiver,
    string? NoVehicle,
    string? NoContainer,
    string? NoSeal,
    decimal? InitialWeight,
    decimal? FinalWeight,
    decimal? TotalWeight);

public record GpsLocationRequest(
    decimal Latitude,
    decimal Longitude,
    decimal? Accuracy,
    DateTime RecordedAt);

public record GpsLocationResponse(
    decimal Latitude,
    decimal Longitude,
    decimal? Accuracy,
    DateTime RecordedAt);

public record CreateUnloadingItemRequest(
    long? SpkShadowId,
    string BlNumber,
    string ProductName,
    decimal Quantity,
    string UomCode,
    string? NoVehicle,
    string? NoContainer,
    string? NoSeal,
    decimal? GrossWeight,
    decimal? FinalWeight,
    decimal? NettWeight,
    int? TotalBag,
    decimal? UnitWeight,
    bool IsChecked,
    int SortOrder);

public record CreateLoadingItemRequest(
    long? SpkShadowId,
    string BlNumber,
    string ProductName,
    decimal Quantity,
    string UomCode,
    string? NoVehicle,
    string? NoContainer,
    string? NoSeal,
    decimal? GrossWeight,
    decimal? FinalWeight,
    decimal? NettWeight,
    int? TotalBag,
    decimal? UnitWeight,
    bool IsChecked,
    int SortOrder);

public record CreateFumigationDetailRequest(
    string? FumiId,
    string? TotalDuration,
    string? BlNumber,
    string? MvName,
    decimal? InitialTemperature,
    decimal? FinalTemperature,
    string? FumigationType,
    decimal? MethylBromideDosage,
    decimal? SulphurFluorideDosage,
    decimal? PhosphineDosage,
    string? Result);

public record CreateStorageDetailRequest(
    bool HasPindahStapel,
    bool HasPembersihan,
    bool HasPerapihan,
    decimal? VolumeWeight,
    int? WorkerOnDuty,
    bool HasMask,
    bool HasSafetyGlasses,
    bool HasHandGloves,
    bool HasHelmet,
    bool HasSafetyShoes,
    bool HasSafetyVest);

public record CreateQcDetailRequest(
    decimal? MoisturePercent,
    decimal? JamurPercent,
    decimal? BauPercent,
    string? QualityStatus);

public record CreateHeavyEquipDetailRequest(
    string? BlNumber,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? StandbyDuration1,
    string? StandbyDuration2,
    string? MinimumDuration,
    decimal? CostPerHour,
    decimal? TotalCost);

public record CreateUnbaggingDetailRequest(
    string? NoVehicle,
    string? NoContainer,
    string? NoSeal,
    decimal? InitialWeight,
    decimal? FinalWeight,
    decimal? UnitWeight,
    decimal? TotalWeight,
    int? TotalBag);

public record CreateRebaggingDetailRequest(
    string? Receiver,
    string? NoVehicle,
    string? NoContainer,
    string? NoSeal,
    decimal? InitialWeight,
    decimal? FinalWeight,
    decimal? TotalWeight);

// Update request
public record UpdateWorkOrderRequest(
    long? PicUserId,
    DateTime? StartDate,
    DateTime? EndDate,
    string? CodeBlock,
    string? Notes,
    GpsLocationRequest? GpsLocation,
    List<long>? TransportOrderShadowIds,
    List<CreateUnloadingItemRequest>? UnloadingItems,
    List<CreateLoadingItemRequest>? LoadingItems,
    CreateFumigationDetailRequest? Fumigation,
    CreateStorageDetailRequest? Storage,
    CreateQcDetailRequest? Qc,
    CreateHeavyEquipDetailRequest? HeavyEquipment,
    CreateUnbaggingDetailRequest? Unbagging,
    CreateRebaggingDetailRequest? Rebagging,
    // others for now mapped to storage detail
    CreateStorageDetailRequest? Others = null
);

public record WorkOrderPicCandidateResponse(long Id, string Fullname);
