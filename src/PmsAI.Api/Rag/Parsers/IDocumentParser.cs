namespace PmsAI.Api.Rag.Parsers;

public interface IDocumentParser
{
    bool CanParse(string fileExtension);
    Task<List<(string Text, int? Page, string? Section)>> ParseAsync(Stream stream, string fileName);
}
