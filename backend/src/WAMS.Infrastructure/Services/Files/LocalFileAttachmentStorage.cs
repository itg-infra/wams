namespace WAMS.Infrastructure.Services.Files;

using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Files;
using WAMS.Domain.Exceptions;

public sealed class LocalFileAttachmentStorage(IOptions<FileAttachmentOptions> options) : IFileAttachmentStorage
{
    private readonly string _rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task SaveAsync(Stream content, string storageKey, string contentType, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(storageKey);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("Storage directory could not be resolved");

        Directory.CreateDirectory(directory);

        content.Position = 0;
        await using var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, ct);
    }

    public Task<StoredFileStream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
            throw new NotFoundException("Stored file not found");

        var fileInfo = new FileInfo(fullPath);
        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        return Task.FromResult(new StoredFileStream(
            stream,
            fileInfo.LastWriteTimeUtc));
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var normalizedKey = storageKey.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var combinedPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));
        if (!combinedPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Invalid storage key");

        return combinedPath;
    }
}
