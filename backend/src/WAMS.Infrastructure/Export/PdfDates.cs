namespace WAMS.Infrastructure.Export;

/// <summary>
/// Timestamps are stored UTC but every reader of these forms is in Jakarta:
/// printing a UTC timestamp straight out puts the previous day on any document
/// generated or approved before 07:00 WIB.
/// </summary>
public static class PdfDates
{
    private static readonly TimeZoneInfo Jakarta =
        TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Jakarta", out var tz) ? tz : TimeZoneInfo.Utc;

    /// <summary>False when the host has no tz database, in which case times stay UTC.</summary>
    public static bool IsJakarta => !ReferenceEquals(Jakarta, TimeZoneInfo.Utc);

    public static DateTime ToJakarta(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Jakarta);

    /// <summary>The "Tgl." line under a signature box; the label alone when unsigned.</summary>
    public static string SignatureDateLine(DateTime? utc) =>
        utc is null ? "Tgl." : $"Tgl. {ToJakarta(utc.Value):dd/MM/yyyy}";
}
