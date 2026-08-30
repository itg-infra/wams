namespace WAMS.Application.Interfaces.Rca;

using WAMS.Application.DTOs.Rca;

public interface IRcaService
{
    Task<RcaDocument> GetDocumentAsync(
        RcaQuery query,
        long userId,
        CancellationToken ct = default);
}
