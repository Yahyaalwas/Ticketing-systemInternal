namespace ITS.Application.Common.Interfaces;

public interface IMarkdownService
{
    string RenderToHtml(string markdown);
    string ExtractMentions(string markdown, out IReadOnlyList<string> mentionedUpns);
}
