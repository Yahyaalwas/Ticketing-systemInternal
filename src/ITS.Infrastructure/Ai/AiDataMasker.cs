using System.Text.RegularExpressions;
using ITS.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ITS.Infrastructure.Ai;

public sealed class AiDataMaskerOptions
{
    public const string SectionName = "Ai:DataMasking";
    public bool Enabled { get; set; } = false;
    public bool MaskEmails { get; set; } = true;
    public bool MaskPhoneNumbers { get; set; } = true;
    public bool MaskIpAddresses { get; set; } = true;
    public List<string> CustomPatterns { get; set; } = [];
}

public sealed partial class AiDataMasker(IOptions<AiDataMaskerOptions> options) : IAiDataMasker
{
    private readonly AiDataMaskerOptions _opts = options.Value;

    public bool IsEnabled => _opts.Enabled;

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(\+\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")]
    private static partial Regex IpRegex();

    public string Mask(string input)
    {
        if (!_opts.Enabled || string.IsNullOrEmpty(input)) return input;

        if (_opts.MaskEmails)
            input = EmailRegex().Replace(input, "[EMAIL]");

        if (_opts.MaskPhoneNumbers)
            input = PhoneRegex().Replace(input, "[PHONE]");

        if (_opts.MaskIpAddresses)
            input = IpRegex().Replace(input, "[IP]");

        foreach (var pattern in _opts.CustomPatterns)
        {
            try { input = Regex.Replace(input, pattern, "[REDACTED]"); }
            catch { /* ignore invalid patterns */ }
        }

        return input;
    }
}
