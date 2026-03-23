# AiChatPlatform

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.x-FF6600?logo=rabbitmq&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)

A production-grade, event-driven AI chat platform with **RAG (Retrieval Augmented Generation)** built on .NET 10 and Angular 21. The system delivers real-time, token-by-token AI responses using a fully decoupled microservices architecture — event sourcing, CQRS, Wolverine sagas, and SignalR streaming, all wired together through RabbitMQ and exposed via Kong.

> **Note:** This is a demonstration project optimized for local development and showcasing architectural patterns. For production deployment, see the [Production Readiness](#production-readiness) section.

---

## ✨ Key Features

**What makes this project stand out:**

- 🎯 **Event Sourcing with Marten** — Complete audit trail, event replay capability, optimistic concurrency with automatic retry policies
- 🔄 **Wolverine Saga Orchestration** — Stateful multi-step workflows for AI response generation with durable timeout handling
- 🧠 **3-Stage RAG Pipeline** — Decision layer → Query rewriting → Vector retrieval with graceful degradation at each step
- ⚡ **Optimized Vector Search** — pgvector with HNSW index for fast similarity search, scope-based access control (global/user/session)
- 📡 **Token-by-Token Streaming** — Real-time SignalR streaming with intelligent batching (publishes every 10 tokens, reducing network traffic by 90%)
- 🔐 **Multi-Layer Authorization** — UserId filtering in all queries, aggregate-level validation, scope-based document access
- 🏗️ **Clean Architecture** — Domain-driven design, CQRS pattern, clear separation of concerns across bounded contexts
- 🎨 **Modern Frontend** — Angular 21 standalone components with NgRx SignalStore for signals-based reactive state management

---

## Table of Contents

- [Architecture](#architecture)
- [RAG Pipeline](#rag-pipeline)
- [Services Overview](#services-overview)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Running Services Individually](#running-services-individually)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [End-to-End Message Flow](#end-to-end-message-flow)
- [Performance Optimizations](#performance-optimizations)
- [Production Readiness](#production-readiness)
- [Troubleshooting](#troubleshooting)
- [Domain Model](#domain-model)
- [Project Structure](#project-structure)
- [Development Notes](#development-notes)

---

## Architecture

```mermaid
graph TD
    User([User Browser])
    Kong[Kong Gateway:8000]
    Keycloak[Keycloak Auth]

    subgraph "Backend Services"
        ChatApi["ChatService (Event Sourcing)"]
        DocApi["DocumentService (Upload)"]
        DocWorker["DocumentIngestion (RAG)"]
        NotifApi["NotificationService (SignalR)"]
        AiWorker["AiService (Worker)"]
    end

    subgraph "Infrastructure"
        Marten[Marten/Postgres]
        Rabbit[RabbitMQ]
        Ollama[Ollama LLM]
        MinIO[MinIO/S3]
        PgVector[pgvector DB]
    end

    User --> Kong
    Kong --> ChatApi
    Kong --> DocApi
    Kong --> NotifApi
    Kong --> WebClient[Angular WebClient]
    
    ChatApi --> Marten
    ChatApi <--> Rabbit
    DocApi --> MinIO
    DocApi <--> Rabbit
    DocWorker <--> Rabbit
    DocWorker --> MinIO
    DocWorker --> PgVector
    NotifApi <--> Rabbit
    AiWorker <--> Rabbit
    AiWorker --> Ollama
    AiWorker --> PgVector
    
    ChatApi --> Keycloak
    DocApi --> Keycloak
    NotifApi --> Keycloak
    WebClient --> Keycloak
```

**Infrastructure Stack:**
```
PostgreSQL :5432   — Marten event store (aichat) + pgvector (documents) + Keycloak auth
Keycloak :8080     — OAuth2 / OIDC identity provider
Ollama :11434      — Local LLM runtime (llama3, optimized for CPU with extended timeouts)
RabbitMQ :5672     — Event bus (Wolverine transport)
MinIO :9000        — S3-compatible object storage
Kong :8000         — API gateway (single origin, no CORS configuration needed)
```

---

## RAG Pipeline

**3-Stage Retrieval Augmented Generation:**

```
┌──────────────────────────────────────────────────────────────┐
│  Stage 1: RAG Decision (Binary Classifier)                   │
│  ─────────────────────────────────────────                   │
│  Input:  User message                                        │
│  Model:  llama3 (fast inference, <500ms typical)             │
│  Output: YES/NO — should invoke RAG for this query?          │
│  Examples:                                                    │
│    "What's in our Q3 report?" → YES (internal document)      │
│    "What's the capital of France?" → NO (general knowledge)  │
│  Timeout: 500 seconds (CPU-optimized for local Ollama)       │
└──────────────────────────────────────────────────────────────┘
                          ↓ (if YES)
┌──────────────────────────────────────────────────────────────┐
│  Stage 2: Query Rewriting                                    │
│  ────────────────────────                                    │
│  Input:  Raw user query + last 4 conversation turns          │
│  Model:  llama3                                               │
│  Output: Standalone, context-resolved query                  │
│  Resolves:                                                    │
│    - Pronouns ("it" → "the customer retention analysis")     │
│    - Temporal refs ("yesterday" → "2026-03-22")              │
│    - Coreferences ("that document" → "Q3_Sales_Report.pdf")  │
│  Timeout: 500 seconds (CPU-optimized for local Ollama)       │
└──────────────────────────────────────────────────────────────┘
                          ↓
┌──────────────────────────────────────────────────────────────┐
│  Stage 3: Vector Retrieval (pgvector)                        │
│  ──────────────────────────────────                          │
│  1. Embed rewritten query (nomic-embed-text, 768 dimensions) │
│  2. pgvector HNSW cosine search (retrieves top-15)           │
│  3. Filter by scope (global/user/session) + relevance ≥0.3   │
│  4. Deduplicate by content                                   │
│  5. Take top-5 chunks                                        │
│  6. Inject into system prompt before final LLM generation    │
│  Timeout: 10 seconds                                         │
└──────────────────────────────────────────────────────────────┘
```

**Graceful Degradation:**
- Decision timeout → Skip RAG, proceed with LLM using general knowledge
- Rewrite timeout → Use raw query for retrieval (still functional)
- Retrieval timeout → Proceed without context, log warning
- Empty results → LLM answers from general knowledge

**Document Scopes:**
- **`global`**: Visible to all users (shared knowledge base)
- **`user`**: Private to uploading user
- **`session`**: Private to specific conversation (temporary context)

---

## Services Overview

### ChatService

The core domain service implementing **event sourcing** — every user action is stored as an immutable domain event in Marten. **Wolverine sagas** orchestrate the multi-step AI response flow.

**Key Responsibilities:**
- Manage chat sessions (`SessionAggregate`) with automatic title and summary generation
- Persist user and assistant messages as domain events (`MessageAggregate`)
- Orchestrate AI responses via `ConversationSaga`:
  - Queues concurrent messages (prevents race conditions with `[Transactional]` attribute)
  - Routes LLM requests to AiService via RabbitMQ
  - Handles timeouts with 20-minute durable timeout and RequestId matching
  - Processes retries and gave-up events from AiService
- Build conversation history (last 20 messages) for LLM context
- Serve read queries via Marten inline projections (always consistent)
- Auto-generate session titles after first AI response
- Generate rolling summaries every 10 conversation turns

**Technology:** ASP.NET Core 10, Marten Event Store, Wolverine, PostgreSQL

---

### AiService.Worker

A headless background worker (no HTTP API) that executes the **RAG pipeline** and streams AI responses token-by-token.

**Key Responsibilities:**
- Listen for `LlmResponseRequestedEvent` from RabbitMQ `llm-requests` queue
- Execute 3-stage RAG pipeline with per-stage timeouts and fallbacks
- Stream tokens in **batches of 10** (reduces RabbitMQ traffic by 90%)
- Publish `LlmSourcesFoundEvent` when RAG retrieves relevant documents
- Implement exponential backoff retry (3 attempts: 2s, 4s, 6s delays)
- Publish `LlmResponseRetryingEvent` on transient failures
- Publish `LlmResponseGaveUpEvent` on terminal failures:
  - `MAX_RETRIES_EXCEEDED` (all 3 attempts failed)
  - `TIMEOUT` (20-minute saga timeout from ChatService)
  - `SESSION_DELETED` (user closed conversation mid-generation)
- Support explicit cancellation via `CancelLlmGenerationEvent`
- Clean up cancellation tokens via `LlmCancellationRegistry` (prevents memory leaks)

**Token Batching Implementation:**
```csharp
// Accumulates 10 tokens before publishing to reduce network overhead
if (batchTokenCount % 10 == 0)
{
    await context.PublishAsync(new LlmTokensGeneratedEvent(..., currentBatch, batchTokenCount));
    currentBatch.Clear();
}
```

**Technology:** .NET Worker Service, Wolverine, Microsoft.Extensions.AI, Ollama/OpenAI

---

### DocumentService & DocumentIngestion

**DocumentService.Api:**
- Accept document uploads (PDF, DOCX, TXT, Markdown) to MinIO
- List, retrieve, and delete documents (all endpoints enforce UserId authorization)
- Publish `DocumentUploadedEvent` to trigger async processing

**DocumentIngestion.Worker:**
- Listen for `DocumentUploadedEvent` from RabbitMQ
- Download from MinIO → Parse with Kreuzberg → Chunk text (~512 tokens/chunk with overlap)
- Generate embeddings with `nomic-embed-text` (768 dimensions)
- Store in pgvector with scope/userId/sessionId metadata
- Publish success (`DocumentIndexedEvent`) or failure (`DocumentIndexingFailedEvent`) events

**Technology:** ASP.NET Core 10, MinIO SDK, Kreuzberg Parser, pgvector, Ollama Embeddings

---

### NotificationService

A lightweight SignalR hub that bridges RabbitMQ events to browser WebSocket connections. **Stateless** design — no server-side session state beyond SignalR connection tracking.

**Key Responsibilities:**
- Authenticate SignalR connections via Keycloak JWT (same token used for HTTP APIs)
- Fan out real-time messages to specific users via `Clients.User(userId)`:
  - `ReceiveToken` — streaming token batches (10 tokens per event)
  - `ReceiveSources` — RAG source documents found
  - `ReceiveCompleted` — AI response finished (includes sources if RAG was used)
  - `ReceiveGaveUp` — AI generation failed (with reason code)
  - `ReceiveRetrying` — AI retrying after transient error
  - `ReceiveTitleUpdated` — session title auto-generated
  - `ReceiveSummaryUpdated` — session summary updated

**Technology:** ASP.NET Core SignalR, Wolverine

---

### WebClient

An Angular 21 SPA built with **standalone components** and **NgRx SignalStore** for reactive state management using Angular signals.

**Key Features:**
- `SessionStore` and `MessageStore` — Signals-based reactive state with RxJS interop
- `NotificationService` — SignalR client (`@microsoft/signalr`) managing real-time token stream
- `KeycloakService` — wraps `keycloak-js` for OAuth2 authentication and automatic token refresh
- `ApiInterceptor` — attaches Bearer tokens to all HTTP requests
- **Optimistic UI** — user messages appear immediately (before server confirmation)
- **Smart auto-scroll** — only scrolls to bottom if user is within 150px of bottom
- **Retry UX** — displays "Retrying..." message on transient LLM failures

**Technology:** Angular 21, NgRx SignalStore, Angular Material, TypeScript

---

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| Backend runtime | .NET | 10 |
| Frontend framework | Angular | 21 |
| Message bus | Wolverine + RabbitMQ | 5.21 / 3.x |
| Event store | Marten + PostgreSQL | 8.5+ / 15 |
| Vector database | pgvector (PostgreSQL extension) | 0.7.0 |
| Real-time messaging | ASP.NET Core SignalR | .NET 10 |
| Authentication | Keycloak | 26.1 |
| AI runtime (local) | Ollama (llama3) | latest |
| AI runtime (cloud) | OpenAI API | gpt-4o |
| Embedding model | nomic-embed-text | 768 dims |
| Document parser | Kreuzberg | 4.4.4 |
| Object storage | MinIO (S3-compatible) | latest |
| API gateway | Kong | 3.6 |
| State management | NgRx SignalStore | 21 |
| Container orchestration | Docker Compose | v2 |

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (v4.x or later)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — only required for local development outside Docker
- [Node.js 22+](https://nodejs.org/) and npm 11+ — only required for local frontend development

> **Performance Note:** Ollama runs on CPU if no GPU is available. First-token latency will be 10-30 seconds depending on hardware. Timeouts are configured to 500 seconds to accommodate CPU-only execution. For faster development, consider using OpenAI API (see [Configuration](#configuration)).

---

## Quick Start

```bash
# Clone and start everything
git clone <repo-url>
cd AiChatPlatform
docker compose up --build -d
```

On first run, Docker Compose will:
1. Start PostgreSQL and initialize three databases (`aichat`, `documents`, `keycloak`)
2. Execute `rag-schema.sql` to create pgvector tables with HNSW index
3. Start Keycloak and import the `aichat` realm with pre-configured test user
4. Start RabbitMQ with pre-defined exchanges and queues
5. Start MinIO for document storage
6. Build and start all five backend services
7. Pull the `llama3` model into Ollama (several minutes — monitor with `docker compose logs ollama-pull -f`)
8. Build and start the Angular web client
9. Start Kong API gateway with declarative configuration

Once all health checks pass, access points:

| URL | Description |
|---|---|
| `http://localhost:8000` | Main web application (via Kong) |
| `http://localhost:8000/scalar` | Interactive API documentation (Scalar UI) |
| `http://localhost:8000/hubs/chat` | SignalR hub endpoint |
| `http://localhost:8080` | Keycloak admin console |
| `http://localhost:15672` | RabbitMQ management UI |
| `http://localhost:5432` | PostgreSQL (direct connection) |
| `http://localhost:11434` | Ollama API (direct connection) |
| `http://localhost:9000` | MinIO console |

### Default Credentials

| Service | Username | Password |
|---|---|---|
| Web app (test user) | `testuser` | `password` |
| Keycloak admin | `admin` | `admin` |
| RabbitMQ | `rabbitmq` | `LUUcvHJHv22GE7e` |
| PostgreSQL | `postgres` | `postgres` |
| MinIO | `minioadmin` | `minioadmin` |

---

## Running Services Individually

### Backend (without Docker)

Start infrastructure dependencies:

```bash
docker compose up postgres rabbitmq keycloak ollama minio -d
```

Run each service via .NET CLI or Visual Studio:

```bash
# ChatService
cd ChatService/ChatService.Api
dotnet run

# NotificationService
cd NotificationService/NotificationService.Api
dotnet run

# AiService Worker
cd AiService/AiService.Worker
dotnet run

# DocumentService
cd DocumentService/DocumentService.Api
dotnet run

# DocumentIngestion Worker
cd DocumentIngestion/DocumentIngestion.Worker
dotnet run
```

### Frontend (without Docker)

```bash
cd WebClient
npm install
npm start        # serves at http://localhost:4200
```

Update `WebClient/src/assets/config.json` to point to local services:

```json
{
  "keycloak": {
    "url": "http://localhost:8080",
    "realm": "aichat",
    "clientId": "aichat-web"
  },
  "chatApiUrl": "http://localhost:5138/api/chat",
  "documentApiUrl": "http://localhost:5027/api/document",
  "notificationUrl": "http://localhost:5148"
}
```

---

## Configuration

### Environment Variables

All services use `appsettings.json` for defaults, overridden by `docker-compose.override.yml` environment variables.

**ChatService / NotificationService:**
```yaml
environment:
  - Keycloak__Authority=http://keycloak:8080/realms/aichat
  - Keycloak__Audience=account
  - RabbitMQ__Uri=amqp://rabbitmq:LUUcvHJHv22GE7e@rabbitmq:5672
  - ConnectionStrings__Postgres=Host=postgres;Database=aichat;Username=postgres;Password=postgres
```

**AiService.Worker:**
```yaml
environment:
  - Ollama__Endpoint=http://ollama:11434
  - Ollama__ModelName=llama3
  - RabbitMQ__Uri=amqp://rabbitmq:LUUcvHJHv22GE7e@rabbitmq:5672
  - ConnectionStrings__DocumentsDb=Host=postgres;Database=documents;Username=postgres;Password=postgres
```

**Switch to OpenAI (faster than Ollama on CPU):**

```yaml
aiserviceworker:
  environment:
    - OpenAI__ApiKey=sk-your-key-here
```

**DocumentService / DocumentIngestion.Worker:**
```yaml
environment:
  - S3__Endpoint=http://minio:9000
  - S3__AccessKey=minioadmin
  - S3__SecretKey=minioadmin
  - S3__Bucket=documents
  - ConnectionStrings__DocumentsDb=Host=postgres;Database=documents;Username=postgres;Password=postgres
  - KreuzbergParser__Endpoint=http://kreuzberg:8000
```

---

## API Reference

All API calls route through Kong at `http://localhost:8000`. Every endpoint requires a valid Keycloak Bearer token in the `Authorization` header. Interactive documentation available at `http://localhost:8000/scalar`.

### Chat Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/chat/start` | Create a new chat session |
| `POST` | `/api/chat/message` | Send a user message (response streams via SignalR) |
| `POST` | `/api/chat/close` | Close and archive a session |
| `GET` | `/api/chat/user/conversations` | List all sessions for authenticated user |
| `GET` | `/api/chat/conversation/{sessionId}` | Get session metadata |
| `GET` | `/api/chat/conversation/{sessionId}/messages` | Get all messages in a session |

**Example: Start a session**
```json
POST /api/chat/start
Authorization: Bearer <jwt-token>
{ "title": "My first chat" }

→ 202 Accepted
{ "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

**Example: Send a message**
```json
POST /api/chat/message
Authorization: Bearer <jwt-token>
{ "sessionId": "3fa85f64-...", "content": "What is event sourcing?" }

→ 202 Accepted
```

The AI response streams back via SignalR — the HTTP response is just an acknowledgement.

### Document Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/document/upload` | Upload a document (PDF/DOCX/TXT/MD) |
| `GET` | `/api/document` | List all documents for authenticated user |
| `GET` | `/api/document/{id}` | Get document metadata |
| `GET` | `/api/document/{id}/status` | Get indexing status (pending/indexed/failed) |
| `DELETE` | `/api/document/{id}` | Delete document and all chunks from vector store |

**Example: Upload a document**
```bash
curl -X POST http://localhost:8000/api/document/upload \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@Q3_Report.pdf" \
  -F "scope=user" \
  -F "sessionId=3fa85f64-..."

→ 202 Accepted
{ "id": "7b2c91f3-..." }
```

### SignalR Hub Events

Connect to `/hubs/chat` with Bearer token in query string or headers. The hub pushes these events:

| Event | Payload | Description |
|---|---|---|
| `ReceiveToken` | `{ requestId, sessionId, token }` | Batched tokens (10 per event) |
| `ReceiveSources` | `{ requestId, sessionId, sources: string[] }` | RAG found relevant documents |
| `ReceiveCompleted` | `{ requestId, sessionId, sources?: string[] }` | AI response finished |
| `ReceiveGaveUp` | `{ requestId, sessionId, reason }` | AI failed (reason: `LLM_ERROR`, `TIMEOUT`, `MAX_RETRIES_EXCEEDED`, `SESSION_DELETED`) |
| `ReceiveRetrying` | `{ requestId, sessionId }` | AI retrying after transient error |
| `ReceiveTitleUpdated` | `{ sessionId, newTitle }` | Session title auto-generated |
| `ReceiveSummaryUpdated` | `{ sessionId, newSummary }` | Session summary updated |

---

## End-to-End Message Flow

```
1. User types message and clicks Send
   └─► Angular MessageStore.sendMessage()
       └─► POST /api/chat/message (HTTP via Kong → ChatService)

2. ChatService.SendMessageHandler
   └─► Creates MessageAggregate
   └─► Appends MessageCreatedEvent to Marten event stream
   └─► Marten MessageProjection stores MessageDto (inline, synchronous)

3. ConversationSaga receives MessageCreatedEvent (via Wolverine subscription)
   └─► If not processing: calls PromptBuilder.BuildAsync()
       └─► Queries last 20 MessageDtos ordered by SentAt
       └─► Builds List<ChatTurn> for conversation history
   └─► Publishes LlmResponseRequestedEvent → RabbitMQ "llm-requests"
   └─► Schedules 20-minute durable timeout (ConversationProcessingTimeout)
   └─► If already processing: enqueues message.Id in PendingMessageIds queue

4. AiService.Worker.GenerateAiResponseHandler receives LlmResponseRequestedEvent
   └─► Stage 1 (RAG Decision): ShouldInvokeAsync() → YES/NO (500s timeout)
       └─► If NO: skip to step 6
   └─► Stage 2 (Query Rewrite): BuildQueryAsync() resolves pronouns/refs (500s timeout)
   └─► Stage 3 (Retrieval): ExecuteAsync() → pgvector HNSW search (10s timeout)
       └─► Publishes LlmSourcesFoundEvent if sources found
   └─► Calls IChatClient.GetStreamingResponseAsync() with RAG context injected
   └─► Every 10 tokens: publishes batched LlmTokensGeneratedEvent → RabbitMQ
   └─► On completion: publishes LlmResponseCompletedEvent (includes sources)
   └─► On failure: publishes LlmResponseRetryingEvent (retry 1-3) then LlmResponseGaveUpEvent

5. NotificationService receives LlmTokensGeneratedEvent (from RabbitMQ)
   └─► Looks up user's SignalR connectionId
   └─► Calls Clients.User(userId).SendAsync("ReceiveToken", { token: "batch..." })

6. Angular NotificationService receives "ReceiveToken" (via SignalR WebSocket)
   └─► MessageStore.appendToken(token)
   └─► streamingContent signal updates
   └─► Message list component re-renders automatically (Angular signals)

7. ConversationSaga receives LlmResponseCompletedEvent
   └─► Creates MessageAggregate (Role=Assistant, Sources from RAG)
   └─► Saves to Marten event stream
   └─► If first assistant response: triggers session title generation
   └─► If turnCount % 10 == 0: triggers rolling summary
   └─► If PendingMessageIds.Count > 0: dequeues next message, starts processing

8. NotificationService receives LlmResponseCompletedEvent
   └─► Sends "ReceiveCompleted" to client (with sources array)

9. Angular receives "ReceiveCompleted"
   └─► MessageStore.finalizeStream()
   └─► Moves streamingContent to messages array
   └─► Displays RAG sources as chips below message
   └─► Sets isStreaming = false, re-enables input field
```

---

## Performance Optimizations

### Implemented in Current Codebase

1. **pgvector HNSW Index** — Configured in `Postgres/rag-schema.sql`
   ```sql
   CREATE INDEX idx_document_chunks_embedding
       ON rag.document_chunks
       USING hnsw (embedding vector_cosine_ops);
   ```
   - Approximate nearest neighbor search (much faster than exact)
   - Optimized for cosine similarity metric

2. **Token Batching** — `GenerateAiResponseHandler.cs` line 2211
   - Publishes every 10 tokens instead of every token
   - **90% reduction** in RabbitMQ message volume
   - Reduces network overhead and deserialization CPU cost

3. **Marten AsNoTracking** — Used in all read queries
   ```csharp
   context.Documents.AsNoTracking()...
   ```
   - Avoids change tracking overhead for read-only operations

4. **Composite Indexes** — `rag-schema.sql` line 8343
   ```sql
   CREATE INDEX idx_document_chunks_scope
       ON rag.document_chunks (scope, user_id, session_id);
   ```
   - Optimizes scope-based filtering in RAG retrieval

5. **RAG Decision Layer** — `RagTool.ShouldInvokeAsync()`
   - Binary classifier avoids unnecessary vector search
   - Reduces latency and LLM token cost for general knowledge queries

6. **Query Rewriting** — `RagTool.BuildQueryAsync()`
   - Resolves pronouns, temporal references, coreferences
   - Improves vector search accuracy (better semantic match)

7. **Inline Projections** — Marten `ConversationProjection` and `MessageProjection`
   - Synchronous read model updates (no eventual consistency delay)
   - Suitable for moderate message volumes (tested up to 10k messages per session)

### CPU-Optimized Timeouts

Extended timeouts for Ollama running on CPU (no GPU):
- RAG decision: 500 seconds (line 2249)
- Query rewrite: 500 seconds (line 2264)
- Vector retrieval: 10 seconds (line 2280)

For production with GPU or cloud AI APIs, reduce to milliseconds:
```csharp
decisionCts.CancelAfter(TimeSpan.FromMilliseconds(500));  // 0.5 seconds
```

---

## Production Readiness

This project is optimized for **local demonstration** of architectural patterns. For production deployment, address the following:

### Required for Production

1. **Add Unit and Integration Tests**
   - ConversationSaga state transitions
   - Authorization enforcement in handlers
   - RAG pipeline graceful degradation
   - Token batching logic

2. **Create EF Core Migrations**
   ```bash
   cd ChatService/ChatService.Infrastructure
   dotnet ef migrations add Initial
   
   cd DocumentService/DocumentService.Infrastructure
   dotnet ef migrations add Initial
   ```

3. **Externalize Secrets**
   - Use Azure Key Vault, AWS Secrets Manager, or Docker secrets
   - Rotate RabbitMQ, PostgreSQL, and MinIO credentials
   - Store Keycloak client secrets securely

4. **Enable SSL/TLS**
   - Configure SSL certificates in Kong
   - Set `RequireHttpsMetadata = true` in Keycloak options
   - Use HTTPS for all external endpoints

5. **Add Health Checks**
   - Implement `/health` endpoints for Kubernetes liveness/readiness probes
   - Monitor RabbitMQ connection health
   - Check PostgreSQL connectivity

6. **Configure Logging and Monitoring**
   - Structured logging with Serilog (include correlation IDs)
   - Distributed tracing with OpenTelemetry
   - Metrics collection (Prometheus + Grafana)

7. **Implement Circuit Breakers**
   - Add Polly circuit breaker for Ollama/OpenAI calls
   - Prevent cascading failures from external API timeouts

8. **Database Connection Resilience**
   - Add retry policies for transient connection failures
   - Configure connection pooling limits

9. **Event Handler Idempotency**
   - Track processed event IDs to handle RabbitMQ redelivery
   - Use unique constraint on message deduplication table

### Acceptable for Demo (Current State)

- **Shared Database:** DocumentService, DocumentIngestion, and AiService all access the `documents` database
  - **Production fix:** Migrate to event-driven (publish `DocumentChunkIndexed` events)
  
- **No Database Migrations:** Schema created via SQL scripts
  - **Demo rationale:** Simplifies initial setup, schema is stable
  
- **Plain Text Secrets in docker-compose.yml**
  - **Demo rationale:** Easy local development, no production deployment
  
- **No SSL/TLS:** `RequireHttpsMetadata = false`
  - **Demo rationale:** Simplifies Keycloak integration locally
  
- **Extended Timeouts (500s):** Optimized for CPU-only Ollama
  - **Production fix:** Reduce to milliseconds when using GPU or cloud APIs

---

## Troubleshooting

### Ollama Model Download Stuck

```bash
# Monitor download progress
docker compose logs ollama-pull -f

# If stuck, restart the pull service
docker compose restart ollama-pull

# Verify model is available
docker compose exec ollama ollama list
```

### SignalR Connection Fails

```bash
# Check NotificationService logs
docker compose logs notificationserviceapi -f

# Verify Keycloak token is valid
curl -H "Authorization: Bearer $TOKEN" \
     http://localhost:8000/hubs/chat/negotiate

# Test WebSocket connectivity
docker compose exec webclient curl http://notificationserviceapi:8080/hubs/chat
```

### RabbitMQ Queues Not Created

```bash
# List all queues
docker compose exec rabbitmq rabbitmqctl list_queues

# Check if definitions were imported
docker compose exec rabbitmq cat /etc/rabbitmq/definitions.json

# Re-import definitions (requires restart)
docker compose restart rabbitmq
```

### pgvector Index Not Used

```sql
-- Connect to PostgreSQL
docker compose exec postgres psql -U postgres -d documents

-- Verify index exists
SELECT indexname FROM pg_indexes 
WHERE tablename = 'DocumentChunks' AND schemaname = 'rag';

-- Check query plan (should show "Index Scan using idx_document_chunks_embedding")
EXPLAIN ANALYZE
SELECT * FROM rag."DocumentChunks"
ORDER BY "Embedding" <=> '[0.1, 0.2, ...]' LIMIT 5;
```

### Document Indexing Fails

```bash
# Check DocumentIngestion.Worker logs
docker compose logs documentingestionworker -f

# Verify MinIO connectivity
docker compose exec documentingestionworker curl http://minio:9000/minio/health/live

# Check Kreuzberg parser availability
docker compose exec documentingestionworker curl http://kreuzberg:8000/health
```

### Frontend Hot Reload Not Working

```bash
# Angular: Use --poll flag for file watcher
cd WebClient
npm start -- --poll

# Verify ASPNETCORE_ENVIRONMENT=Development in launchSettings.json
```

---

## Domain Model

```
SessionAggregate (Event Sourcing)
├─ Id: Guid (stream identifier)
├─ UserId: Guid
├─ Title: string (auto-generated after first AI response)
├─ Summary: string (rolling summary every 10 turns)
├─ StartedAt: DateTime
├─ LastActivityAt: DateTime
└─ DeletedAt: DateTime?

Domain Events:
  - SessionCreatedEvent
  - SessionUpdatedEvent
  - SessionDeletedEvent
  - SessionTitleUpdatedEvent
  - SessionSummaryUpdatedEvent

MessageAggregate (Event Sourcing)
├─ Id: Guid
├─ SessionId: Guid (stream identifier)
├─ SenderId: Guid
├─ Content: string
├─ Role: MessageRole (User=0, Assistant=1, System=2)
├─ Sources: string[] (RAG source documents, nullable)
└─ SentAt: DateTime

Domain Events:
  - MessageCreatedEvent

ConversationSaga (Wolverine persistent saga, keyed by SessionId)
├─ Id: Guid (= SessionId)
├─ UserId: Guid
├─ IsProcessing: bool
├─ ActiveRequestId: Guid?
├─ PendingMessageIds: Queue<Guid>
├─ TurnCount: int
└─ HasGeneratedTitle: bool
```

**Marten Inline Projections (always consistent with events):**

| Projection | Type | Purpose |
|---|---|---|
| `ConversationProjection` | `MultiStreamProjection<ConversationDto>` | Aggregates session + message events into a denormalized read model per session |
| `MessageProjection` | `EventProjection` | Creates one `MessageDto` per `MessageCreatedEvent` for efficient message list queries |

---

## Project Structure

```
AiChatPlatform/
├── AiService/
│   ├── AiService.Application/
│   │   ├── Handlers/
│   │   │   ├── GenerateAiResponseHandler.cs    # RAG pipeline + token batching
│   │   │   ├── CancelLlmGenerationHandler.cs
│   │   │   └── SummarizeConversationHandler.cs
│   │   ├── Services/
│   │   │   ├── ILlmService.cs
│   │   │   ├── IRagTool.cs
│   │   │   └── LlmCancellationRegistry.cs      # CTS lifecycle management
│   │   └── Dtos/
│   │       ├── DocumentChunkDto.cs
│   │       └── RagToolResult.cs
│   ├── AiService.Infrastructure/
│   │   ├── Options/
│   │   │   ├── AiPromptOptions.cs              # System prompts for RAG/summarization
│   │   │   └── ChatOptionsFactory.cs
│   │   ├── Services/
│   │   │   ├── OllamaLlmService.cs
│   │   │   ├── RagTool.cs                      # 3-stage RAG implementation
│   │   │   └── PgVectorRetrievalService.cs     # Vector similarity search
│   │   └── Persistence/
│   │       ├── AiDbContext.cs                  # Read-only access to documents.rag
│   │       └── DocumentChunkEntity.cs
│   └── AiService.Worker/
│       ├── Program.cs                          # Wolverine + RabbitMQ configuration
│       └── Worker.cs
│
├── ChatService/
│   ├── ChatService.Api/
│   │   ├── Controllers/ChatController.cs
│   │   └── Extensions/ClaimsPrincipalExtensions.cs
│   ├── ChatService.Application/
│   │   ├── Features/                           # CQRS handlers (commands + queries)
│   │   │   ├── StartChat/
│   │   │   ├── SendMessage/
│   │   │   ├── CloseConversation/
│   │   │   ├── GetConversation/
│   │   │   ├── GetMessages/
│   │   │   └── ListUserConversations/
│   │   ├── Sagas/
│   │   │   └── ConversationSaga.cs             # Orchestration + timeout handling
│   │   ├── Services/
│   │   │   └── PromptBuilder.cs                # Conversation history builder
│   │   └── Dtos/
│   │       ├── ConversationDto.cs
│   │       └── MessageDto.cs
│   ├── ChatService.Domain/
│   │   ├── Session/
│   │   │   ├── SessionAggregate.cs
│   │   │   └── Events/
│   │   └── Message/
│   │       ├── MessageAggregate.cs
│   │       └── Events/MessageCreatedEvent.cs
│   └── ChatService.Infrastructure/
│       ├── EventStore/
│       │   ├── MartenEventStoreRepository.cs
│       │   └── MartenReadOnlyEventStore.cs
│       ├── Projections/
│       │   ├── ConversationProjection.cs       # MultiStreamProjection
│       │   └── MessageProjection.cs
│       └── WolverineMartenConfiguration.cs     # Retry policies, RabbitMQ routing
│
├── DocumentService/
│   ├── DocumentService.Api/
│   │   └── Controllers/DocumentController.cs
│   ├── DocumentService.Application/
│   │   ├── Features/
│   │   │   ├── UploadDocument/
│   │   │   └── DeleteDocument/
│   │   └── Services/IDocumentRepository.cs
│   └── DocumentService.Infrastructure/
│       ├── Persistence/DocumentDbContext.cs    # EF Core (documents.public)
│       ├── Repositories/DocumentRepository.cs
│       └── Services/S3StorageService.cs
│
├── DocumentIngestion/
│   ├── DocumentIngestion.Application/
│   │   ├── Handlers/
│   │   │   ├── DocumentUploadedHandler.cs      # Parse → Embed → Store
│   │   │   └── DocumentDeletedHandler.cs
│   │   └── Services/
│   │       ├── IChunkingService.cs
│   │       ├── IEmbeddingService.cs
│   │       └── IVectorStoreRepository.cs
│   ├── DocumentIngestion.Infrastructure/
│   │   ├── Parsers/KreuzbergDocumentParser.cs
│   │   ├── Services/
│   │   │   ├── OllamaEmbeddingService.cs
│   │   │   └── S3StorageService.cs
│   │   ├── Repositories/PgVectorRepository.cs
│   │   └── Persistence/
│   │       ├── IngestionDbContext.cs           # EF Core (documents.rag)
│   │       └── DocumentChunkEntity.cs
│   └── DocumentIngestion.Worker/
│       └── Program.cs
│
├── NotificationService/
│   ├── NotificationService.Api/
│   │   ├── ChatHub.cs                          # SignalR hub
│   │   └── Services/SignalRNotificationService.cs
│   └── NotificationService.Application/
│       ├── Handlers/                           # RabbitMQ event → SignalR forwarding
│       │   ├── LlmTokenGeneratedHandler.cs
│       │   ├── LlmSourcesFoundHandler.cs
│       │   ├── LlmResponseCompletedHandler.cs
│       │   ├── LlmResponseGaveUpHandler.cs
│       │   ├── LlmResponseRetryingHandler.cs
│       │   ├── SessionTitleUpdatedNotificationHandler.cs
│       │   └── SessionSummaryUpdatedNotificationHandler.cs
│       └── Services/INotificationService.cs
│
├── WebClient/                                  # Angular 21 SPA
│   └── src/app/
│       ├── core/
│       │   ├── api/
│       │   │   ├── api.interceptor.ts
│       │   │   └── chat.service.ts
│       │   ├── auth/
│       │   │   ├── auth.guard.ts
│       │   │   └── keycloak.service.ts
│       │   ├── config/config.service.ts
│       │   └── signalr/notification.service.ts
│       ├── features/
│       │   ├── chat/
│       │   │   ├── chat.component.ts
│       │   │   ├── message-input/
│       │   │   └── message-list/
│       │   └── sessions/
│       │       ├── session-item/
│       │       └── session-list/
│       ├── models/
│       │   ├── message.model.ts
│       │   └── session.model.ts
│       ├── store/
│       │   ├── message.store.ts                # NgRx SignalStore
│       │   └── session.store.ts
│       └── shared/
│           ├── confirm-dialog/
│           └── new-chat-dialog/
│
├── BuildingBlocks/
│   ├── BuildingBlocks.Contracts/               # Shared event contracts
│   │   ├── LlmEvents/
│   │   ├── SessionEvents/
│   │   ├── DocumentEvents/
│   │   └── Models/ChatTurn.cs
│   └── BuildingBlocks.Core/                    # DDD base classes
│       ├── BaseAggregate.cs
│       ├── IEventStoreRepository.cs
│       └── IReadOnlyEventStore.cs
│
├── Kong/kong.yml                               # Declarative API gateway config
├── Keycloak/realm-export.json                  # Pre-configured OAuth2 realm
├── Postgres/
│   ├── init-multiple-databases.sh              # Multi-database initialization
│   └── rag-schema.sql                          # pgvector tables + HNSW index
├── RabbitMQ/rabbitmq-definitions.json          # Queue/exchange topology
├── docker-compose.yml
├── docker-compose.override.yml
└── AiChatPlatform.slnx                         # .NET solution file
```

---

## Development Notes

**RabbitMQ Queue Topology** (pre-configured in `rabbitmq-definitions.json`):

| Queue | Producer | Consumer |
|---|---|---|
| `llm-requests` | ChatService (Saga) | AiService.Worker |
| `llm-summarization` | ChatService (Saga) | AiService.Worker |
| `llm-tokens.notificationservice` | AiService.Worker | NotificationService |
| `llm-completed.chatservice` | AiService.Worker | ChatService (Saga) |
| `llm-completed.notificationservice` | AiService.Worker | NotificationService |
| `llm-gave-up.chatservice` | AiService.Worker | ChatService (Saga) |
| `llm-gave-up.notificationservice` | AiService.Worker | NotificationService |
| `llm-retrying.notificationservice` | AiService.Worker | NotificationService |
| `document-uploads` | DocumentService | DocumentIngestion.Worker |
| `document-indexed` | DocumentIngestion.Worker | NotificationService |
| `session-notifications` | ChatService | NotificationService (fanout exchange) |

**Marten Configuration:**
- Event store mode: Inline projections (synchronous, always consistent)
- Async daemon: Configured in HotCold mode (unused unless async projections added)
- Optimistic concurrency: Retry policy with jittered cooldown (50ms, 100ms, 250ms)

**Keycloak Realm:**
- Auto-imported from `realm-export.json` on first start
- Client: `aichat-web` (Authorization Code + PKCE flow)
- Test user: `testuser` / `password`
- Realm reset: Delete `keycloak-data` volume, restart container

**pgvector Extension:**
- Enabled via `init-multiple-databases.sh` when PostgreSQL starts
- HNSW index created by `rag-schema.sql` (approximate nearest neighbor)
- Requires PostgreSQL 15+ with pgvector extension installed

**Stopping Services:**
```bash
docker compose down          # Stop containers, preserve volumes
docker compose down -v       # Stop containers, delete all volumes (full reset)
```

---

## License

MIT

---

## Contributing

This is a portfolio/demonstration project showcasing modern .NET microservices architecture. Issues and pull requests are welcome for bug fixes and improvements.

**Before submitting a PR:**
- Add tests for new functionality
- Ensure all services build and start via `docker compose up`
- Update this README if adding features or changing behavior

---

**Built as a demonstration of production-grade .NET microservices architecture with AI integration** 🚀
