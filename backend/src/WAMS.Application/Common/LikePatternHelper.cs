namespace WAMS.Application.Common;

public static class LikePatternHelper
{
    // Escapes assume '\' as the ILIKE escape char. EF.Functions.ILike(col, pattern) (2-arg)
    // makes Npgsql emit "ESCAPE ''", which disables escaping entirely - always use the
    // 3-arg overload, EF.Functions.ILike(col, pattern, "\\"), with patterns from this helper.
    public static string ToContainsPattern(string search)
    {
        var escaped = search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return $"%{escaped}%";
    }
}
