namespace WAMS.Infrastructure.Export;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WAMS.Application.DTOs.Rca;
using WAMS.Application.Interfaces.Rca;

public class RcaPdfRenderer : IRcaPdfRenderer
{
    private const string HeaderBg = "#1E3A5F";
    private const string StripeBg = "#EEF2F7";
    private const string BorderColor = "#D1D9E6";
    private const string BorderColorDark = "#1A1A2E";
    private const string TextDark = "#1A1A2E";
    private const string BorderSoftBlack = "#5A5A66"; // softer black for grid lines
    private const string Highlight = "#FFF2CC"; // pale yellow for emphasized metadata

    public byte[] Render(RcaDocument document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20, Unit.Point);
                page.DefaultTextStyle(x => x.FontFamily(PdfFonts.Default));

                page.Header().Element(c => ComposeHeader(c, document));

                page.Content().PaddingVertical(6).Column(col =>
                {
                    col.Item().PaddingTop(10);
                    col.Item().Element(c => ComposeMainTable(c, document));
                    col.Item().PaddingTop(10).Element(c => ComposeSignatures(c, document));
                    col.Item().PaddingTop(10).Element(c => ComposePosSummary(c, document));
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, RcaDocument doc)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Logo - left
                if (doc.LogoData is not null) row.ConstantItem(120).Height(40).AlignLeft().AlignMiddle().Image(doc.LogoData).FitArea();
                else row.ConstantItem(120);

                // Title block - centered
                row.RelativeItem().AlignMiddle().Column(c =>
                {
                    c.Item().AlignCenter().Text(doc.CompanyName).Bold().FontSize(10).FontColor(TextDark);
                    c.Item().AlignCenter().Text("Form Standar").Bold().FontSize(8).FontColor(TextDark);
                    c.Item().AlignCenter().Text("Rekapitulasi Kas Operasional (RCA)").Bold().FontSize(8).FontColor(TextDark);
                });

                // Metadata - right
                row.ConstantItem(200).AlignMiddle().Table(t =>
                {
                    t.ColumnsDefinition(cols => { cols.RelativeColumn(width: 0.45f); cols.RelativeColumn(width: 0.55f); });

                    void MetaRow(string label, string value, bool highlight = false)
                    {
                        t.Cell().AlignRight().PaddingVertical(1)
                            .Text($"{label} :").FontSize(6).Bold().FontColor(TextDark);

                        var v = t.Cell().AlignRight().PaddingVertical(1).PaddingHorizontal(3);
                        if (highlight) v = v.Background(Highlight);
                        v.AlignRight().Text(value).FontSize(6).Bold().FontColor(TextDark);
                    }

                    MetaRow("RCA ID", doc.RcaId, highlight: true);
                    MetaRow("Tanggal", doc.DateTo.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID")));
                    MetaRow("Area", doc.Area ?? "-");
                    MetaRow("Gudang", doc.WarehouseCode);
                    MetaRow("RFBA ID", "");
                });
            });
        });
    }

    private static void ComposeMainTable(IContainer container, RcaDocument doc)
    {
        var total = doc.Lines.Sum(l => l.AmountRupiah);

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.5f); // Date
                cols.RelativeColumn(1.5f);    // COA
                cols.RelativeColumn(1.5f);    // Bill of Lading
                cols.RelativeColumn(1.5f); // Pos Biaya
                cols.RelativeColumn(1.5f);    // Tipe Operasional
                cols.RelativeColumn(1.5f);    // Product
                cols.RelativeColumn(1.5f); // Berat/Jumlah
                cols.RelativeColumn(0.8f); // Satuan
                cols.RelativeColumn(3);    // Keterangan Pos Biaya
                cols.RelativeColumn(2);    // Keterangan lain-lain
                cols.RelativeColumn(2f); // Jumlah (Rp)
            });

            string[] headers = [
                "Tgl Kegiatan",
                "COA & Component",
                "Bill of Lading",
                "Pos Biaya",
                "Tipe Operasional",
                "Product",
                "Berat/Jumlah",
                "Satuan",
                "Keterangan Pos Biaya",
                "Keterangan lain-lain",
                "Jumlah dalam Rupiah"
            ];

            table.Header(header =>
            {
                for (var i = 0; i < headers.Length; i++)
                {
                    var cell = header
                        .Cell()
                        .Border(0.5f).BorderColor(BorderSoftBlack)
                        .Background(HeaderBg)
                        .PaddingVertical(4).PaddingHorizontal(4);

                    if (i == headers.Length - 1) cell = cell.AlignRight();

                    cell.Text(headers[i]).Bold().FontSize(6).FontColor(Colors.White);
                }
            });

            // Data rows - no fill, full soft-black grid for a clean, print-ready look
            for (var i = 0; i < doc.Lines.Count; i++)
            {
                var line = doc.Lines[i];

                // AlignRight is called on the container (IContainer), not on the text descriptor
                void Cell(string text, bool rightAlign = false)
                {
                    var cell = table.Cell()
                        .Border(0.5f).BorderColor(BorderSoftBlack)
                        .PaddingVertical(4).PaddingHorizontal(4);

                    if (rightAlign)
                        cell.AlignRight().Text(text).FontSize(6).FontColor(TextDark);
                    else
                        cell.Text(text).FontSize(6).FontColor(TextDark);
                }

                // Guard against an unscheduled work order (min-date) leaking through as "01/01/0001"
                Cell(line.ActivityDate == DateOnly.MinValue ? "-" : line.ActivityDate.ToString("dd/MM/yyyy"));
                Cell(line.CoaCode);
                Cell(line.BillOfLading ?? "");
                Cell(line.PosBiayaCode);
                Cell(line.TipeOperasional);
                Cell(line.ProductName);
                Cell($"{line.Quantity:N2}");
                Cell(line.UomCode);
                Cell(line.KeteranganPosBiaya);
                Cell(line.Notes ?? "");
                Cell($"{line.AmountRupiah:N2}", rightAlign: true);
            }

            // Total row 2 cells: label spanning first 10 columns, amount in column 11
            table
                .Cell()
                .ColumnSpan(10)
                .Border(0.5f).BorderColor(BorderSoftBlack)
                .PaddingVertical(4).PaddingHorizontal(4).AlignRight()
                .Text("Total :").Bold().FontSize(6).FontColor(TextDark);

            table
                .Cell()
                .Border(0.5f).BorderColor(BorderSoftBlack)
                .PaddingVertical(4).PaddingHorizontal(4).AlignRight()
                .Text($"Rp {total:N2}").Bold().FontSize(6).FontColor(TextDark);
        });
    }

    private static void ComposePosSummary(IContainer container, RcaDocument doc)
    {
        const int Cols = 4; // pairs per row

        container.AlignLeft().Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < Cols; i++)
                {
                    cols.ConstantColumn(70); // code
                    cols.ConstantColumn(85); // amount
                }
            });

            t.Cell()
                .ColumnSpan(Cols * 2)
                .Border(0.2f).BorderColor(BorderSoftBlack)
                .Background(HeaderBg)
                .PaddingVertical(3).PaddingHorizontal(4)
                .AlignCenter()
                .Text("TOTAL BIAYA PER POS")
                .Bold().FontSize(7).FontColor(Colors.White);

            var items = doc.PosTotals.ToList();

            for (var row = 0; row * Cols < items.Count || (row == 0 && items.Count == 0); row++)
            {
                var rowItems = items.Skip(row * Cols).Take(Cols).ToList();
                if (rowItems.Count == 0) break;

                while (rowItems.Count < Cols) rowItems.Add(new PosBiayaTotal("", "", 0));

                for (var i = 0; i < Cols; i++)
                {
                    var item = rowItems[i];

                    t.Cell()
                        .Border(0.2f).BorderColor(BorderSoftBlack)
                        .PaddingVertical(2).PaddingHorizontal(4)
                        .Text(item.Code)
                        .Bold().FontSize(7).FontColor(TextDark);

                    t.Cell()
                        .Border(0.2f).BorderColor(BorderSoftBlack)
                        .PaddingVertical(2).PaddingHorizontal(4)
                        .AlignRight()
                        .Text(item.Code == "" ? "" : $"Rp {item.Total:N2}")
                        .Bold().FontSize(7).FontColor(TextDark);
                }
            }
        });
    }

    private static void ComposeSignatures(IContainer container, RcaDocument doc)
    {
        // Column layout, left to right:
        //   [0]            Dibuat oleh    (BP maker)
        //   [1 .. N]       Disetujui oleh (one column per workflow stage - DYNAMIC,
        //                                  driven by the company's workflow engine)
        //   [N+1]          Diketahui oleh (Superadmin - Ferdy Wan)
        //   [N+2]          Diketahui oleh (AP Staff - role not in WAMS)
        //   [N+3]          Diketahui oleh (Manajer Keuangan - role not in WAMS)
        //
        // A 1-stage company renders a single "Disetujui oleh" column; a 2-stage
        // company renders two; etc. We always render at least one approval column
        // so the form never collapses if no stages are present.
        var approvers = doc.Signatures.Approvers;
        var approverCount = Math.Max(approvers.Count, 1);
        var columnCount = 1 + approverCount + 3;

        // The maker, every approver, and the first "Diketahui" (Superadmin) are
        // handled inside WAMS, so their signing box shows "Digantikan oleh approval
        // WAMS". The last two Diketahui (AP Staff, Manajer Keuangan) keep an empty
        // box for a physical signature on the printout.
        var wamsHandledCount = 1 + approverCount + 1;
        var diketahuiStart = 1 + approverCount;

        (string header, uint span)[] headers =
        [
            ("Dibuat oleh,", 1),
            ("Disetujui oleh,", (uint)approverCount),
            ("Diketahui oleh,", 2),
            ("Diketahui oleh,", 1)
        ];

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < columnCount; i++) cols.RelativeColumn();
            });

            // ROW 1: Header Labels
            foreach (var (header, span) in headers)
            {
                table.Cell()
                    .ColumnSpan(span)
                    .Border(0.2f).BorderColor(BorderColorDark)
                    .PaddingVertical(4).PaddingHorizontal(6)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(header)
                    .FontSize(6).SemiBold().FontColor(TextDark);
            }

            // ROW 2: Signing area
            for (var i = 0; i < columnCount; i++)
            {
                var cell = table.Cell()
                    .Border(0.2f).BorderColor(BorderColorDark)
                    .Height(50);

                if (i < wamsHandledCount)
                {
                    cell.PaddingVertical(4).PaddingHorizontal(6)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text("Digantikan oleh approval WAMS")
                        .Italic().FontSize(6).FontColor(TextDark);
                }
            }

            // ROW 3: Names / Titles
            for (var i = 0; i < columnCount; i++)
            {
                string? name;
                if (i == 0)
                    name = doc.Signatures.Maker;
                else if (i < diketahuiStart)
                    name = i - 1 < approvers.Count ? approvers[i - 1] : null;
                else
                    // Hardcoded roles - these roles do not exist in WAMS yet.
                    name = (i - diketahuiStart) switch
                    {
                        0 => "Ferdy Wan",
                        1 => "AP Staff",
                        2 => "Manajer Keuangan",
                        _ => null
                    };

                table.Cell()
                    .Border(0.2f).BorderColor(BorderColorDark)
                    .PaddingVertical(4).PaddingHorizontal(6)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(name ?? string.Empty)
                    .FontSize(6).SemiBold().FontColor(TextDark);
            }
        });
    }
}
