namespace WAMS.Infrastructure.ExternalSync.Item;

/// <summary>
/// Maps the JSON response from GET /WAMS/LkCostItem
/// Field names must exactly match the ERP response casing.
/// </summary>
public record ItemErpDto(string ItemCode, string ItemName, string AcctCode, string AcctName);
