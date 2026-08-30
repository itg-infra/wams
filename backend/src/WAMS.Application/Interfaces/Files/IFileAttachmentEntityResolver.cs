namespace WAMS.Application.Interfaces.Files;

public interface IFileAttachmentEntityResolver
{
    Task<FileAttachmentEntityContext?> ResolveAsync(long userId, string entityType, long entityId, CancellationToken ct = default);
}

public sealed record FileAttachmentEntityContext(
    string EntityType,
    long EntityId,
    long CompanyId)
{
    // Entity owner (creator). When set, DeleteAsync allows owner to delete any attachment on the entity,
    // not just ones they personally uploaded.
    public long? OwnerUserId { get; init; }

    // False when the entity's state prohibits adding or removing attachments (e.g. submitted/approved).
    // Read operations (list, download) are always permitted regardless of this flag.
    public bool CanModify { get; init; } = true;
}
