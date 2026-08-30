namespace WAMS.Infrastructure.Repositories.Files;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.Files;
using WAMS.Domain.Entities.Files;
using WAMS.Infrastructure.Data;

public sealed class FileAttachmentRepository(AppDbContext db) : IFileAttachmentRepository
{
    public Task CreateAsync(FileAttachment attachment, CancellationToken ct = default)
    {
        db.FileAttachments.Add(attachment);
        return Task.CompletedTask;
    }

    public Task CreateManyAsync(IEnumerable<FileAttachment> attachments, CancellationToken ct = default)
    {
        db.FileAttachments.AddRange(attachments);
        return Task.CompletedTask;
    }

    public Task<int> CountByEntityAsync(string entityType, long entityId, CancellationToken ct = default)
        => db.FileAttachments.CountAsync(
            x => x.EntityType == entityType && x.EntityId == entityId,
            ct);

    public async Task<long> SumSizeByEntityAsync(string entityType, long entityId, CancellationToken ct = default)
        => await db.FileAttachments
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .SumAsync(x => (long?)x.FileSize, ct) ?? 0;

    public Task<List<FileAttachment>> GetByEntityAsync(string entityType, long entityId, CancellationToken ct = default)
        => db.FileAttachments
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .Include(x => x.UploadedBy)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<List<FileAttachment>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        string entityType,
        long entityId,
        CancellationToken ct = default
    )
        => db.FileAttachments
            .Where(x => ids.Contains(x.Id) && x.EntityType == entityType && x.EntityId == entityId)
            .Include(x => x.UploadedBy)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public Task<FileAttachment?> GetByIdAsync(
        long id,
        string entityType,
        long entityId,
        CancellationToken ct = default
    )
        => db.FileAttachments
            .Include(x => x.UploadedBy)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.EntityType == entityType && x.EntityId == entityId,
                ct);

    public Task DeleteAsync(FileAttachment attachment, CancellationToken ct = default)
    {
        db.FileAttachments.Remove(attachment);
        return Task.CompletedTask;
    }
}
