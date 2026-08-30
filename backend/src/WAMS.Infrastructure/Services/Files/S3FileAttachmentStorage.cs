namespace WAMS.Infrastructure.Services.Files;

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Files;
using WAMS.Domain.Constants;
using WAMS.Domain.Exceptions;

public sealed class S3FileAttachmentStorage(IAmazonS3 client, IOptions<ObjectStorageOptions> options) : IFileAttachmentStorage
{
    private readonly string _bucket = options.Value.BucketName;

    public async Task SaveAsync(Stream content, string storageKey, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };
        await client.PutObjectAsync(request, ct);
    }

    public async Task<StoredFileStream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            var request = new GetObjectRequest { BucketName = _bucket, Key = storageKey };
            var response = await client.GetObjectAsync(request, ct);
            try
            {
                var lastModified = response.LastModified is DateTime lm
                    ? new DateTimeOffset(lm, TimeSpan.Zero)
                    : (DateTimeOffset?)null;
                return new StoredFileStream(response.ResponseStream, lastModified);
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(ErrorMessages.FileAttachment.StoredFileNotFound);
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest { BucketName = _bucket, Key = storageKey };
        await client.DeleteObjectAsync(request, ct);
    }
}
