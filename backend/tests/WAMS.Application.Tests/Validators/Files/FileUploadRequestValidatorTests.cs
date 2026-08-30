namespace WAMS.Application.Tests.Validators;

using FluentAssertions;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Files;
using WAMS.Application.Interfaces.Files;
using WAMS.Application.Validators.Files;
using Xunit;

public sealed class FileUploadRequestValidatorTests
{
    private readonly FileUploadRequestValidator _validator = new(
        Options.Create(new FileAttachmentOptions()));

    [Fact]
    public void Validate_WithNullFiles_IsInvalid()
    {
        var result = _validator.Validate(new FileUploadRequest("budget-plans", 1, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Files");
    }

    [Fact]
    public void Validate_WithEmptyFilesList_IsInvalid()
    {
        var result = _validator.Validate(new FileUploadRequest("budget-plans", 1, []));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Files");
    }

    [Fact]
    public void Validate_WithTooManyFiles_IsInvalid()
    {
        var options = Options.Create(new FileAttachmentOptions { MaxAttachmentsPerEntity = 2 });
        var validator = new FileUploadRequestValidator(options);
        var files = Enumerable.Range(0, 3)
            .Select(_ => (IUploadFile)new StubUploadFile("f.pdf", "application/pdf", 100))
            .ToList();

        var result = validator.Validate(new FileUploadRequest("budget-plans", 1, files));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Files");
    }

    [Fact]
    public void Validate_WithOversizedFile_IsInvalid()
    {
        var options = Options.Create(new FileAttachmentOptions { MaxFileSizeBytes = 10 });
        var validator = new FileUploadRequestValidator(options);
        var request = new FileUploadRequest("budget-plans", 1,
            [new StubUploadFile("quote.pdf", "application/pdf", 11)]);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Files[0].Length");
    }

    [Fact]
    public void Validate_WithDisallowedMimeType_IsInvalid()
    {
        var request = new FileUploadRequest("budget-plans", 1,
            [new StubUploadFile("script.exe", "application/x-msdownload", 100)]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Files[0].ContentType");
    }

    [Fact]
    public void Validate_WithValidSingleFile_IsValid()
    {
        var request = new FileUploadRequest("budget-plans", 1,
            [new StubUploadFile("quote.pdf", "application/pdf", 1024)]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMultipleValidFiles_IsValid()
    {
        var request = new FileUploadRequest("budget-plans", 1,
        [
            new StubUploadFile("quote.pdf", "application/pdf", 1024),
            new StubUploadFile("invoice.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 2048)
        ]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidEntityType_IsInvalid()
    {
        var request = new FileUploadRequest("../budget-plans", 1,
            [new StubUploadFile("quote.pdf", "application/pdf", 1024)]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "EntityType");
    }

    [Fact]
    public void Validate_WithZeroLengthFile_IsInvalid()
    {
        var request = new FileUploadRequest("budget-plans", 1,
            [new StubUploadFile("empty.pdf", "application/pdf", 0)]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Files[0].Length");
    }

    [Fact]
    public void Validate_WithEmptyFileName_IsInvalid()
    {
        var request = new FileUploadRequest("budget-plans", 1,
            [new StubUploadFile("", "application/pdf", 1024)]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Files[0].FileName");
    }

    [Fact]
    public void Validate_WithEmptyContentType_IsInvalid()
    {
        var request = new FileUploadRequest("budget-plans", 1,
            [new StubUploadFile("quote.pdf", "", 1024)]);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Files[0].ContentType");
    }

    private sealed class StubUploadFile(string fileName, string contentType, long length) : IUploadFile
    {
        public string FileName => fileName;
        public string ContentType => contentType;
        public long Length => length;

        public Task<Stream> OpenReadStreamAsync(CancellationToken ct = default)
            => Task.FromResult<Stream>(new MemoryStream());
    }
}
