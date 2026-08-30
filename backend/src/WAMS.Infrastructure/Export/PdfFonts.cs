namespace WAMS.Infrastructure.Export;

public static class PdfFonts
{
    public static readonly string Default = "PT Serif";

    static PdfFonts()
    {
        QuestPDF.Drawing.FontManager.RegisterFontFromEmbeddedResource(
            "WAMS.Infrastructure.Export.Fonts.PTSerif-Regular.ttf"
        );
        QuestPDF.Drawing.FontManager.RegisterFontFromEmbeddedResource(
            "WAMS.Infrastructure.Export.Fonts.PTSerif-Bold.ttf"
        );
    }
}
