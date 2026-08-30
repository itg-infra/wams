// src/WAMS.Domain/Constants/ProvinceCodes.cs
namespace WAMS.Domain.Constants;

public static class ProvinceCodes
{
    public const string Global = "GLOBAL";

    // (Code, Name [UPPER, matching key], Display [proper case, for UI], Aliases [UPPER]).
    // Aliases need NOT repeat Name; Name is matched implicitly. Covers all values
    // currently seen in WarehouseShadow.Location.
    public static readonly IReadOnlyList<(string Code, string Name, string Display, string[] Aliases)> Seed =
    [
        (Global,    "GLOBAL",            "Global",              []),
        ("ID-JI",   "JAWA TIMUR",        "Jawa Timur",          []),
        ("ID-JT",   "JAWA TENGAH",       "Jawa Tengah",         []),
        ("ID-JK",   "JAKARTA",           "Jakarta",             ["DKI JAKARTA"]),
        ("ID-SN",   "SULAWESI SELATAN",  "Sulawesi Selatan",    []),
        ("ID-ST",   "SULAWESI TENGAH",   "Sulawesi Tengah",     []),
        ("ID-SR",   "SULAWESI BARAT",    "Sulawesi Barat",      []),
        ("ID-SU",   "SUMATERA UTARA",    "Sumatera Utara",      []),
        ("ID-LA",   "LAMPUNG",           "Lampung",             []),
        ("ID-JA",   "JAMBI",             "Jambi",               []),
        ("ID-NB",   "NUSA TENGGARA BARAT", "Nusa Tenggara Barat", []),
        ("ID-KI",   "KALIMANTAN TIMUR",  "Kalimantan Timur",    []),
        ("ID-BB",   "BANGKA BELITUNG",   "Bangka Belitung",     ["KEPULAUAN BANGKA BELITUNG"]),
    ];
}
