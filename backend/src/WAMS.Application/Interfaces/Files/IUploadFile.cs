namespace WAMS.Application.Interfaces.Files;

public interface IUploadFile
{
    string FileName { get; }
    string ContentType { get; }
    long Length { get; }
    Task<Stream> OpenReadStreamAsync(CancellationToken ct = default);
}
