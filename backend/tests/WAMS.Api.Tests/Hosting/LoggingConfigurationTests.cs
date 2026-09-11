using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using WAMS.Api.Hosting;
using Xunit;

namespace WAMS.Api.Tests.Hosting;

public class LoggingConfigurationTests
{
    [Fact]
    public void Read_UsesValuesFromDotnetConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:MinLevel"] = "Debug",
                ["Logging:WriteToFile"] = "false"
            })
            .Build();

        var result = LoggingConfiguration.Read(configuration);

        result.MinLevel.Should().Be(LogEventLevel.Debug);
        result.WriteToFile.Should().BeFalse();
    }

    [Fact]
    public void Read_InvalidOrMissingValues_UsesDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:MinLevel"] = "not-a-level"
            })
            .Build();

        var result = LoggingConfiguration.Read(configuration);

        result.MinLevel.Should().Be(LogEventLevel.Information);
        result.WriteToFile.Should().BeTrue();
    }
}
