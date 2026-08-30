namespace WAMS.Application.Interfaces.Rca;

using WAMS.Application.DTOs.Rca;

public interface IRcaPdfRenderer
{
    byte[] Render(RcaDocument document);
}
