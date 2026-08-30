namespace WAMS.Infrastructure.Export;

using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WAMS.Application.DTOs.Rfba;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Rfba;

/// <summary>
/// Reproduces the client's "Form Standar Pengajuan Batch Advances (RFBA)" form.
/// One A4 page per <see cref="RfbaFormPage"/>; the renderer never sees a budget
/// plan, so re-anchoring the form is a mapper change only.
/// </summary>
public class RfbaFormPdfRenderer : IRfbaFormPdfRenderer
{
    private const string StripeBg = "#EEF2F7";
    private const string BorderColor = "#D1D9E6";
    private const string TextDark = "#1A1A2E";
    private const string HighlightBg = "#FDEADA";

    /// <summary>Body rows in the reference form's fixed-height grid.</summary>
    private const int ReferenceRowCount = 13;

    private static string Money(decimal value) => value.ToString("#,##0.00", CultureInfo.InvariantCulture);

    private static string Qty(decimal value) => value.ToString("#,##0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Blank rows needed to reach the reference grid height. A page with more
    /// components than the reference simply grows and QuestPDF paginates it.
    /// </summary>
    public static int BuildFillerRowCount(int rowCount) => Math.Max(0, ReferenceRowCount - rowCount);

    public byte[] Render(RfbaFormDocument document, PdfReportMetadata metadata)
    {
        return Document.Create(container =>
        {
            foreach (var formPage in document.Pages)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Portrait());
                    page.Margin(30, Unit.Point);
                    page.DefaultTextStyle(x => x.FontFamily(PdfFonts.Default));

                    page.Header().Element(c => ComposeHeader(c, metadata));

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Item().PaddingTop(14).Element(c => ComposeMeta(c, formPage));
                        col.Item().PaddingTop(18).Element(c => ComposeItems(c, formPage));
                        col.Item().PaddingTop(14).Element(c => ComposePayee(c, formPage));
                        col.Item().PaddingTop(26).Element(c => ComposeSignatures(c, document));
                    });

                    if (document.IsDraft)
                        page.Foreground().AlignCenter().AlignMiddle().Rotate(-30)
                            .Text("DRAFT").FontSize(100).Bold().FontColor(Colors.Grey.Medium.WithAlpha(0.25f));
                });
            }
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, PdfReportMetadata metadata)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (metadata.LogoData is not null)
                    row.ConstantItem(120).Height(45).AlignLeft().AlignMiddle().Image(metadata.LogoData).FitArea();
                else
                    row.ConstantItem(120);

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignRight().Text(metadata.CompanyName).Bold().FontSize(11).FontColor(TextDark);

                    if (!string.IsNullOrWhiteSpace(metadata.Address))
                        c.Item().AlignRight().Text(metadata.Address).FontSize(8).FontColor(TextDark);
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(TextDark);

            col.Item().PaddingTop(14).AlignCenter().Text("Form Standar Pengajuan").Bold().FontSize(10).FontColor(TextDark);
            col.Item().AlignCenter().Text("Batch Advances (RFBA)").Bold().FontSize(10).FontColor(TextDark);
        });
    }

    private static void ComposeMeta(IContainer container, RfbaFormPage page)
    {
        container.Column(col =>
        {
            // Sample prints the long form: "Tuesday, 03 February 2026".
            col.Item().AlignRight().Text(page.DocDate.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture))
                .Bold().FontSize(8).FontColor(TextDark);

            col.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Produk : {page.Produk}").Bold().FontSize(8).FontColor(TextDark);
                    c.Item().Text($"Bill of Lading : {page.BillOfLading}").Bold().FontSize(8).FontColor(TextDark);
                    c.Item().Text($"Vessel : {page.Vessel}").Bold().FontSize(8).FontColor(TextDark);
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.ConstantItem(70).Text("RFBA ID :").Bold().FontSize(8).FontColor(TextDark);
                        r.RelativeItem().Background(HighlightBg).PaddingHorizontal(3)
                            .Text(page.RfbaId).FontSize(8).FontColor(TextDark);
                    });

                    c.Item().PaddingTop(2).Row(r =>
                    {
                        r.ConstantItem(70).Text("Area Gudang :").Bold().FontSize(8).FontColor(TextDark);
                        r.RelativeItem().PaddingHorizontal(3).Text(page.AreaGudang).FontSize(8).FontColor(TextDark);
                    });
                });
            });
        });
    }

    private static void ComposeItems(IContainer container, RfbaFormPage page)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(6).Text("Estimasi Biaya & Pekerjaan :").Bold().FontSize(8).FontColor(TextDark);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.8f);
                    columns.RelativeColumn(2.2f);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Komponen");
                    HeaderCell(header.Cell(), "Jumlah");
                    HeaderCell(header.Cell(), "Satuan");
                    HeaderCell(header.Cell(), "Tarif per Satuan");
                    HeaderCell(header.Cell(), "Total Jumlah Rupiah");
                });

                foreach (var row in page.Rows)
                {
                    BodyCell(table.Cell(), row.Component, center: true);
                    BodyCell(table.Cell(), Qty(row.Quantity), center: true);
                    BodyCell(table.Cell(), row.Uom, center: true);
                    BodyCell(table.Cell(), $"Rp {Money(row.UnitRate)}", center: true);
                    BodyCell(table.Cell(), $"Rp {Money(row.Total)}", center: true);
                }

                for (var i = 0; i < BuildFillerRowCount(page.Rows.Count); i++)
                {
                    BodyCell(table.Cell(), string.Empty, center: true);
                    BodyCell(table.Cell(), string.Empty, center: true);
                    BodyCell(table.Cell(), string.Empty, center: true);
                    BodyCell(table.Cell(), string.Empty, center: true);
                    BodyCell(table.Cell(), string.Empty, center: true);
                }

                table.Cell().ColumnSpan(4).Border(1).BorderColor(TextDark).Padding(4)
                    .AlignRight().Text("Total :").Bold().FontSize(8).FontColor(TextDark);

                table.Cell().Border(1).BorderColor(TextDark).Background(StripeBg).Padding(4)
                    .AlignCenter().Text($"Rp {Money(page.Total)}").Bold().FontSize(8).FontColor(TextDark);
            });
        });

        static void HeaderCell(IContainer cell, string text) =>
            cell.Border(1).BorderColor(TextDark).Padding(4)
                .AlignCenter().Text(text).Bold().FontSize(8).FontColor(TextDark);

        static void BodyCell(IContainer cell, string text, bool center) =>
            cell.Border(1).BorderColor(TextDark).Padding(4).MinHeight(16)
                .Element(c => center ? c.AlignCenter() : c)
                .Text(text).FontSize(8).FontColor(TextDark);
    }

    private static void ComposePayee(IContainer container, RfbaFormPage page)
    {
        container.Row(row =>
        {
            row.RelativeItem();

            row.RelativeItem().Border(1).BorderColor(TextDark).Column(c =>
            {
                c.Item().Background(StripeBg).BorderBottom(1).BorderColor(TextDark).Padding(3)
                    .Text("Di Transfer ke :").Bold().FontSize(8).FontColor(TextDark);

                PayeeLine(c.Item(), "Nama :", page.PayeeName);
                PayeeLine(c.Item(), "Acc. Number :", page.PayeeAccountNumber);
                PayeeLine(c.Item(), "Bank :", page.PayeeBank);
            });
        });

        static void PayeeLine(IContainer container, string label, string? value) =>
            container.PaddingHorizontal(3).PaddingVertical(2).Row(r =>
            {
                r.ConstantItem(75).Text(label).FontSize(8).FontColor(TextDark);
                r.RelativeItem().Text(value).FontSize(8).FontColor(TextDark);
            });
    }

    /// <summary>
    /// Maker/Approvers print the WAMS name when known; "Mengetahui" stays a blank
    /// wet-signature box - there is no WAMS role behind it. "Disetujui Oleh" is one
    /// column per approval stage of the plan's workflow (same as the PO and RCA
    /// forms), so a 1-stage company prints one column and a 2-stage company two.
    /// </summary>
    public static List<(string Label, string? Name, string DateLine)> BuildSignatories(RfbaFormDocument document)
    {
        List<(string Label, string? Name, string DateLine)> signatories =
            [("Dibuat oleh,", document.MakerName, PdfDates.SignatureDateLine(document.MakerDate))];

        foreach (var approver in document.Approvers)
            signatories.Add(("Disetujui Oleh,", approver.Name, PdfDates.SignatureDateLine(approver.ApprovedAt)));

        // Never collapse the block: a plan with no workflow still prints one empty
        // approval column to sign by hand.
        if (document.Approvers.Count == 0)
            signatories.Add(("Disetujui Oleh,", null, PdfDates.SignatureDateLine(null)));

        signatories.Add(("Mengetahui", null, PdfDates.SignatureDateLine(null)));

        return signatories;
    }

    private static void ComposeSignatures(IContainer container, RfbaFormDocument document)
    {
        container.Row(row =>
        {
            foreach (var (label, name, dateLine) in BuildSignatories(document))
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(label).Bold().FontSize(8).FontColor(TextDark);
                    c.Item().Height(50);
                    c.Item().PaddingRight(20).LineHorizontal(0.5f).LineColor(TextDark);
                    c.Item().PaddingTop(2).Text(name ?? string.Empty).FontSize(8).FontColor(TextDark);
                    c.Item().Text(dateLine).FontSize(8).FontColor(TextDark);
                });
            }
        });
    }
}
