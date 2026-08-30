namespace WAMS.Infrastructure.Export;

using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.PurchaseOrders;

public class PurchaseOrderPdfRenderer : IPurchaseOrderPdfRenderer
{
    private const string HeaderBg = "#1E3A5F";
    private const string StripeBg = "#EEF2F7";
    private const string BorderColor = "#D1D9E6";
    private const string TextDark = "#1A1A2E";

    private static string Money(decimal value) => value.ToString("#,##0.00", CultureInfo.InvariantCulture);

    private static string Qty(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Reference form merges PPN/PPh into one "Tax" line; kept separate here so
    /// each figure reconciles against our AP data. Discount is always 0 (no PO
    /// field for it) but stays in the list for layout parity.
    /// </summary>
    public static IReadOnlyList<(string Label, string Value)> BuildTotalsRows(PurchaseOrderResponse po) =>
    [
        ("Sub Total", Money(po.GrandTotal)),
        ("Discount", Money(0m)),
        ("PPN", Money(po.TotalPpnAmount)),
        ("PPh", Money(po.TotalPphAmount)),
        ("Total", Money(po.TaxInclusiveGrandTotal))
    ];

    /// <summary>
    /// Generation date once sent to SAP; falls back to the editable DocDate for drafts.
    /// GeneratedAt is a UTC timestamp so it needs converting; DocDate is already a plain date.
    /// </summary>
    public static DateTime DisplayDate(PurchaseOrderResponse po) =>
        po.GeneratedAt is null ? po.DocDate : PdfDates.ToJakarta(po.GeneratedAt.Value);

    private static void ComposeLabelValue(IContainer container, string label, string value)
    {
        container.Row(row =>
        {
            row.ConstantItem(45).Text(label).FontSize(8).FontColor(TextDark);
            row.ConstantItem(8).Text(":").FontSize(8).FontColor(TextDark);
            row.RelativeItem().Text(value).FontSize(8).FontColor(TextDark);
        });
    }

    public byte[] Render(PurchaseOrderResponse po, PdfReportMetadata metadata)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Portrait());
                page.Margin(30, Unit.Point);
                page.DefaultTextStyle(x => x.FontFamily(PdfFonts.Default));

