namespace WAMS.Infrastructure.Tests.Services;

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WAMS.Application.Common;
using WAMS.Domain.Exceptions;
using WAMS.Infrastructure.Services.Files;
using Xunit;

public sealed class S3FileAttachmentStorageTests
{
    private readonly IAmazonS3 _s3 = Substitute.For<IAmazonS3>();
    private readonly S3FileAttachmentStorage _sut;
    private const string Bucket = "wams-test";

    public S3FileAttachmentStorageTests()
    {
        var opts = Options.Create(new ObjectStorageOptions
        {
            Endpoint = "https://s3.test",
            AccessKey = "key",
            SecretKey = "secret",
            BucketName = Bucket,
            Region = "us-east-1",
            ForcePathStyle = true
        });
        _sut = new S3FileAttachmentStorage(_s3, opts);
    }

    [Fact]
    public async Task SaveAsync_CallsPutObject_WithCorrectBucketKeyAndContentType()
    {
        var content = new MemoryStream("hello"u8.ToArray());
        _s3.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        await _sut.SaveAsync(content, "entities/1/abc.pdf", "application/pdf", TestContext.Current.CancellationToken);

        await _s3.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(r =>
                r.BucketName == Bucket &&
                r.Key == "entities/1/abc.pdf" &&
                r.ContentType == "application/pdf"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsStoredFileStream_WithLastModified()
    {
        var lastModified = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var bodyBytes = "file content"u8.ToArray();
        _s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetObjectResponse
            {
                ResponseStream = new MemoryStream(bodyBytes),
                LastModified = lastModified,
                HttpStatusCode = HttpStatusCode.OK
            });

        var result = await _sut.OpenReadAsync("entities/1/abc.pdf", TestContext.Current.CancellationToken);

        result.LastModifiedUtc.Should().Be(new DateTimeOffset(lastModified, TimeSpan.Zero));
        var buffer = new byte[bodyBytes.Length];
        await result.Content.ReadExactlyAsync(buffer, TestContext.Current.CancellationToken);
        buffer.Should().Equal(bodyBytes);
    }

    [Fact]
    public async Task OpenReadAsync_ThrowsNotFoundException_WhenObjectDoesNotExist()
    {
        var notFound = new AmazonS3Exception("The specified key does not exist.")
        {
            StatusCode = HttpStatusCode.NotFound
        };
        _s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(notFound);

        var act = async () => await _sut.OpenReadAsync("entities/1/missing.pdf");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task OpenReadAsync_Rethrows_NonNotFoundS3Exceptions()
    {
        var serverError = new AmazonS3Exception("Internal Server Error")
        {
            StatusCode = HttpStatusCode.InternalServerError
        };
        _s3.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(serverError);

        var act = async () => await _sut.OpenReadAsync("entities/1/abc.pdf");

        await act.Should().ThrowAsync<AmazonS3Exception>();
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleteObject_WithCorrectBucketAndKey()
    {
        _s3.DeleteObjectAsync(Arg.Any<DeleteObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

        await _sut.DeleteAsync("entities/1/abc.pdf", TestContext.Current.CancellationToken);

        await _s3.Received(1).DeleteObjectAsync(
            Arg.Is<DeleteObjectRequest>(r =>
                r.BucketName == Bucket &&
                r.Key == "entities/1/abc.pdf"),
            Arg.Any<CancellationToken>());
    }
}
