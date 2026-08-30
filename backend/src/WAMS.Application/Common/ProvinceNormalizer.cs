namespace WAMS.Application.Common;

using System.Text.RegularExpressions;

public static partial class ProvinceNormalizer
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        return Whitespace().Replace(raw.Trim(), " ").ToUpperInvariant();
    }
}
