namespace WAMS.Api.Controllers.Files;

using WAMS.Api.Controllers.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using WAMS.Api.Models;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Files;
using WAMS.Application.Interfaces.Files;
using WAMS.Domain.Constants;

[ApiController]
[Route("api/v1/files/{entityType}/{entityId:long}")]
[Authorize]
public sealed class FilesController(IFileAttachmentService fileAttachmentService) : BaseController
{
    /// <summary>Uploads one or more files attached to an entity.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<List<FileAttachmentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(
        string entityType,
        long entityId,
        [FromForm] FileUploadForm form,
        CancellationToken ct)
    {
        var files = form.Files?
            .Select(f => (IUploadFile)new FormFileUpload(f))
            .ToList() ?? [];

        var request = new FileUploadRequest(entityType, entityId, files);
        var result = await fileAttachmentService.UploadAsync(GetUserId(), request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.File.Uploaded
        ));
    }

    /// <summary>Lists all files attached to an entity.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FileAttachmentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(string entityType, long entityId, CancellationToken ct)
    {
        var result = await fileAttachmentService.GetByEntityAsync(GetUserId(), entityType, entityId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.File.Retrieved
        ));
    }

    /// <summary>Downloads a file attached to an entity, with range support.</summary>
    [HttpGet("{fileId:long}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string entityType, long entityId, long fileId, CancellationToken ct)
    {
        var file = await fileAttachmentService.DownloadAsync(GetUserId(), entityType, entityId, fileId, ct);
        var result = File(file.Content, file.ContentType, file.OriginalFileName);

        result.EnableRangeProcessing = true;
        result.LastModified = file.LastModifiedUtc;
        result.EntityTag = BuildEntityTag(file);

        Response.Headers.CacheControl = "private, no-store";

        return result;
    }

    /// <summary>Deletes a file attached to an entity.</summary>
    [HttpDelete("{fileId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string entityType, long entityId, long fileId, CancellationToken ct)
    {
        await fileAttachmentService.DeleteAsync(GetUserId(), entityType, entityId, fileId, ct);

        return NoContent();
    }

    private static EntityTagHeaderValue BuildEntityTag(FileDownloadResponse file)
    {
        var raw = $"{file.Id}:{file.FileSize}:{file.UploadedAt.Ticks}";
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(raw))
        );

        return new EntityTagHeaderValue($"\"{hash}\"");
    }
}
