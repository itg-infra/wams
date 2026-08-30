namespace WAMS.Application.Interfaces.Files;

using WAMS.Domain.Entities.Files;

public interface IFileAttachmentRepository
{
    Task CreateAsync(FileAttachment attachment, CancellationToken ct = default);
    Task CreateManyAsync(IEnumerable<FileAttachment> attachments, CancellationToken ct = default);
    Task<int> CountByEntityAsync(string entityType, long entityId, CancellationToken ct = default);
    Task<long> SumSizeByEntityAsync(string entityType, long entityId, CancellationToken ct = default);
    Task<List<FileAttachment>> GetByEntityAsync(string entityType, long entityId, CancellationToken ct = default);
    Task<List<FileAttachment>> GetByIdsAsync(IReadOnlyCollection<long> ids, string entityType, long entityId, CancellationToken ct = default);
    Task<FileAttachment?> GetByIdAsync(long id, string entityType, long entityId, CancellationToken ct = default);
    Task DeleteAsync(FileAttachment attachment, CancellationToken ct = default);
}
