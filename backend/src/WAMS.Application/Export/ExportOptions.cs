namespace WAMS.Application.Export;

public sealed class ExportOptions
{
    public const string SectionName = "Export";
    public int MaxRows { get; set; } = 50_000;
}
