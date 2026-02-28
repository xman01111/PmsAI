using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PmsAI.Api.Rag.Parsers;

public class PdfParser : IDocumentParser
{
    public bool CanParse(string fileExtension) =>
        fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<List<(string Text, int? Page, string? Section)>> ParseAsync(Stream stream, string fileName)
    {
        var result = new List<(string Text, int? Page, string? Section)>();

        // PdfPig requires seekable stream; copy to memory if needed
        Stream pdfStream = stream.CanSeek ? stream : CopyToMemory(stream);
        using var pdf = PdfDocument.Open(pdfStream);

        foreach (var page in pdf.GetPages())
        {
            var text = string.Join(" ", page.GetWords().Select(w => w.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Add((text.Trim(), page.Number, null));
            }
        }

        return Task.FromResult(result);
    }

    private static MemoryStream CopyToMemory(Stream stream)
    {
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}
