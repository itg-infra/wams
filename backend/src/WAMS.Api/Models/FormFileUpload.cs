namespace WAMS.Api.Models;

using Microsoft.AspNetCore.Http;
using WAMS.Application.Interfaces.Files;

public sealed class FormFileUpload(IFormFile formFile) : IUploadFile
{
    public string FileName => formFile.FileName;
    public string ContentType => formFile.ContentType;
    public long Length => formFile.Length;

    public Task<Stream> OpenReadStreamAsync(CancellationToken ct = default)
        => Task.FromResult(formFile.OpenReadStream());
}
