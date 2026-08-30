namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class RateCardStatus : SmartEnum<RateCardStatus, string>
{
    public static readonly RateCardStatus Draft = new(nameof(Draft), "Draft");
    public static readonly RateCardStatus Submitted = new(nameof(Submitted), "Submitted");

    private RateCardStatus(string name, string value) : base(name, value) { }

    public bool CanBeSubmitted => this == Draft;
}
