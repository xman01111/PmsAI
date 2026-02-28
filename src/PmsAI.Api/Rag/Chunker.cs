namespace PmsAI.Api.Rag;

public class TextChunk
{
    public string ChunkId { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public int? Page { get; set; }
    public string? Section { get; set; }
    public int TokenCount { get; set; }
}

public class Chunker
{
    private const int MaxTokens = 800;
    private const int OverlapTokens = 100;

    public List<TextChunk> Chunk(string text, int? startPage = null)
    {
        var chunks = new List<TextChunk>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // Simple word-based tokenization approximation (1 token ≈ 4 chars or 0.75 words)
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int wordsPerChunk = MaxTokens; // approximate: 1 word ≈ 1 token
        int overlapWords = OverlapTokens;

        int start = 0;
        while (start < words.Length)
        {
            int end = Math.Min(start + wordsPerChunk, words.Length);
            var chunkWords = words[start..end];
            var chunkText = string.Join(" ", chunkWords);

            chunks.Add(new TextChunk
            {
                ChunkId = Guid.NewGuid().ToString(),
                Text = chunkText,
                Page = startPage,
                TokenCount = chunkWords.Length
            });

            if (end >= words.Length) break;
            start = end - overlapWords;
            if (start < 0) start = 0;
        }

        return chunks;
    }

    public List<TextChunk> ChunkWithOverlap(List<(string Text, int? Page, string? Section)> paragraphs)
    {
        var chunks = new List<TextChunk>();
        var buffer = new List<string>();
        int? currentPage = null;
        string? currentSection = null;
        int currentTokens = 0;

        foreach (var (text, page, section) in paragraphs)
        {
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (currentTokens + wordCount > MaxTokens && buffer.Count > 0)
            {
                chunks.Add(new TextChunk
                {
                    ChunkId = Guid.NewGuid().ToString(),
                    Text = string.Join(" ", buffer),
                    Page = currentPage,
                    Section = currentSection,
                    TokenCount = currentTokens
                });

                // Keep overlap
                var overlapBuffer = new List<string>();
                int overlapCount = 0;
                for (int i = buffer.Count - 1; i >= 0 && overlapCount < OverlapTokens; i--)
                {
                    var w = buffer[i].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    overlapBuffer.Insert(0, buffer[i]);
                    overlapCount += w;
                }
                buffer = overlapBuffer;
                currentTokens = overlapCount;
            }

            buffer.Add(text);
            currentPage ??= page;
            currentSection ??= section;
            currentTokens += wordCount;
        }

        if (buffer.Count > 0)
        {
            chunks.Add(new TextChunk
            {
                ChunkId = Guid.NewGuid().ToString(),
                Text = string.Join(" ", buffer),
                Page = currentPage,
                Section = currentSection,
                TokenCount = currentTokens
            });
        }

        return chunks;
    }
}
