using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmsAI.Api.Ai;
using PmsAI.Api.Db.Entities;
using PmsAI.Api.Db.Repositories;
using PmsAI.Api.Models;
using PmsAI.Api.Rag;
using PmsAI.Api.Rag.Parsers;
using PmsAI.Api.Rag.VectorStore;
using PmsAI.Contracts.Ingest;

namespace PmsAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IngestController : ControllerBase
{
    private readonly IEnumerable<IDocumentParser> _parsers;
    private readonly Chunker _chunker;
    private readonly IEmbeddingClient _embedding;
    private readonly IVectorStore _vectorStore;
    private readonly IDocumentRepository _documentRepository;
    private readonly SqlSugar.ISqlSugarClient _db;
    private readonly ILogger<IngestController> _logger;

    public IngestController(
        IEnumerable<IDocumentParser> parsers,
        Chunker chunker,
        IEmbeddingClient embedding,
        IVectorStore vectorStore,
        IDocumentRepository documentRepository,
        SqlSugar.ISqlSugarClient db,
        ILogger<IngestController> logger)
    {
        _parsers = parsers;
        _chunker = chunker;
        _embedding = embedding;
        _vectorStore = vectorStore;
        _documentRepository = documentRepository;
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<ActionResult<IngestResponse>> Ingest(
        IFormFile file,
        [FromForm] Guid? hotelId,
        [FromForm] string title,
        [FromForm] string? roles,
        CancellationToken cancellationToken)
    {
        var tenantContext = HttpContext.Items["TenantContext"] as TenantContext;
        if (tenantContext == null || tenantContext.TenantId == Guid.Empty)
            return Unauthorized(new { error = "Invalid tenant context" });

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "file is required" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var parser = _parsers.FirstOrDefault(p => p.CanParse(ext));
        if (parser == null)
            return BadRequest(new { error = $"Unsupported file type: {ext}" });

        // Compute hash
        string hash;
        using (var sha = SHA256.Create())
        using (var stream = file.OpenReadStream())
        {
            hash = Convert.ToHexString(await sha.ComputeHashAsync(stream, cancellationToken));
        }

        // Check duplicate
        var existing = await _documentRepository.GetByHashAsync(tenantContext.TenantId, hash);
        if (existing != null && existing.Status == "Done")
        {
            return Ok(new IngestResponse { JobId = Guid.Empty, DocId = existing.DocId, Status = "AlreadyExists" });
        }

        var docId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var sourcePath = $"uploads/{tenantContext.TenantId}/{docId}{ext}";

        // Insert document record
        var doc = new DocumentEntity
        {
            DocId = docId,
            TenantId = tenantContext.TenantId,
            HotelId = hotelId,
            Title = title,
            SourceType = ext.TrimStart('.'),
            SourcePath = sourcePath,
            Hash = hash,
            Status = "Processing",
            RolesCsv = roles,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _documentRepository.InsertAsync(doc);

        // Insert ingestion job
        var job = new IngestionJobEntity
        {
            JobId = jobId,
            TenantId = tenantContext.TenantId,
            HotelId = hotelId,
            DocId = docId,
            Status = "Running",
            CreatedAt = DateTime.UtcNow
        };
        await _db.Insertable(job).ExecuteCommandAsync();

        // Process in background
        _ = ProcessDocumentAsync(file, ext, parser, doc, job, tenantContext, roles, cancellationToken);

        return Ok(new IngestResponse { JobId = jobId, DocId = docId, Status = "Queued" });
    }

    private async Task ProcessDocumentAsync(
        IFormFile file,
        string ext,
        IDocumentParser parser,
        DocumentEntity doc,
        IngestionJobEntity job,
        TenantContext tenantContext,
        string? roles,
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var paragraphs = await parser.ParseAsync(stream, file.FileName);
            var chunks = _chunker.ChunkWithOverlap(paragraphs);

            _logger.LogInformation("Processing {Count} chunks for doc {DocId}", chunks.Count, doc.DocId);

            var texts = chunks.Select(c => c.Text).ToList();
            var vectors = await _embedding.EmbedBatchAsync(texts, cancellationToken);

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var payload = new ChunkPayload
                {
                    TenantId = tenantContext.TenantId.ToString(),
                    HotelId = doc.HotelId?.ToString(),
                    Roles = string.IsNullOrEmpty(roles) ? null : roles.Split(',').Select(r => r.Trim()).ToList(),
                    DocId = doc.DocId.ToString(),
                    ChunkId = chunk.ChunkId,
                    Title = doc.Title,
                    SourceType = doc.SourceType,
                    SourcePath = doc.SourcePath,
                    Page = chunk.Page,
                    Section = chunk.Section,
                    Text = chunk.Text,
                    Hash = doc.Hash,
                    UpdatedAt = DateTime.UtcNow.ToString("O")
                };
                await _vectorStore.UpsertAsync(chunk.ChunkId, vectors[i], payload, cancellationToken);
            }

            doc.Status = "Done";
            doc.UpdatedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(doc);

            job.Status = "Done";
            job.FinishedAt = DateTime.UtcNow;
            await _db.Updateable(job).ExecuteCommandAsync();

            _logger.LogInformation("Ingestion complete for doc {DocId}", doc.DocId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for doc {DocId}", doc.DocId);

            doc.Status = "Failed";
            doc.UpdatedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(doc);

            job.Status = "Failed";
            job.Error = ex.Message;
            job.FinishedAt = DateTime.UtcNow;
            await _db.Updateable(job).ExecuteCommandAsync();
        }
    }
}
