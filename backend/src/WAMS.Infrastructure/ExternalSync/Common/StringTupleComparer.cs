namespace WAMS.Infrastructure.ExternalSync.Common;

public sealed class StringTupleComparer : IEqualityComparer<(string, string)>
{
    public static readonly StringTupleComparer Instance = new();

    public bool Equals((string, string) x, (string, string) y)
        => string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode((string, string) obj)
        => HashCode.Combine(obj.Item1.ToUpperInvariant(), obj.Item2.ToUpperInvariant());
}
