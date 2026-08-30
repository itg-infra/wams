namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Files;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Files;
using WAMS.Application.Services.Files;
using WAMS.Domain.Entities.Files;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Exceptions;
using Xunit;
using DomainValidationException = WAMS.Domain.Exceptions.ValidationException;

public sealed class FileAttachmentServiceTests
{
    private readonly IFileAttachmentRepository _attachmentRepository = Substitute.For<IFileAttachmentRepository>();
    private readonly IFileAttachmentStorage _storage = Substitute.For<IFileAttachmentStorage>();
    private readonly IFileAttachmentEntityResolver _entityResolver = Substitute.For<IFileAttachmentEntityResolver>();
    private readonly IFileMimeDetector _mimeDetector = Substitute.For<IFileMimeDetector>();
    private readonly IValidator<FileUploadRequest> _uploadValidator = Substitute.For<IValidator<FileUploadRequest>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FileAttachmentService _sut;

    public FileAttachmentServiceTests()
    {
        _sut = new FileAttachmentService(
            _attachmentRepository,
            _storage,
            _entityResolver,
            _mimeDetector,
            _uploadValidator,
            _unitOfWork,
            NullLogger<FileAttachmentService>.Instance,
            Options.Create(new FileAttachmentOptions()));
    }

    // --- UploadAsync ---

    [Fact]
    public async Task UploadAsync_WhenEntityDoesNotExist_ThrowsNotFound()
    {
        var request = BuildRequest(1);
        SetupValidationPass(request);
        _entityResolver.ResolveAsync(5L, "budget-plans", 10, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = () => _sut.UploadAsync(5, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UploadAsync_WhenAttachmentLimitReached_ThrowsValidation()
    {
        var request = BuildRequest(2);
        SetupValidationPass(request);
        _entityResolver.ResolveAsync(5L, "budget-plans", 10, Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentEntityContext("budget-plans", 10, 1));
        _attachmentRepository.CountByEntityAsync("budget-plans", 10, Arg.Any<CancellationToken>())
            .Returns(9); // 9 existing + 2 new = 11 > 10

        var act = () => _sut.UploadAsync(5, request);

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task UploadAsync_WhenEntityCannotBeModified_ThrowsForbidden()
    {
        var request = BuildRequest(1);
        SetupValidationPass(request);
        _entityResolver.ResolveAsync(5L, "budget-plans", 10, Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentEntityContext("budget-plans", 10, 1) { CanModify = false });

        var act = () => _sut.UploadAsync(5, request);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _storage.DidNotReceive().SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WhenSignatureCheckFails_ThrowsValidationAndDoesNotStore()
    {
        var request = BuildRequest(1);
        SetupValidationPass(request);
        SetupEntityContext();
        _mimeDetector.Detect(Arg.Any<byte[]>(), Arg.Any<int>())
            .Returns((string?)null);

        var act = () => _sut.UploadAsync(5, request);

        await act.Should().ThrowAsync<DomainValidationException>();
        await _storage.DidNotReceive().SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WhenStorageFailsOnSecondFile_DeletesFirstFileAndThrows()
    {
        var request = BuildRequest(2);
        SetupValidationPass(request);
        SetupEntityContext();
        _mimeDetector.Detect(Arg.Any<byte[]>(), Arg.Any<int>())
            .Returns("application/pdf");

        string? firstStoredKey = null;
        _storage
            .When(x => x.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(ci =>
            {
                if (firstStoredKey is null)
                    firstStoredKey = ci.ArgAt<string>(1);
                else
                    throw new IOException("disk full");
            });

        var act = () => _sut.UploadAsync(5, request);

        await act.Should().ThrowAsync<IOException>();
        await _storage.Received(1).DeleteAsync(firstStoredKey!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WhenDbCommitFails_DeletesAllStoredFilesAndThrows()
    {
        var request = BuildRequest(2);
        SetupValidationPass(request);
        SetupEntityContext();
        _mimeDetector.Detect(Arg.Any<byte[]>(), Arg.Any<int>())
            .Returns("application/pdf");
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("db down"));

        var act = () => _sut.UploadAsync(5, request);

        await act.Should().ThrowAsync<Exception>();
        await _storage.Received(2).DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WithValidFiles_CallsCreateManyAndCommitsOnce()
    {
        var request = BuildRequest(2);
        SetupValidationPass(request);
        SetupEntityContext();
        _mimeDetector.Detect(Arg.Any<byte[]>(), Arg.Any<int>())
            .Returns("application/pdf");

        var storedAttachments = new List<FileAttachment>();
        _attachmentRepository
            .When(x => x.CreateManyAsync(Arg.Any<IEnumerable<FileAttachment>>(), Arg.Any<CancellationToken>()))
            .Do(ci =>
            {
                var batch = ci.ArgAt<IEnumerable<FileAttachment>>(0).ToList();
                for (var i = 0; i < batch.Count; i++)
                    batch[i].Id = i + 1;
                storedAttachments.AddRange(batch);
            });
        _attachmentRepository
            .GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), "budget-plans", 10, Arg.Any<CancellationToken>())
            .Returns(ci => storedAttachments.Select(a => new FileAttachment
            {
                Id = a.Id,
                CompanyId = a.CompanyId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                StorageKey = a.StorageKey,
                UploadedByUserId = a.UploadedByUserId,
                UploadedBy = new User { Fullname = "Test User" }
            }).ToList());

        var result = await _sut.UploadAsync(5, request, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        await _attachmentRepository.Received(1).CreateManyAsync(Arg.Any<IEnumerable<FileAttachment>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_WhenStorageDeleteFails_DoesNotThrow()
    {
        _entityResolver.ResolveAsync(5L, "budget-plans", 10, Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentEntityContext("budget-plans", 10, 1));
        _attachmentRepository.GetByIdAsync(7, "budget-plans", 10, Arg.Any<CancellationToken>())
            .Returns(new FileAttachment
            {
                Id = 7,
                CompanyId = 1,
                EntityType = "budget-plans",
                EntityId = 10,
                OriginalFileName = "quote.pdf",
                ContentType = "application/pdf",
                FileSize = 12,
                StorageKey = "budget-plans/10/test.pdf",
                UploadedByUserId = 5
            });
        _storage.When(x => x.DeleteAsync("budget-plans/10/test.pdf", Arg.Any<CancellationToken>()))
            .Do(_ => throw new IOException("disk issue"));

        var act = () => _sut.DeleteAsync(5, "budget-plans", 10, 7);

        await act.Should().NotThrowAsync();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- Helpers ---

    private static FileUploadRequest BuildRequest(int fileCount)
    {
        var files = Enumerable.Range(0, fileCount)
            .Select(_ => (IUploadFile)new StubUploadFile())
            .ToList();
        return new FileUploadRequest("budget-plans", 10, files);
    }

    private void SetupValidationPass(FileUploadRequest request)
        => _uploadValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

    private void SetupEntityContext()
    {
        _entityResolver.ResolveAsync(5L, "budget-plans", 10, Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentEntityContext("budget-plans", 10, 1));
        _attachmentRepository.CountByEntityAsync("budget-plans", 10, Arg.Any<CancellationToken>())
            .Returns(0);
    }

    private sealed class StubUploadFile : IUploadFile
    {
        public string FileName => "quote.pdf";
        public string ContentType => "application/pdf";
        public long Length => 1024;

        public Task<Stream> OpenReadStreamAsync(CancellationToken ct = default)
            => Task.FromResult<Stream>(new MemoryStream("%PDF-1.4"u8.ToArray()));
    }
}
