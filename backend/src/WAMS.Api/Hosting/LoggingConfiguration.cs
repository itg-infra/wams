using Serilog.Events;

namespace WAMS.Api.Hosting;

public sealed record LoggingConfiguration(LogEventLevel MinLevel, bool WriteToFile)
{
    public static LoggingConfiguration Read(IConfiguration configuration)
    {
        var minLevel = Enum.TryParse<LogEventLevel>(
            configuration["Logging:MinLevel"], ignoreCase: true, out var parsedLevel)
            ? parsedLevel
            : LogEventLevel.Information;

        var writeToFile = configuration.GetValue("Logging:WriteToFile", true);

        return new LoggingConfiguration(minLevel, writeToFile);
    }
}
