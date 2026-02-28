using PmsAI.Api.Rag.VectorStore;
using PmsAI.Contracts.Chat;

namespace PmsAI.Api.Rag;

public class ContextBuilder
{
    private readonly ILogger<ContextBuilder> _logger;

    public ContextBuilder(ILogger<ContextBuilder> logger)
    {
        _logger = logger;
    }

    public (string Context, List<CitationDto> Citations) Build(List<ChunkSearchResult> chunks)
    {
        if (chunks.Count == 0)
        {
            return (string.Empty, new List<CitationDto>());
        }

        var citations = new List<CitationDto>();
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var citationId = $"C{i + 1}";

            citations.Add(new CitationDto
            {
                CitationId = citationId,
                DocId = Guid.TryParse(chunk.DocId, out var docId) ? docId : Guid.Empty,
                Title = chunk.Title,
                SourcePath = chunk.SourcePath,
                Page = chunk.Page,
                ChunkId = chunk.ChunkId,
                Score = chunk.Score,
                Snippet = chunk.Text.Length > 200 ? chunk.Text[..200] + "..." : chunk.Text
            });

            sb.AppendLine($"[{citationId}] {chunk.Title}");
            if (chunk.Page.HasValue) sb.AppendLine($"页码: {chunk.Page}");
            if (!string.IsNullOrEmpty(chunk.Section)) sb.AppendLine($"章节: {chunk.Section}");
            sb.AppendLine(chunk.Text);
            sb.AppendLine();
        }

        return (sb.ToString(), citations);
    }
}
