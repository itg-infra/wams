namespace WAMS.Infrastructure.Export;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WAMS.Application.Export;

public class GenericPdfDocument<T>(
    IReadOnlyList<ExportColumnDefinition<T>> columns,
    IReadOnlyList<T> data,
    PdfReportMetadata metadata) : IDocument
{

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Portrait());
            page.Margin(30, Unit.Point);
            page.DefaultTextStyle(x => x.FontFamily(PdfFonts.Default));
            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(8).Element(ComposeContent);
            page.Footer().BorderTop(0.5f).BorderColor(BorderColor).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().AlignLeft()
                    .Text(metadata.Title).FontSize(7).FontColor(Colors.Grey.Medium);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                    text.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (metadata.LogoData is not null)
                    row.ConstantItem(60).Height(40).Image(metadata.LogoData).FitArea();

                // add padding here
                row.ConstantItem(10);

                row.RelativeItem().AlignLeft().Column(c =>
                {
                    var currentTime = PdfDates.ToJakarta(metadata.GeneratedAt);
                    var tzLabel = PdfDates.IsJakarta ? "WIB" : "UTC";

                    c.Item().Text(metadata.CompanyName).Bold().FontSize(12);
                    c.Item().Text(metadata.Title).FontSize(11);
                    c.Item()
                        .Text($"Generated: {currentTime:yyyy-MM-dd HH:mm} {tzLabel}")
                        .FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });

            col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private const string HeaderBg   = "#1E3A5F";
    private const string StripeBg   = "#EEF2F7";
    private const string BorderColor = "#D1D9E6";
    private const string TextDark    = "#1A1A2E";

    private void ComposeContent(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                foreach (var col in columns)
                    cols.RelativeColumn((float)col.Width);
            });

            table.Header(header =>
            {
                foreach (var col in columns)
                {
                    header.Cell()
                        .Background(HeaderBg)
                        .BorderBottom(1).BorderColor(HeaderBg)
                        .PaddingVertical(6).PaddingHorizontal(6)
                        .Text(col.Header)
                        .Bold()
                        .FontColor(Colors.White)
                        .FontSize(8);
                }
            });

            for (var i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var bg = i % 2 == 0 ? Colors.White : (Color)StripeBg;

                foreach (var col in columns)
                {
                    table.Cell()
                        .Background(bg)
                        .BorderBottom(0.5f).BorderColor(BorderColor)
                        .PaddingVertical(5).PaddingHorizontal(6)
                        .Text(ExportFormatHelper.FormatString(col.Accessor(item), col.Format))
                        .FontSize(8)
                        .FontColor(TextDark);
                }
            }
        });
    }
}
