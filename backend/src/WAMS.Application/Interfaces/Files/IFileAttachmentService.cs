namespace WAMS.Application.Interfaces.Files;

using WAMS.Application.DTOs.Files;

public interface IFileAttachmentService
{
    Task<List<FileAttachmentResponse>> UploadAsync(long userId, FileUploadRequest request, CancellationToken ct = default);
    Task<List<FileAttachmentResponse>> GetByEntityAsync(long userId, string entityType, long entityId, CancellationToken ct = default);
    Task<FileDownloadResponse> DownloadAsync(long userId, string entityType, long entityId, long fileId, CancellationToken ct = default);
    Task DeleteAsync(long userId, string entityType, long entityId, long fileId, CancellationToken ct = default);
}
