using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PmsAI.Api.Options;

namespace PmsAI.Api.Ai;

public class QwenEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _http;
    private readonly QwenEmbeddingOptions _options;
    private readonly ILogger<QwenEmbeddingClient> _logger;

    private const string DashScopeUrl = "https://dashscope.aliyuncs.com/api/v1/services/embeddings/text-embedding/text-embedding";

    public QwenEmbeddingClient(IHttpClientFactory httpClientFactory, IOptions<QwenEmbeddingOptions> options, ILogger<QwenEmbeddingClient> logger)
    {
        _http = httpClientFactory.CreateClient("Qwen");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await EmbedBatchAsync(new List<string> { text }, cancellationToken);
        return result[0];
    }

    public async Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        var allVectors = new List<float[]>();
        for (int i = 0; i < texts.Count; i += _options.BatchSize)
        {
            var batch = texts.Skip(i).Take(_options.BatchSize).ToList();
            var vectors = await EmbedBatchInternalAsync(batch, cancellationToken);
            allVectors.AddRange(vectors);
        }
        return allVectors;
    }

    private async Task<List<float[]>> EmbedBatchInternalAsync(List<string> texts, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _options.Model,
            input = new { texts },
            parameters = new { text_type = "document" }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, DashScopeUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        _logger.LogDebug("Embedding {Count} texts with Qwen", texts.Count);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var embeddings = doc.RootElement
            .GetProperty("output")
            .GetProperty("embeddings");

        var result = new List<float[]>();
        foreach (var item in embeddings.EnumerateArray())
        {
            var embedding = item.GetProperty("embedding");
            var vector = embedding.EnumerateArray().Select(e => e.GetSingle()).ToArray();
            result.Add(vector);
        }
        return result;
    }
}
