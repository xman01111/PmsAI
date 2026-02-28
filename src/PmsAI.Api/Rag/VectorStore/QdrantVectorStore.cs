using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using PmsAI.Api.Options;

namespace PmsAI.Api.Rag.VectorStore;

public class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _http;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStore(IHttpClientFactory httpClientFactory, IOptions<QdrantOptions> options, ILogger<QdrantVectorStore> logger)
    {
        _http = httpClientFactory.CreateClient("Qdrant");
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_options.Url}/collections/{_options.CollectionName}";
        var check = await _http.GetAsync(url, cancellationToken);
        if (check.IsSuccessStatusCode) return;

        var body = new
        {
            vectors = new
            {
                size = _options.VectorSize,
                distance = "Cosine"
            }
        };
        var json = JsonSerializer.Serialize(body);
        var response = await _http.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Created Qdrant collection: {Collection}", _options.CollectionName);
    }

    public async Task UpsertAsync(string pointId, float[] vector, ChunkPayload payload, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.Url}/collections/{_options.CollectionName}/points";
        var body = new
        {
            points = new[]
            {
                new
                {
                    id = pointId,
                    vector,
                    payload
                }
            }
        };
        var json = JsonSerializer.Serialize(body);
        var response = await _http.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ChunkSearchResult>> SearchAsync(
        float[] queryVector,
        string tenantId,
        string? hotelId,
        List<string>? roles,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_options.Url}/collections/{_options.CollectionName}/points/search";

        // Build tenant + hotel filter (enforce multi-tenant isolation)
        var mustConditions = new JsonArray
        {
            new JsonObject
            {
                ["key"] = "tenantId",
                ["match"] = new JsonObject { ["value"] = tenantId }
            }
        };

        if (!string.IsNullOrEmpty(hotelId))
        {
            mustConditions.Add(new JsonObject
            {
                ["should"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["key"] = "hotelId",
                        ["match"] = new JsonObject { ["value"] = hotelId }
                    },
                    new JsonObject
                    {
                        ["is_null"] = new JsonObject { ["key"] = "hotelId" }
                    }
                }
            });
        }

        var filter = new JsonObject { ["must"] = mustConditions };

        var requestBody = new JsonObject
        {
            ["vector"] = new JsonArray(queryVector.Select(v => JsonValue.Create(v)).ToArray<JsonNode?>()),
            ["limit"] = _options.TopK,
            ["score_threshold"] = _options.ScoreThreshold,
            ["with_payload"] = true,
            ["filter"] = filter
        };

        var json = requestBody.ToJsonString();
        var response = await _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var results = new List<ChunkSearchResult>();

        foreach (var item in doc.RootElement.GetProperty("result").EnumerateArray())
        {
            var score = item.GetProperty("score").GetDouble();
            var payload = item.GetProperty("payload");

            results.Add(new ChunkSearchResult
            {
                ChunkId = GetString(payload, "chunkId"),
                DocId = GetString(payload, "docId"),
                Title = GetString(payload, "title"),
                SourceType = GetString(payload, "sourceType"),
                SourcePath = GetString(payload, "sourcePath"),
                Page = payload.TryGetProperty("page", out var pageEl) && pageEl.ValueKind == JsonValueKind.Number ? pageEl.GetInt32() : null,
                Section = payload.TryGetProperty("section", out var sectionEl) ? sectionEl.GetString() : null,
                Text = GetString(payload, "text"),
                Score = score
            });
        }

        return results;
    }

    public async Task DeleteByDocIdAsync(string tenantId, string docId, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.Url}/collections/{_options.CollectionName}/points/delete";
        var body = new
        {
            filter = new
            {
                must = new[]
                {
                    new { key = "tenantId", match = new { value = tenantId } },
                    new { key = "docId", match = new { value = docId } }
                }
            }
        };
        var json = JsonSerializer.Serialize(body);
        var response = await _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string GetString(JsonElement el, string key)
    {
        return el.TryGetProperty(key, out var val) ? val.GetString() ?? string.Empty : string.Empty;
    }
}
