namespace WAMS.Infrastructure.Export;

using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.RecapWorkOrders;

public class RecapWorkOrderPdfRenderer : IRecapWorkOrderPdfRenderer
{
    private const string HeaderBg = "#1E3A5F";
    private const string SectionBg = "#DCE6F1";
    private const string BorderColor = "#5A5A66";
    private const string TextDark = "#1A1A2E";
    private const string Highlight = "#FFF2CC";
    private static readonly CultureInfo Indonesian = new("id-ID");

    public byte[] Render(RecapWorkOrderDetailResponse recap, PdfReportMetadata metadata)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20, Unit.Point);
                page.DefaultTextStyle(x => x.FontFamily(PdfFonts.Default).FontColor(TextDark));

                page.Header().Element(c => ComposeHeader(c, recap, metadata));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Element(c => ComposeBudgetPlan(c, recap));
                    column.Item().Element(c => ComposeSpkTable(c, recap.Plan.SpkDocuments));
                    column.Item().Element(c => ComposeCostTable(c, recap.Plan.CostDetails));
                    column.Item().Element(c => ComposeWorkOrderTable(c, recap.Realization.WorkOrders));
                    column.Item().Element(c => ComposeSummary(c, recap.Realization));
                    column.Item().Element(c => ComposeReview(c, recap));
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(6);
                    text.CurrentPageNumber().FontSize(6);
                    text.Span(" / ").FontSize(6);
                    text.TotalPages().FontSize(6);
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(
        IContainer container,
        RecapWorkOrderDetailResponse recap,
        PdfReportMetadata metadata)
    {
        var header = recap.Plan.Header;

        container.Row(row =>
        {
            if (metadata.LogoData is not null)
                row.ConstantItem(120).Height(40).AlignLeft().AlignMiddle().Image(metadata.LogoData).FitArea();
            else
                row.ConstantItem(120);

            row.RelativeItem().AlignMiddle().Column(column =>
            {
                column.Item().AlignCenter().Text(metadata.CompanyName).Bold().FontSize(10);
                column.Item().AlignCenter().Text("Form Standar").Bold().FontSize(8);
                column.Item().AlignCenter().Text("Rekapitulasi Work Order").Bold().FontSize(8);
            });

            row.ConstantItem(210).AlignMiddle().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.42f);
                    columns.RelativeColumn(0.58f);
                });


                AddMetaRow(table, "Recap ID", $"{recap.Id}", true);
                AddMetaRow(table, "Tanggal", header.DocDate.ToString("dddd, dd MMMM yyyy", Indonesian));
                AddMetaRow(table, "Area", header.Location ?? "-");
                AddMetaRow(table, "Gudang", header.WarehouseCode);
                AddMetaRow(table, "Status", recap.RecapStatus);
            });
        });
    }

    private static void AddMetaRow(TableDescriptor table, string label, string value, bool highlight = false)
    {
        table.Cell().AlignRight().PaddingVertical(1)
            .Text($"{label} :").FontSize(6).Bold();

        var valueCell = table.Cell().AlignRight().PaddingVertical(1).PaddingHorizontal(3);
        if (highlight)
            valueCell = valueCell.Background(Highlight);
        valueCell.AlignRight().Text(value).FontSize(6).Bold();
    }

    private static void ComposeBudgetPlan(IContainer container, RecapWorkOrderDetailResponse recap)
    {
        var header = recap.Plan.Header;
        container.Column(column =>
        {
            column.Item().Element(c => SectionTitle(c, "BUDGET PLAN INFORMATION"));
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddInfoCell(table, "Budget No", header.BudgetNo);
                AddInfoCell(table, "Template Code", header.TemplateCode);
                AddInfoCell(table, "Budget Status", header.BudgetPlanStatus);
                AddInfoCell(table, "Warehouse", header.WarehouseName);
                AddInfoCell(table, "Document Date", header.DocDate.ToString("dd/MM/yyyy"));
                AddInfoCell(table, "Remark", header.Remark ?? "-");
                AddInfoCell(table, "Recap Status", recap.RecapStatus);
                AddInfoCell(table, "Rejection Reason", recap.RejectionReason ?? "-");
            });
        });
    }

    private static void AddInfoCell(TableDescriptor table, string label, string value)
    {
        table.Cell().Border(0.5f).BorderColor(BorderColor).Padding(4).Column(column =>
        {
            column.Item().Text(label).Bold().FontSize(5.5f);
            column.Item().PaddingTop(2).Text(value).FontSize(6.5f);
        });
    }

    private static void ComposeSpkTable(IContainer container, IReadOnlyList<RecapSpkDocumentResponse> rows)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionTitle(c, "BASE DOCUMENT / SPK"));
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(0.7f);
                });

                AddTableHeader(table, ["Type", "SPK No", "Document No", "BL No", "Item Code", "Item Name", "Qty", "Delivery", "UOM"]);

                if (rows.Count == 0)
                {
                    AddEmptyRow(table, 9);
                    return;
                }

                foreach (var row in rows)
                {
                    AddCell(table, row.SpkType);
                    AddCell(table, row.SpkNo);
                    AddCell(table, row.DocumentNo);
                    AddCell(table, row.BlNo ?? "-");
                    AddCell(table, row.ItemCode);
                    AddCell(table, row.ItemName);
                    AddCell(table, FormatNumber(row.Quantity), true);
                    AddCell(table, FormatNumber(row.DeliveryQty), true);
                    AddCell(table, row.UoM);
                }
            });
        });
    }

    private static void ComposeCostTable(IContainer container, IReadOnlyList<RecapCostDetailResponse> rows)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionTitle(c, "PLAN COST DETAILS"));
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(0.5f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(0.7f);
                    columns.RelativeColumn(0.6f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.1f);
                });

                AddTableHeader(table,
                    ["Type", "Vendor", "RFBA", "External Doc", "Cost", "COA", "BL No", "Unit Cost", "Qty", "UOM", "Description", "Total"]);

                if (rows.Count == 0)
                {
                    AddEmptyRow(table, 12);
                    return;
                }

                foreach (var row in rows)
                {
                    AddCell(table, row.Type);
                    AddCell(table, $"{row.VendorCode} - {row.VendorName}");
                    AddCell(table, row.IsRfba ? "Yes" : "No");
                    AddCell(table, row.DocExternal ?? "-");
                    AddCell(table, row.CostName);
                    AddCell(table, $"{row.CoaCode} - {row.CoaName}");
                    AddCell(table, row.BillOfLading ?? "-");
                    AddCell(table, FormatCurrency(row.UnitCost), true);
                    AddCell(table, FormatNumber(row.UnitCount), true);
                    AddCell(table, row.UomCode);
                    AddCell(table, row.Description ?? "-");
                    AddCell(table, FormatCurrency(row.TotalValue), true);
                }
            });
        });
    }

    private static void ComposeWorkOrderTable(IContainer container, IReadOnlyList<RecapWoItemResponse> rows)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionTitle(c, "WORK ORDER REALIZATION"));
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.1f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(0.6f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.1f);
                });

                AddTableHeader(table,
                    ["WO Code", "BL No", "PIC", "RFBA", "Start Date", "End Date", "Actual Cost", "Status", "Product", "Vehicle"]);

                if (rows.Count == 0)
                {
                    AddEmptyRow(table, 10);
                    return;
                }

                foreach (var row in rows)
                {
                    AddCell(table, row.WorkOrderCode);
                    AddCell(table, row.BlNumber ?? "-");
                    AddCell(table, row.PicName ?? "-");
                    AddCell(table, row.IsRfba ? "Yes" : "No");
                    AddCell(table, FormatDate(row.StartDate));
                    AddCell(table, FormatDate(row.EndDate));
                    AddCell(table, FormatCurrency(row.ActualCost), true);
                    AddCell(table, row.WorkOrderStatus);
                    AddCell(table, row.Product ?? "-");
                    AddCell(table, row.VehicleNo ?? "-");
                }
            });
        });
    }

    private static void ComposeSummary(IContainer container, RecapRealizationResponse realization)
    {
        container.AlignRight().Width(430).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            AddSummaryCell(table, "Budget Plan", FormatCurrency(realization.BudgetPlanTotal));
            AddSummaryCell(table, "Realization", FormatCurrency(realization.BudgetRealization));
            AddSummaryCell(table, "Variance", FormatCurrency(realization.BudgetVariance));
            AddSummaryCell(table, "Realization %", $"{realization.RealizationPercent:N2}%");
        });
    }

    private static void AddSummaryCell(TableDescriptor table, string label, string value)
    {
        table.Cell().Border(0.5f).BorderColor(BorderColor).Column(column =>
        {
            column.Item().Background(HeaderBg).Padding(3).AlignCenter()
                .Text(label).Bold().FontSize(6).FontColor(Colors.White);
            column.Item().Padding(4).AlignRight().Text(value).Bold().FontSize(7);
        });
    }

    private static void ComposeReview(IContainer container, RecapWorkOrderDetailResponse recap)
    {
        container.AlignRight().Width(190).Border(0.5f).BorderColor(BorderColor).Column(column =>
        {
            column.Item().Background(SectionBg).Padding(3).AlignCenter()
                .Text("Reviewed By").Bold().FontSize(7);
            column.Item().Height(35).AlignCenter().AlignMiddle()
                .Text(recap.ReviewedBy ?? "-").Bold().FontSize(7);
            column.Item().Padding(3).AlignCenter()
                .Text(PdfDates.SignatureDateLine(recap.ReviewedAt)).FontSize(6);
        });
    }

    private static void SectionTitle(IContainer container, string title) =>
        container.Background(SectionBg).Border(0.5f).BorderColor(BorderColor).Padding(3)
            .Text(title).Bold().FontSize(7);

    private static void AddTableHeader(TableDescriptor table, IReadOnlyList<string> headers)
    {
        table.Header(header =>
        {
            foreach (var text in headers)
            {
                header.Cell().Border(0.5f).BorderColor(BorderColor).Background(HeaderBg)
                    .PaddingVertical(3).PaddingHorizontal(2)
                    .Text(text).Bold().FontSize(5.5f).FontColor(Colors.White);
            }
        });
    }

    private static void AddCell(TableDescriptor table, string value, bool rightAlign = false)
    {
        var cell = table.Cell().Border(0.5f).BorderColor(BorderColor).PaddingVertical(3).PaddingHorizontal(2);
        if (rightAlign)
            cell.AlignRight().Text(value).FontSize(5.5f);
        else
            cell.Text(value).FontSize(5.5f);
    }

    private static void AddEmptyRow(TableDescriptor table, uint columns) =>
        table.Cell().ColumnSpan(columns).Border(0.5f).BorderColor(BorderColor).Padding(5)
            .AlignCenter().Text("No data available").Italic().FontSize(6);

    private static string FormatNumber(decimal? value) => value is null ? "-" : $"{value.Value:N2}";
    private static string FormatCurrency(decimal value) => $"Rp {value:N2}";
    private static string FormatDate(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm") ?? "-";
}
