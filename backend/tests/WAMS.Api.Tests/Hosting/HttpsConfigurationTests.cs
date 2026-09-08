using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WAMS.Api.Hosting;
using Xunit;

namespace WAMS.Api.Tests.Hosting;

public class HttpsConfigurationTests
{
    [Fact]
    public void Read_HttpsDisabled_UsesHttpAndConfiguredPort()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PORT"] = "8121"
        });

        var result = HttpsConfiguration.Read(configuration);

        result.Enabled.Should().BeFalse();
        result.Port.Should().Be(8121);
    }

    [Fact]
    public void Read_HttpsEnabled_RequiresCertificatePath()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["HTTPS"] = "true",
            ["HTTPS_CERT_PASSWORD"] = "secret"
        });

        var action = () => HttpsConfiguration.Read(configuration);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("HTTPS_CERT_PATH is required when HTTPS=true");
    }

    [Fact]
    public void Read_HttpsEnabled_RequiresCertificatePassword()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["HTTPS"] = "true",
            ["HTTPS_CERT_PATH"] = "C:/WAMS/certificates/wams.pfx"
        });

        var action = () => HttpsConfiguration.Read(configuration);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("HTTPS_CERT_PASSWORD is required when HTTPS=true");
    }

    [Fact]
    public void Read_HttpsEnabled_ReturnsCertificateConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PORT"] = "8121",
            ["HTTPS"] = "true",
            ["HTTPS_CERT_PATH"] = "C:/WAMS/certificates/wams.pfx",
            ["HTTPS_CERT_PASSWORD"] = "secret"
        });

        var result = HttpsConfiguration.Read(configuration);

        result.Enabled.Should().BeTrue();
        result.Port.Should().Be(8121);
        result.CertificatePath.Should().Be("C:/WAMS/certificates/wams.pfx");
        result.CertificatePassword.Should().Be("secret");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
