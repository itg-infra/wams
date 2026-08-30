namespace WAMS.Domain.Constants;

public static class CostTreatments
{
    public const string Dibiayakan = "Dibiayakan";
    public const string TidakDibiayakan = "TidakDibiayakan";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Dibiayakan,
        TidakDibiayakan,
    };
}
