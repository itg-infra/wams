namespace WAMS.Application.Interfaces.Rfba;

using WAMS.Application.DTOs.Rfba;
using WAMS.Application.Export;

public interface IRfbaFormPdfRenderer
{
    byte[] Render(RfbaFormDocument document, PdfReportMetadata metadata);
}
