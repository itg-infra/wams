namespace WAMS.Infrastructure.Tests.Services;

using MimeDetective;
using MimeDetective.Definitions;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Infrastructure.Services.Files;
using Xunit;

public sealed class FileMimeDetectorTests
{
    private static readonly IContentInspector Inspector = new ContentInspectorBuilder
    {
        Definitions = DefaultDefinitions.All()
    }.Build();

    private static FileMimeDetector BuildDetector(IEnumerable<string>? allowed = null)
    {
        var opts = new FileAttachmentOptions();
        if (allowed is not null)
            opts.AllowedMimeTypes = allowed.ToList();
        return new FileMimeDetector(Inspector, Options.Create(opts));
    }

    // PNG magic: 89 50 4E 47 0D 0A 1A 0A
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // JPEG magic: FF D8 FF
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];

    // PDF magic: %PDF
    private static readonly byte[] PdfHeader = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];

    // ZIP-based (used for docx/xlsx): 50 4B 03 04
    private static readonly byte[] ZipHeader = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00];

    [Fact]
    public void Detect_PngBytes_ReturnsImagePng()
    {
        var sut = BuildDetector();
        var result = sut.Detect(PngHeader, PngHeader.Length);
        Assert.Equal("image/png", result);
    }

    [Fact]
    public void Detect_JpegBytes_ReturnsImageJpeg()
    {
        var sut = BuildDetector();
        var result = sut.Detect(JpegHeader, JpegHeader.Length);
        Assert.Equal("image/jpeg", result);
    }

    [Fact]
    public void Detect_PdfBytes_ReturnsApplicationPdf()
    {
        var sut = BuildDetector();
        var result = sut.Detect(PdfHeader, PdfHeader.Length);
        Assert.Equal("application/pdf", result);
    }

    [Fact]
    public void Detect_ZipBytes_ReturnsNullWhenZipNotInAllowedList()
    {
        // ZIP base type is not in the default allowed list
        var sut = BuildDetector();
        var result = sut.Detect(ZipHeader, ZipHeader.Length);
        // application/zip is not in AllowedMimeTypes; only docx/xlsx are.
        // MimeDetective may return application/zip for raw ZIP - should be null.
        Assert.Null(result);
    }

    [Fact]
    public void Detect_UnknownBytes_ReturnsNull()
    {
        var sut = BuildDetector();
        var garbage = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        var result = sut.Detect(garbage, garbage.Length);
        Assert.Null(result);
    }

    [Fact]
    public void Detect_KnownTypeNotInAllowedList_ReturnsNull()
    {
        // Build detector with empty allowed list
        var sut = BuildDetector(allowed: []);
        var result = sut.Detect(PngHeader, PngHeader.Length);
        Assert.Null(result);
    }

    [Fact]
    public void Detect_PartialHeader_DoesNotThrow()
    {
        var sut = BuildDetector();
        var ex = Record.Exception(() => sut.Detect(PngHeader, 4));
        Assert.Null(ex);
    }
}
