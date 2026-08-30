namespace WAMS.Application.DTOs.Items;

public record ItemSummaryResponse(
    long Id, 
    string ItemCode, 
    string ItemName, 
    string AcctCode, 
    string AcctName
);
