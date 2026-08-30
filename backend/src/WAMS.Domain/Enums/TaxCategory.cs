namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class TaxCategory : SmartEnum<TaxCategory, string>
{
    public static readonly TaxCategory Ppn = new(nameof(Ppn), "Ppn");
    public static readonly TaxCategory Pph = new(nameof(Pph), "Pph");

    private TaxCategory(string name, string value) : base(name, value) { }
}
