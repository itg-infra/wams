namespace WAMS.Infrastructure.Services.Files;

using WAMS.Application.Interfaces.Files;

public sealed class FileAttachmentEntityResolver(IEnumerable<IFileAttachmentEntityHandler> handlers) : IFileAttachmentEntityResolver
{
    private readonly IReadOnlyDictionary<string, IFileAttachmentEntityHandler> _handlers = handlers
        .ToDictionary(x => x.EntityType, StringComparer.OrdinalIgnoreCase);

    public async Task<FileAttachmentEntityContext?> ResolveAsync(long userId, string entityType, long entityId, CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(entityType, out var handler))
            return null;

        return await handler.ResolveAsync(userId, entityId, ct);
    }
}