                page.Header().Element(c => ComposeHeader(c, metadata));

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Item().Element(c => ComposeParties(c, po));
                    col.Item().PaddingTop(12).Element(c => ComposeItems(c, po));
                    col.Item().PaddingTop(10).Element(c => ComposeSummary(c, po));
                    col.Item().PaddingTop(30).Element(c => ComposeSignatures(c, po));
                });

                // No SapPoNumber means it never reached SAP - stamp as draft.
                if (po.SapPoNumber is null)
                    page.Foreground().AlignCenter().AlignMiddle().Rotate(-30)
                        .Text("DRAFT").FontSize(100).Bold().FontColor(Colors.Grey.Medium.WithAlpha(0.25f));
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, PdfReportMetadata metadata)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c =>
                {
                    // MaxWidth caps the logo so a wide image can't claim more than
                    // its share of the row and push the company column off-alignment.
                    if (metadata.LogoData is not null)
                        c.MaxWidth(120).Height(45).AlignLeft().AlignMiddle().Image(metadata.LogoData).FitArea();
                });

                row.ConstantItem(20);

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(metadata.CompanyName).Bold().FontSize(14).FontColor(TextDark);

                    if (!string.IsNullOrWhiteSpace(metadata.Address))
                        c.Item().PaddingTop(4).Text(metadata.Address).FontSize(12).FontColor(TextDark);
                });
            });

            col.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(20);
                row.RelativeItem().AlignCenter().Text("Purchase Order").Bold().FontSize(13).FontColor(TextDark);
            });

            col.Item().PaddingTop(3).LineHorizontal(1).LineColor(BorderColor);
        });
    }

    private static void ComposeParties(IContainer container, PurchaseOrderResponse po)
    {
        // vendor address omitted, VendorShadow only has CardCode/CardName.
        container.Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().BorderBottom(1).BorderColor(TextDark).PaddingBottom(2)
                    .Text("To").Bold().FontSize(8).FontColor(TextDark);

                c.Item().Background(StripeBg).Padding(6).MinHeight(55)
                    .Text(po.VendorName).FontSize(8).FontColor(TextDark);
            });

            row.ConstantItem(20);

            row.RelativeItem().Column(c =>
            {
                c.Item().BorderBottom(1).BorderColor(TextDark).PaddingBottom(2)
                    .Text(" ").FontSize(8);

                c.Item().Background(StripeBg).Padding(6).MinHeight(55).Column(m =>
                {
                    m.Item().Element(e => ComposeLabelValue(e, "No.", po.Code));
                    m.Item().Element(e => ComposeLabelValue(e, "Tanggal", DisplayDate(po).ToString("dd/MM/yyyy")));
                });
            });
        });
    }

    private static void ComposeItems(IContainer container, PurchaseOrderResponse po)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3.5f);   // Nama Barang / Jasa
                cols.RelativeColumn(1.5f);   // Kode
                cols.RelativeColumn(3.5f);   // Deskripsi Kode
                cols.RelativeColumn(1f);     // Unit
                cols.RelativeColumn(2f);     // Harga
                cols.RelativeColumn(2f);     // Total
            });

            (string Text, bool Right)[] headers =
            [
                ("Nama Barang / Jasa", false),
                ("Kode", false),
                ("Deskripsi Kode", false),
                ("Unit", false),
                ("Harga", true),
                ("Total", true)
            ];

            table.Header(header =>
            {
                foreach (var (text, right) in headers)
                {
                    var cell = header.Cell()
                        .Background(HeaderBg)
                        .PaddingVertical(5).PaddingHorizontal(5);

                    if (right) cell = cell.AlignRight();

                    cell.Text(text).Bold().FontSize(8).FontColor(Colors.White);
                }
            });

            for (var i = 0; i < po.Items.Count; i++)
            {
                var item = po.Items[i];
                var bg = i % 2 == 0 ? Colors.White : (Color)StripeBg;

                // AlignRight is called on the container, not on the text descriptor
                void Cell(string text, bool right = false)
                {
                    var cell = table.Cell()
                        .Background(bg)
                        .BorderBottom(0.5f).BorderColor(BorderColor)
                        .PaddingVertical(5).PaddingHorizontal(5);

                    if (right) cell = cell.AlignRight();

                    cell.Text(text).FontSize(8).FontColor(TextDark);
                }

                Cell(item.ItemName);
                Cell(item.ItemCode);
                Cell(item.ItemName);
                Cell(Qty(item.Quantity));
                Cell(Money(item.CostValue), right: true);
                Cell(Money(item.TotalValue), right: true);
            }
        });
    }

    private static void ComposeSummary(IContainer container, PurchaseOrderResponse po)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().BorderBottom(1).BorderColor(TextDark).PaddingBottom(2)
                    .Text("Keterangan :").FontSize(8).FontColor(TextDark);

                c.Item().PaddingTop(4)
                    .Text(po.Remark ?? "-").FontSize(8).FontColor(TextDark);
            });

            row.ConstantItem(20);

            row.RelativeItem().Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1f);
                    cols.RelativeColumn(1f);
                });

                var rows = BuildTotalsRows(po);

                for (var i = 0; i < rows.Count; i++)
                {
                    var (label, value) = rows[i];
                    var isTotal = i == rows.Count - 1;
                    var bg = isTotal ? (Color)StripeBg : Colors.White;

                    var labelCell = t.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4);
                    var valueCell = t.Cell().Background(bg).PaddingVertical(3).PaddingHorizontal(4).AlignRight();

                    if (isTotal)
                    {
                        labelCell.Text(label).Bold().FontSize(9).FontColor(TextDark);
                        valueCell.Text(value).Bold().FontSize(9).FontColor(TextDark);
                    }
                    else
                    {
                        labelCell.Text(label).FontSize(8).FontColor(TextDark);
                        valueCell.Text(value).FontSize(8).FontColor(TextDark);
                    }
                }
            });
        });
    }

    /// <summary>
    /// Maker/Approvers print the WAMS name and date when known; "Diketahui Oleh"
    /// stays a blank wet-signature box. "Disetujui Oleh" is one column per approval
    /// stage of the source budget plan's workflow (same as the RCA form), so a
    /// 1-stage company prints one column and a 2-stage company prints two.
    /// </summary>
    public static List<(string Label, string? Name, string DateLine)> BuildSignatories(PurchaseOrderResponse po)
    {
        List<(string Label, string? Name, string DateLine)> signatories =
            [("Dibuat Oleh,", po.CreatedByName, PdfDates.SignatureDateLine(po.CreatedAt))];

        foreach (var approver in po.Approvers)
            signatories.Add(("Disetujui Oleh,", approver.Name, PdfDates.SignatureDateLine(approver.ApprovedAt)));

        // Never collapse the block: a PO whose BP carries no workflow still prints
        // an empty approval column to sign by hand.
        if (po.Approvers.Count == 0)
            signatories.Add(("Disetujui Oleh,", null, PdfDates.SignatureDateLine(null)));

        signatories.Add(("Diketahui Oleh,", null, PdfDates.SignatureDateLine(null)));

        return signatories;
    }

    private static void ComposeSignatures(IContainer container, PurchaseOrderResponse po)
    {
        container.Row(row =>
        {
            foreach (var (label, name, dateLine) in BuildSignatories(po))
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(label).FontSize(8).FontColor(TextDark);
                    c.Item().Height(50);
                    c.Item().PaddingRight(20).LineHorizontal(0.5f).LineColor(TextDark);
                    c.Item().PaddingTop(2).Text(name ?? string.Empty).FontSize(8).FontColor(TextDark);
                    c.Item().Text(dateLine).FontSize(8).FontColor(TextDark);
                });
            }
        });
    }
}
