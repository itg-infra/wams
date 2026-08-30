namespace WAMS.Infrastructure.Export;

using System.Globalization;

public static class ExportFormatHelper
{
    public static string FormatString(object? value, string? format)
    {
        if (value is null) return "";
        if (value is DateTime dt)
            return format is not null ? dt.ToString(format, CultureInfo.InvariantCulture) : dt.ToString(CultureInfo.InvariantCulture);
        if (value is DateOnly d)
            return format is not null ? d.ToString(format, CultureInfo.InvariantCulture) : d.ToString(CultureInfo.InvariantCulture);
        if (value is decimal dec)
            return format is not null ? dec.ToString(format, CultureInfo.InvariantCulture) : dec.ToString(CultureInfo.InvariantCulture);
        if (value is double dbl)
            return format is not null ? dbl.ToString(format, CultureInfo.InvariantCulture) : dbl.ToString(CultureInfo.InvariantCulture);
        if (value is bool b)
            return b ? "Yes" : "No";
        return value.ToString() ?? "";
    }
}
