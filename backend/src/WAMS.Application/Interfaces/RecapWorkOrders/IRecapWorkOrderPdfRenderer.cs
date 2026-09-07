namespace WAMS.Application.Interfaces.RecapWorkOrders;

using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Application.Export;

public interface IRecapWorkOrderPdfRenderer
{
    byte[] Render(RecapWorkOrderDetailResponse recap, PdfReportMetadata metadata);
}
