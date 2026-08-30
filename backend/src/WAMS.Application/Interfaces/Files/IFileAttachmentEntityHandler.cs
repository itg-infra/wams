namespace WAMS.Application.Interfaces.Files;

public interface IFileAttachmentEntityHandler
{
    string EntityType { get; }
    Task<FileAttachmentEntityContext?> ResolveAsync(long userId, long entityId, CancellationToken ct = default);
}
