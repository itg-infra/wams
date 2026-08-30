namespace WAMS.Application.Interfaces.PurchaseOrders;

using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;

public interface IPurchaseOrderPdfRenderer
{
    byte[] Render(PurchaseOrderResponse po, PdfReportMetadata metadata);
}
