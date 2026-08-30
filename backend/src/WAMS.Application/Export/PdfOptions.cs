namespace WAMS.Application.Export;

public sealed class PdfOptions
{
    public const string SectionName = "Pdf";
    public int MaxRows { get; set; } = 5000;
}
