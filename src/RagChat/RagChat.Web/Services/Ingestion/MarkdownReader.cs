namespace RagChat.Web.Services.Ingestion;

// Reads Markdown into paragraph blocks, tagging each with its heading path ("1. Introduction > 1.1 Product Overview").
// Paragraphs are separated by blank lines; heading lines (#, ##, ...) update the path and aren't blocks themselves.
public static class MarkdownReader
{
    public static IEnumerable<DocumentBlock> Read(string markdown)
    {
        var headings = new List<string>(); // headings[0] = "#", headings[1] = "##", ...
        var paragraph = new List<string>();

        foreach (var line in markdown.Split('\n').Select(l => l.TrimEnd('\r')))
        {
            var level = line.TakeWhile(c => c == '#').Count();
            var isHeading = level > 0 && line.Length > level && line[level] == ' ';

            // A blank line or a heading ends the current paragraph
            if ((isHeading || string.IsNullOrWhiteSpace(line)) && paragraph.Count > 0)
            {
                yield return new DocumentBlock(string.Join('\n', paragraph), Page: null, SectionPath(headings));
                paragraph.Clear();
            }

            if (isHeading)
            {
                // Keep the parent headings, replace this level and anything deeper
                if (headings.Count >= level)
                {
                    headings.RemoveRange(level - 1, headings.Count - (level - 1));
                }
                headings.Add(line[(level + 1)..].Trim());
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                paragraph.Add(line);
            }
        }

        if (paragraph.Count > 0)
        {
            yield return new DocumentBlock(string.Join('\n', paragraph), Page: null, SectionPath(headings));
        }
    }

    // The top heading is usually the document title, so the path starts below it
    private static string? SectionPath(List<string> headings) =>
        headings.Count > 1 ? string.Join(" > ", headings.Skip(1)) : headings.FirstOrDefault();
}
