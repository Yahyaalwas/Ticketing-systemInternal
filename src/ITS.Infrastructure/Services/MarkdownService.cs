using System.Text.RegularExpressions;
using Ganss.Xss; // provided by the HtmlSanitizer NuGet package
using ITS.Application.Common.Interfaces;
using Markdig;

namespace ITS.Infrastructure.Services;

public sealed partial class MarkdownService : IMarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly HtmlSanitizer Sanitizer = BuildSanitizer();

    [GeneratedRegex(@"@([a-zA-Z0-9._\-]+@[a-zA-Z0-9._\-]+\.[a-zA-Z]{2,})", RegexOptions.Compiled)]
    private static partial Regex MentionPattern();

    public string RenderToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var html = Markdown.ToHtml(markdown, Pipeline);
        return Sanitizer.Sanitize(html);
    }

    public string ExtractMentions(string markdown, out IReadOnlyList<string> mentionedUpns)
    {
        var matches = MentionPattern().Matches(markdown);
        mentionedUpns = matches.Select(m => m.Groups[1].Value.ToLowerInvariant()).Distinct().ToList();
        return markdown;
    }

    private static HtmlSanitizer BuildSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Add("pre");
        sanitizer.AllowedTags.Add("code");
        sanitizer.AllowedTags.Add("blockquote");
        sanitizer.AllowedTags.Add("table");
        sanitizer.AllowedTags.Add("thead");
        sanitizer.AllowedTags.Add("tbody");
        sanitizer.AllowedTags.Add("tr");
        sanitizer.AllowedTags.Add("th");
        sanitizer.AllowedTags.Add("td");
        sanitizer.AllowedAttributes.Add("class");
        sanitizer.AllowedAttributes.Add("id");
        return sanitizer;
    }
}
