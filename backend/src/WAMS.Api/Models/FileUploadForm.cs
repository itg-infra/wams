namespace WAMS.Api.Models;

using Microsoft.AspNetCore.Http;

public sealed class FileUploadForm
{
    public List<IFormFile>? Files { get; set; }
}
