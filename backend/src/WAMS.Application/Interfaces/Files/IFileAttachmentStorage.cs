namespace WAMS.Application.Interfaces.Files;

public interface IFileAttachmentStorage
{
    Task SaveAsync(Stream content, string storageKey, string contentType, CancellationToken ct = default);
    Task<StoredFileStream> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}

public sealed record StoredFileStream(
    Stream Content,
    DateTimeOffset? LastModifiedUtc
);
