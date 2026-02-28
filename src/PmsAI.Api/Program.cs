using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PmsAI.Api.Ai;
using PmsAI.Api.Db;
using PmsAI.Api.Db.Entities;
using PmsAI.Api.Db.Repositories;
using PmsAI.Api.Middleware;
using PmsAI.Api.Options;
using PmsAI.Api.Pms;
using PmsAI.Api.Pms.Tools;
using PmsAI.Api.Rag;
using PmsAI.Api.Rag.Parsers;
using PmsAI.Api.Rag.VectorStore;
using PmsAI.Api.Services;
using Serilog;
using SqlSugar;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Options
builder.Services.Configure<DeepSeekOptions>(builder.Configuration.GetSection("DeepSeek"));
builder.Services.Configure<QwenEmbeddingOptions>(builder.Configuration.GetSection("QwenEmbedding"));
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection("Qdrant"));
builder.Services.Configure<PmsAdapterOptions>(builder.Configuration.GetSection("PmsAdapter"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// JWT Authentication
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });
builder.Services.AddAuthorization();

// SqlSugar
builder.Services.AddSqlSugar(builder.Configuration);

// Repositories
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IHotelRepository, HotelRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IPendingActionRepository, PendingActionRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();

// HTTP Clients
builder.Services.AddHttpClient("DeepSeek");
builder.Services.AddHttpClient("Qwen");
builder.Services.AddHttpClient("Qdrant");
builder.Services.AddHttpClient("PmsAdapter");

// AI clients
builder.Services.AddScoped<ILLMChatClient, DeepSeekChatClient>();
builder.Services.AddScoped<IEmbeddingClient, QwenEmbeddingClient>();

// RAG
builder.Services.AddScoped<IVectorStore, QdrantVectorStore>();
builder.Services.AddSingleton<Chunker>();
builder.Services.AddScoped<ContextBuilder>();
builder.Services.AddScoped<IDocumentParser, MarkdownParser>();
builder.Services.AddScoped<IDocumentParser, PdfParser>();
builder.Services.AddScoped<IDocumentParser, WordParser>();

// PMS
builder.Services.AddScoped<IPmsClient, PmsAdapterClient>();

// Services
builder.Services.AddScoped<HotelResolverService>();
builder.Services.AddScoped<PendingActionService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<PmsToolExecutor>();
builder.Services.AddScoped<ChatOrchestrator>();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PmsAI API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("chat", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Auto-create tables (development/initial deployment)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        db.CodeFirst.InitTables(
            typeof(TenantEntity),
            typeof(HotelEntity),
            typeof(HotelAliasEntity),
            typeof(UserEntity),
            typeof(UserHotelEntity),
            typeof(DocumentEntity),
            typeof(IngestionJobEntity),
            typeof(PendingActionEntity),
            typeof(ToolAuditLogEntity),
            typeof(ChatAuditLogEntity)
        );
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database initialization failed (this is expected if SQL Server is not running)");
    }
}

// Ensure Qdrant collection exists
using (var scope = app.Services.CreateScope())
{
    try
    {
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
        await vectorStore.EnsureCollectionAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Qdrant initialization failed (this is expected if Qdrant is not running)");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();
app.MapControllers();

app.Run();
