namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class AccountPayableStatus : SmartEnum<AccountPayableStatus, string>
{
    public static readonly AccountPayableStatus Draft = new(nameof(Draft), "Draft");
    public static readonly AccountPayableStatus Generated = new(nameof(Generated), "Generated");

    private AccountPayableStatus(string name, string value) : base(name, value) { }

    public bool CanBeEdited => this == Draft;
    public bool CanBeDeleted => this == Draft;
    public bool CanBeGenerated => this == Draft;
}
