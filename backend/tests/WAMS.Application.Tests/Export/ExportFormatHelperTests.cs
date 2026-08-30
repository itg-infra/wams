namespace WAMS.Application.Tests.Export;

using FluentAssertions;
using Xunit;
using WAMS.Infrastructure.Export;

public class ExportFormatHelperTests
{
    [Fact]
    public void FormatString_Null_ReturnsEmpty()
        => ExportFormatHelper.FormatString(null, null).Should().Be("");

    [Fact]
    public void FormatString_DateTime_NoFormat_UsesInvariant()
    {
        var dt = new DateTime(2026, 1, 15, 10, 30, 0);
        var result = ExportFormatHelper.FormatString(dt, null);
        result.Should().Be(dt.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormatString_DateTime_WithFormat_AppliesFormat()
        => ExportFormatHelper.FormatString(new DateTime(2026, 1, 15), "yyyy-MM-dd")
            .Should().Be("2026-01-15");

    [Fact]
    public void FormatString_DateOnly_WithFormat_AppliesFormat()
        => ExportFormatHelper.FormatString(new DateOnly(2026, 3, 7), "dd/MM/yyyy")
            .Should().Be("07/03/2026");

    [Fact]
    public void FormatString_Decimal_WithFormat_AppliesFormat()
        => ExportFormatHelper.FormatString(1234.56m, "#,##0.00")
            .Should().Be("1,234.56");

    [Fact]
    public void FormatString_Bool_True_ReturnsYes()
        => ExportFormatHelper.FormatString(true, null).Should().Be("Yes");

    [Fact]
    public void FormatString_Bool_False_ReturnsNo()
        => ExportFormatHelper.FormatString(false, null).Should().Be("No");

    [Fact]
    public void FormatString_String_ReturnsAsIs()
        => ExportFormatHelper.FormatString("hello", null).Should().Be("hello");
}
