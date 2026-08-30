namespace WAMS.Api.Tests.Controllers;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using WAMS.Api.Controllers.Companies;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Companies;
using Xunit;

public class CompaniesControllerTests
{
    private readonly ICompanyService _companyService = Substitute.For<ICompanyService>();
    private readonly IExportService _exportService = Substitute.For<IExportService>();
    private readonly IOptions<ExportOptions> _exportOptions = Substitute.For<IOptions<ExportOptions>>();
    private readonly CompaniesController _sut;

    public CompaniesControllerTests()
    {
        _exportOptions.Value.Returns(new ExportOptions { MaxRows = 1000 });
        _sut = new CompaniesController(_companyService, _exportService, _exportOptions);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([], "jwt"))
            }
        };
    }

    private static IFormFile BuildFormFile(string contentType, long sizeBytes)
    {
        var file = Substitute.For<IFormFile>();
        file.ContentType.Returns(contentType);
        file.Length.Returns(sizeBytes);
        file.OpenReadStream().Returns(new MemoryStream(new byte[(int)sizeBytes]));
        return file;
    }

    [Fact]
    public async Task UploadLogo_ValidPng_Returns200()
    {
        var file = BuildFormFile("image/png", 1024);

        var result = await _sut.UploadLogo(1, file, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _companyService.Received(1).UploadLogoAsync(1, Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogo_NullFile_Returns400()
    {
        var result = await _sut.UploadLogo(1, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _companyService.DidNotReceive().UploadLogoAsync(Arg.Any<long>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogo_OversizeFile_Returns400()
    {
        var file = BuildFormFile("image/png", 3 * 1024 * 1024); // 3 MB

        var result = await _sut.UploadLogo(1, file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _companyService.DidNotReceive().UploadLogoAsync(Arg.Any<long>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadLogo_InvalidContentType_Returns400()
    {
        var file = BuildFormFile("text/plain", 1024);

        var result = await _sut.UploadLogo(1, file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _companyService.DidNotReceive().UploadLogoAsync(Arg.Any<long>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveLogo_Returns204()
    {
        var result = await _sut.RemoveLogo(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _companyService.Received(1).RemoveLogoAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLogo_WithExistingLogo_ReturnsFileWithCorrectContentType()
    {
        var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47]);
        _companyService.GetLogoAsync(1, Arg.Any<CancellationToken>())
            .Returns(((Stream)stream, "image/png"));

        var result = await _sut.GetLogo(1, CancellationToken.None);

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("image/png");
        await _companyService.Received(1).GetLogoAsync(1, Arg.Any<CancellationToken>());
    }
}
