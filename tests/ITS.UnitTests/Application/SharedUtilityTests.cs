using FluentAssertions;
using ITS.Shared.Extensions;
using ITS.Shared.Utilities;

namespace ITS.UnitTests.Application;

public class SharedUtilityTests
{
    [Theory]
    [InlineData("OPS", true)]
    [InlineData("INFRA", true)]
    [InlineData("A1", true)]
    [InlineData("ops", false)]   // lowercase not valid
    [InlineData("O", false)]     // too short
    [InlineData("TOOLONGKEY", true)]  // exactly 10 chars
    [InlineData("TOOLONGKEYY", false)] // 11 chars — too long
    [InlineData("OPS-1", false)]  // hyphens not allowed
    public void IsValidProjectKey_ReturnsExpected(string key, bool expected)
    {
        key.IsValidProjectKey().Should().Be(expected);
    }

    [Fact]
    public void Truncate_LongString_AddsEllipsis()
    {
        var result = "Hello World".Truncate(7);
        result.Should().Be("Hello W…");
        result.Length.Should().Be(8); // 7 + ellipsis is 8 chars but truncated to 7 chars of content
    }

    [Fact]
    public void Truncate_ShortString_ReturnsUnchanged()
    {
        "Hi".Truncate(10).Should().Be("Hi");
    }

    [Fact]
    public void NullIfWhiteSpace_WhitespaceString_ReturnsNull()
    {
        "   ".NullIfWhiteSpace().Should().BeNull();
        "".NullIfWhiteSpace().Should().BeNull();
        ((string?)null).NullIfWhiteSpace().Should().BeNull();
    }

    [Fact]
    public void RowVersionHelper_RoundTrip_IsConsistent()
    {
        var original = new byte[] { 0, 0, 0, 0, 0, 0, 0, 42 };

        var etag = RowVersionHelper.ToETag(original);
        var roundTripped = RowVersionHelper.FromETag(etag);

        roundTripped.Should().Equal(original);
    }

    [Fact]
    public void RowVersionHelper_TryFromETag_InvalidInput_ReturnsFalse()
    {
        RowVersionHelper.TryFromETag("not-base64!!!", out _).Should().BeFalse();
        RowVersionHelper.TryFromETag(null, out _).Should().BeFalse();
        RowVersionHelper.TryFromETag("", out _).Should().BeFalse();
    }
}
