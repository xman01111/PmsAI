# PmsAI — 酒店集团 AI 知识库问答 + PMS 业务工具系统

## 1. 项目简介

PmsAI 是一套面向酒店集团（多租户多酒店）的 AI 知识库问答与 PMS 业务工具调用系统，基于 .NET 8 WebAPI 构建。

主要功能：
- 基于 RAG（检索增强生成）的知识库问答，支持 Markdown / PDF / Word 文档
- DeepSeek 大模型（兼容 OpenAI ChatCompletions 协议）
- 通义（阿里云 DashScope）向量化
- Qdrant 向量数据库，多租户强制隔离
- PMS 业务工具调用（预订查询、房态查询、工单创建、延退申请）
- 写操作二次确认机制
- JWT 多租户鉴权
- 全链路审计日志
- ASP.NET Core Rate Limiting

## 2. 技术架构

```
用户请求 (JWT Token)
    │
    ▼
TenantContextMiddleware （解析 tenantId/userId/hotelIds/roles）
    │
    ▼
ChatController / IngestController / ToolsController
    │
    ▼
ChatOrchestrator
  ├── QwenEmbeddingClient → DashScope API → float[] 向量
  ├── QdrantVectorStore   → Qdrant（强制 tenantId filter）→ TopK chunks
  ├── ContextBuilder      → 拼装 RAG prompt 上下文
  ├── DeepSeekChatClient  → DeepSeek API（含 tools 参数）
  └── PmsToolExecutor
        ├── 只读工具 → PmsAdapterClient → PMS Adapter REST API
        └── 写工具  → PendingActionService（保存 DB）→ 等待用户确认
              └── ToolsController.Confirm → 执行写操作

SqlSugar (SQL Server) ← 所有 ORM 操作
Serilog               ← 日志
```

## 3. 快速启动

### 3.1 配置 appsettings.json

编辑 `src/PmsAI.Api/appsettings.json`，填写真实的 API Key：

```json
{
  "DeepSeek":       { "ApiKey": "sk-..." },
  "QwenEmbedding":  { "ApiKey": "sk-..." },
  "Jwt":            { "SecretKey": "your-secret-key-min-32-chars" },
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=PmsAI;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3.2 启动 Qdrant（Docker）

```bash
cd docker/qdrant
docker compose up -d
```

### 3.3 运行项目

```bash
dotnet run --project src/PmsAI.Api
```

启动时会自动：
- SqlSugar CodeFirst 建表（首次部署）
- 在 Qdrant 创建 `kb_chunks` 集合

访问 Swagger UI：`http://localhost:5000/swagger`

## 4. API 接口

### POST /api/chat

对话问答（RAG + 工具调用）。

```json
请求：
{
  "conversationId": "可选，GUID",
  "message": "查一下1001号房间的预订情况",
  "hotelRef": "可选，酒店名称/别名/编号",
  "stream": false
}

响应：
{
  "answer": "...",
  "conversationId": "guid",
  "citations": [...],
  "toolCalls": [...],
  "pendingAction": null
}
```

### POST /api/ingest

文档入库（multipart/form-data）。

| 字段     | 类型     | 说明                          |
|----------|----------|-------------------------------|
| file     | file     | .md / .pdf / .docx             |
| hotelId  | Guid?    | null = 集团通用               |
| title    | string   | 文档标题                      |
| roles    | string?  | 逗号分隔角色，null = 所有角色 |

### POST /api/tools/confirm

确认/取消写操作（工单/延退）。

```json
请求：
{
  "pendingActionId": "guid",
  "confirm": true
}
```

## 5. 多租户说明

JWT Token 中需包含以下 Claims：

| Claim       | 说明                            |
|-------------|--------------------------------|
| `tenant_id` | 租户 GUID                       |
| `sub`       | 用户 GUID                       |
| `hotel_ids` | 逗号分隔的酒店 GUID 列表         |
| `roles`     | 逗号分隔的角色代码               |

也可通过 Header 传入：`X-Tenant-Id`, `X-Hotel-Ids`, `X-Roles`。

**所有向量检索都强制注入 `tenantId` filter**，确保跨租户数据完全隔离。

## 6. 文档入库说明

支持格式：`.md`、`.pdf`（PdfPig）、`.docx`（DocumentFormat.OpenXml）。

文档处理流程：
1. 解析文档 → 分段
2. Chunker 分块（约 800 tokens，100 token 重叠）
3. QwenEmbeddingClient 批量向量化（每批 25 条）
4. 写入 Qdrant（携带 tenantId/hotelId/roles 等元数据）
5. 更新 Documents / IngestionJobs 状态
