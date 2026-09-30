namespace WAMS.Api.Controllers.Logs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;

[ApiController]
[Route("api/v1/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.Audit.LogRead)]
    public IActionResult GetLatest([FromQuery] int lines = 100)
    {
        lines = Math.Clamp(lines, 1, 1000);
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        var latestLog = Directory.Exists(logDirectory)
            ? Directory.EnumerateFiles(logDirectory, "wams-*.log")
                .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        if (latestLog is null)
            return Content(string.Empty, "text/plain; charset=utf-8");

        // ponytail: scans the active file per request; seek backward if log volume makes this slow.
        var content = string.Join(Environment.NewLine, System.IO.File.ReadLines(latestLog).TakeLast(lines));
        return Content(content, "text/plain; charset=utf-8");
    }
}
