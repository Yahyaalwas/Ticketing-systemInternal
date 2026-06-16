using System.Text.RegularExpressions;

namespace ITS.Shared.Extensions;

public static partial class StringExtensions
{
    [GeneratedRegex(@"^[A-Z0-9]{2,10}$")]
    private static partial Regex ProjectKeyPattern();

    public static bool IsValidProjectKey(this string value)
        => !string.IsNullOrEmpty(value) && ProjectKeyPattern().IsMatch(value);

    public static string ToProjectKey(this string value)
        => value.ToUpperInvariant().Trim();

    public static string Truncate(this string value, int maxLength, string suffix = "…")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value[..(maxLength - suffix.Length)] + suffix;
    }

    public static string? NullIfWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
