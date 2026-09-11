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
    public void Read_HttpsEnabled_AllowsCertificateWithoutPassword()
    {
        var certificatePath = Path.GetTempFileName() + ".pfx";
        File.WriteAllText(certificatePath, "certificate");

        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["HTTPS"] = "true",
            ["HTTPS_CERT_PATH"] = certificatePath
        });

        try
        {
            var result = HttpsConfiguration.Read(configuration);

            result.Enabled.Should().BeTrue();
            result.CertificatePath.Should().Be(certificatePath);
            result.CertificatePassword.Should().BeNull();
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    [Fact]
    public void Read_HttpsEnabled_ReturnsCertificateConfiguration()
    {
        var certificatePath = Path.GetTempFileName() + ".pfx";
        File.WriteAllText(certificatePath, "certificate");

        try
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["PORT"] = "8121",
                ["HTTPS"] = "true",
                ["HTTPS_CERT_PATH"] = certificatePath,
                ["HTTPS_CERT_PASSWORD"] = "secret"
            });

            var result = HttpsConfiguration.Read(configuration);

            result.Enabled.Should().BeTrue();
            result.Port.Should().Be(8121);
            result.CertificatePath.Should().Be(certificatePath);
            result.CertificatePassword.Should().Be("secret");
        }
        finally
        {
            File.Delete(certificatePath);
        }
    }

    [Fact]
    public void Read_HttpsEnabled_UsesOnlyPfxInDirectory()
    {
        var directory = CreateTempDirectory();
        var certificatePath = Path.Combine(directory, "wams.pfx");
        File.WriteAllText(certificatePath, "certificate");

        try
        {
            var result = HttpsConfiguration.Read(BuildConfiguration(new Dictionary<string, string?>
            {
                ["HTTPS"] = "true",
                ["HTTPS_CERT_PATH"] = directory,
                ["HTTPS_CERT_PASSWORD"] = "secret"
            }));

            result.CertificatePath.Should().Be(certificatePath);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Read_HttpsEnabled_ReportsMissingCertificatePathClearly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-wams-{Guid.NewGuid()}.pfx");
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["HTTPS"] = "true",
            ["HTTPS_CERT_PATH"] = path,
            ["HTTPS_CERT_PASSWORD"] = "secret"
        });

        var action = () => HttpsConfiguration.Read(configuration);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"HTTPS certificate path does not exist: {path}");
    }

    [Fact]
    public void Read_HttpsEnabled_ReportsDirectoryWithoutPfxClearly()
    {
        var directory = CreateTempDirectory();

        try
        {
            var action = () => HttpsConfiguration.Read(BuildConfiguration(new Dictionary<string, string?>
            {
                ["HTTPS"] = "true",
                ["HTTPS_CERT_PATH"] = directory,
                ["HTTPS_CERT_PASSWORD"] = "secret"
            }));

            action.Should().Throw<InvalidOperationException>()
                .WithMessage($"HTTPS certificate directory contains no .pfx files: {directory}");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Read_HttpsEnabled_ReportsMultiplePfxClearly()
    {
        var directory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(directory, "first.pfx"), "first");
        File.WriteAllText(Path.Combine(directory, "second.PFX"), "second");

        try
        {
            var action = () => HttpsConfiguration.Read(BuildConfiguration(new Dictionary<string, string?>
            {
                ["HTTPS"] = "true",
                ["HTTPS_CERT_PATH"] = directory,
                ["HTTPS_CERT_PASSWORD"] = "secret"
            }));

            action.Should().Throw<InvalidOperationException>()
                .WithMessage($"HTTPS certificate directory contains 2 .pfx files: {directory}. " +
                    "Set HTTPS_CERT_PATH to the exact .pfx file to use.");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"wams-cert-{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
