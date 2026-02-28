using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PmsAI.Api.Rag.Parsers;

public class WordParser : IDocumentParser
{
    public bool CanParse(string fileExtension) =>
        fileExtension.Equals(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<List<(string Text, int? Page, string? Section)>> ParseAsync(Stream stream, string fileName)
    {
        var result = new List<(string Text, int? Page, string? Section)>();

        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return Task.FromResult(result);

        string? currentSection = null;
        foreach (var para in body.Elements<Paragraph>())
        {
            var style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
            var text = string.Concat(para.Descendants<Text>().Select(t => t.Text));

            if (string.IsNullOrWhiteSpace(text)) continue;

            if (style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) ||
                style.StartsWith("heading", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = text.Trim();
            }
            else
            {
                result.Add((text.Trim(), null, currentSection));
            }
        }

        return Task.FromResult(result);
    }
}
