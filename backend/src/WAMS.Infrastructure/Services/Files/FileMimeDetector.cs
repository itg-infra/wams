namespace WAMS.Infrastructure.Services.Files;

using MimeDetective;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Files;

public sealed class FileMimeDetector(
    IContentInspector inspector,
    IOptions<FileAttachmentOptions> options) : IFileMimeDetector
{
    private readonly HashSet<string> _allowed =
        new(options.Value.AllowedMimeTypes, StringComparer.OrdinalIgnoreCase);

    public string? Detect(byte[] header, int headerLength)
    {
        var slice = headerLength < header.Length ? header[..headerLength] : header;
        var results = inspector.Inspect(slice);
        var mime = results.ByMimeType().FirstOrDefault()?.MimeType;
        return mime is not null && _allowed.Contains(mime) ? mime : null;
    }
}
