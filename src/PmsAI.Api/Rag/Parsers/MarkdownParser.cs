namespace PmsAI.Api.Rag.Parsers;

public class MarkdownParser : IDocumentParser
{
    public bool CanParse(string fileExtension) =>
        fileExtension.Equals(".md", StringComparison.OrdinalIgnoreCase);

    public Task<List<(string Text, int? Page, string? Section)>> ParseAsync(Stream stream, string fileName)
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        var result = new List<(string Text, int? Page, string? Section)>();
        string? currentSection = null;
        var paragraphBuffer = new System.Text.StringBuilder();

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.TrimEnd();
            if (trimmed.StartsWith("#"))
            {
                // Flush current paragraph
                if (paragraphBuffer.Length > 0)
                {
                    result.Add((paragraphBuffer.ToString().Trim(), null, currentSection));
                    paragraphBuffer.Clear();
                }
                currentSection = trimmed.TrimStart('#').Trim();
            }
            else if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (paragraphBuffer.Length > 0)
                {
                    result.Add((paragraphBuffer.ToString().Trim(), null, currentSection));
                    paragraphBuffer.Clear();
                }
            }
            else
            {
                paragraphBuffer.AppendLine(trimmed);
            }
        }

        if (paragraphBuffer.Length > 0)
        {
            result.Add((paragraphBuffer.ToString().Trim(), null, currentSection));
        }

        return Task.FromResult(result.Where(r => !string.IsNullOrWhiteSpace(r.Text)).ToList());
    }
}
