using FluentAssertions;
using WAMS.Infrastructure.ExternalSync.Scheduler;
using Xunit;

namespace WAMS.Infrastructure.Tests.ExternalSync;

public class MasterDataSyncBackgroundServiceTests
{
    private static readonly TimeZoneInfo Wib = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");

    // 2026-08-03 is a Monday. WIB = UTC+7.
    [Theory]
    [InlineData("2026-08-03T00:59:00Z", false)] // Mon 07:59 WIB - before window
    [InlineData("2026-08-03T01:00:00Z", true)]  // Mon 08:00 WIB - window opens
    [InlineData("2026-08-03T09:59:00Z", true)]  // Mon 16:59 WIB - still in window
    [InlineData("2026-08-03T10:00:00Z", false)] // Mon 17:00 WIB - window closes
    [InlineData("2026-08-08T05:00:00Z", false)] // Sat 12:00 WIB - weekend, in-window hour
    [InlineData("2026-08-09T05:00:00Z", false)] // Sun 12:00 WIB - weekend, in-window hour
    public void IsOfficeHours_WeekdaysOnly_MatchesConfiguredWindow(string utcIso, bool expected)
    {
        var utcNow = DateTimeOffset.Parse(utcIso).UtcDateTime;

        MasterDataSyncBackgroundService.IsOfficeHours(utcNow, Wib, startHour: 8, endHour: 17, weekdaysOnly: true)
            .Should().Be(expected);
    }

    [Fact]
    public void IsOfficeHours_WeekdaysOnlyFalse_TreatsWeekendSameAsWeekday()
    {
        var saturdayNoonWib = DateTimeOffset.Parse("2026-08-08T05:00:00Z").UtcDateTime;

        MasterDataSyncBackgroundService.IsOfficeHours(saturdayNoonWib, Wib, startHour: 8, endHour: 17, weekdaysOnly: false)
            .Should().BeTrue();
    }
}
