# AiChatPlatform

A production-grade, event-driven AI chat platform built on .NET 10 and Angular 21. The system delivers real-time, token-by-token AI responses using a fully decoupled microservices architecture — event sourcing, CQRS, Wolverine sagas, and SignalR streaming, all wired together through RabbitMQ and exposed via Kong.

---

## Table of Contents

- [Architecture](#architecture)
- [Services](#services)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Running Services Individually](#running-services-individually)
- [Configuration Reference](#configuration-reference)
- [API Reference](#api-reference)
- [End-to-End Message Flow](#end-to-end-message-flow)
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
        NotifApi["NotificationService (SignalR)"]
        AiWorker["AiService (Worker)"]
    end

    subgraph "Infrastructure"
        Marten[Marten/Postgres]
        Rabbit[RabbitMQ]
        Ollama[Ollama LLM]
    end

    User --> Kong
    Kong --> ChatApi
    Kong --> NotifApi
    Kong --> WebClient[Angular WebClient]
    
    ChatApi --> Marten
    ChatApi <--> Rabbit
    NotifApi <--> Rabbit
    AiWorker <--> Rabbit
    AiWorker --> Ollama
    
    ChatApi --> Keycloak
    NotifApi --> Keycloak
    WebClient --> Keycloak
```

```

Infrastructure
──────────────
  PostgreSQL :5432   — Marten event store + projections (aichat db) + Keycloak (keycloak db)
  Keycloak :8080     — OAuth2 / OIDC identity provider
  Ollama :11434      — Local LLM runtime (llama3 pulled on first start)
```

---

## Services

### ChatService
The core domain service. Handles session and message lifecycle through event sourcing — every user action is stored as an immutable domain event. Wolverine sagas orchestrate the multi-step AI response flow including queuing, retries, and persistence of the AI response message.

**Responsibilities:**
- Start, update, and close chat sessions (`SessionAggregate`)
- Save user messages as domain events (`MessageAggregate`)
- Maintain `ConversationSaga` per session — routes LLM requests, serialises concurrent messages, handles LLM failures
- Build conversation history prompts via `PromptBuilder` (last 20 messages)
- Serve read queries via Marten projections (`ConversationProjection`, `MessageProjection`)

### AiService.Worker
A headless background worker with no HTTP surface. Listens for `LlmResponseRequestedEvent` from RabbitMQ, calls the configured LLM (Ollama or OpenAI), and publishes tokens in real time. Implements up to 3 retry attempts with exponential backoff before giving up.

**Responsibilities:**
- Consume LLM requests from the `llm-requests` queue
- Stream tokens back as `LlmTokenGeneratedEvent` per token
- Publish `LlmResponseCompletedEvent` with the full response and token count
- Publish `LlmResponseGaveUpEvent` with a reason code on terminal failure

### NotificationService
A lightweight SignalR hub that bridges the async event bus to connected browser clients. Stateless beyond the SignalR connection — it receives events from RabbitMQ and forwards them to the correct user's connection.

**Responsibilities:**
- Authenticate clients via Keycloak JWT (same token as ChatService)
- Fan out `ReceiveToken`, `ReceiveCompleted`, and `ReceiveGaveUp` SignalR messages
- Buffer partial tokens to avoid excessive round-trips (`StreamBufferService`)

### WebClient
An Angular 21 SPA using standalone components, signals-based state management via NgRx SignalStore, and Angular Material for UI. Connects to the backend exclusively through Kong.

**Key implementation details:**
- `SessionStore` and `MessageStore` — NgRx SignalStore with RxJS interop
- `NotificationService` — SignalR client managing the token stream lifecycle
- `KeycloakService` — wraps keycloak-js for token refresh and silent SSO
- `ApiInterceptor` — attaches Bearer tokens to all outbound HTTP requests
- Optimistic UI — user messages appear immediately before the server confirms

---

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| Backend runtime | .NET | 10 |
| Frontend framework | Angular | 21 |
| Message bus | Wolverine + RabbitMQ | 5.19 / 3.x |
| Event store | Marten + PostgreSQL | latest / 15 |
| Real-time | ASP.NET Core SignalR | .NET 10 |
| Auth | Keycloak | 26.1 |
| AI runtime (local) | Ollama (llama3) | latest |
| AI runtime (cloud) | OpenAI API | gpt-4.1 |
| API gateway | Kong | 3.6 |
| State management | NgRx SignalStore | 21 |
| Container runtime | Docker Compose | v2 |

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (v4.x or later)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — only required for local development outside Docker
- [Node.js 22+](https://nodejs.org/) and npm 11+ — only required for local frontend development

> **GPU note:** Ollama will run on CPU if no GPU is available, but first-token latency will be high (~10–30 s depending on hardware). For development, pointing at an OpenAI-compatible API is faster — see [Configuration Reference](#configuration-reference).

---

## Quick Start

```bash
# Clone and start everything
git clone <repo-url>
cd AiChatPlatform
docker compose up --build -d
```

On first run, Docker Compose will:
1. Start PostgreSQL and initialise two databases (`aichat`, `keycloak`)
2. Start Keycloak and import the `aichat` realm (test user pre-configured)
3. Start RabbitMQ with pre-defined exchanges and queues
4. Build and start all three backend services
5. Pull the `llama3` model into Ollama (this takes several minutes on first run — watch progress with `docker compose logs ollama-pull -f`)
6. Build and start the Angular WebClient
7. Start Kong with the declarative config

Once all health checks pass, the application is available at:

| URL | Description |
|---|---|
| `http://localhost:8000` | Web application (via Kong) |
| `http://localhost:8000/scalar` | Interactive API docs (Scalar UI) |
| `http://localhost:8000/hubs/chat` | SignalR hub endpoint |
| `http://localhost:8080` | Keycloak admin console |
| `http://localhost:15672` | RabbitMQ management UI |
| `http://localhost:5432` | PostgreSQL (direct) |
| `http://localhost:11434` | Ollama API (direct) |

### Default credentials

| Service | Username | Password |
|---|---|---|
| Web app (test user) | `testuser` | `password` |
| Keycloak admin | `admin` | `admin` |
| RabbitMQ | `rabbitmq` | `LUUcvHJHv22GE7e` |
| PostgreSQL | `postgres` | `postgres` |

---

## Running Services Individually

### Backend (without Docker)

Each service requires its dependencies (PostgreSQL, RabbitMQ, Keycloak, Ollama) to be running. The easiest way is to start only the infrastructure:

```bash
docker compose up postgres rabbitmq keycloak ollama -d
```

Then run each service via the .NET CLI or Visual Studio:

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
```

### Frontend (without Docker)

```bash
cd WebClient
npm install
npm start        # serves at http://localhost:4200
```

The Angular app reads its backend URLs from `src/assets/config.json`. For local development pointing at local services, update:

```json
{
  "apiUrl": "http://localhost:5000",
  "notificationUrl": "http://localhost:5001",
  "keycloakUrl": "http://localhost:8080",
  "keycloakRealm": "aichat",
  "keycloakClientId": "aichat-web"
}
```

### Building the solution

```bash
dotnet build AiChatPlatform.slnx
```

> **Note:** The solution file uses the `.slnx` format introduced in Visual Studio 2022 17.12. Earlier versions of Visual Studio will not open it. Use `dotnet` CLI or VS Code with the C# extension instead.

---

## Configuration Reference

All backend services are configured via environment variables, which map to typed `Options` classes.

### ChatService

| Variable | Description | Default (Docker) |
|---|---|---|
| `ConnectionStrings__Marten` | PostgreSQL connection string | `Host=postgres;...` |
| `Keycloak__Authority` | Keycloak realm URL | `http://host.docker.internal:8080/realms/aichat` |
| `Keycloak__Audience` | Expected JWT audience | `aichat-web` |
| `RabbitMQ__Uri` | RabbitMQ AMQP connection string | `amqp://rabbitmq:...@rabbitmq:5672` |
| `OpenApi__ServerUrl` | Public base URL for Scalar docs | `http://localhost:8000` |

### NotificationService

| Variable | Description |
|---|---|
| `Keycloak__Authority` | Same as ChatService |
| `Keycloak__Audience` | Same as ChatService |
| `RabbitMQ__Uri` | Same format as ChatService |

### AiService.Worker

| Variable | Description | Default (Docker) |
|---|---|---|
| `Ollama__BaseUrl` | Ollama base URL | `http://ollama:11434` |
| `Ollama__Model` | Model name to use | `llama3` |
| `OpenAI__ApiKey` | OpenAI API key (leave empty to use Ollama) | — |
| `OpenAI__Model` | OpenAI model name | `gpt-4.1-2025-04-14` |
| `RabbitMQ__Uri` | Same format as ChatService | — |

To switch from Ollama to OpenAI, set `OpenAI__ApiKey` to a valid key in `docker-compose.override.yml`:

```yaml
aiserviceworker:
  environment:
    - OpenAI__ApiKey=sk-...
```

---

## API Reference

All API calls go through Kong at `http://localhost:8000`. Every endpoint requires a valid Keycloak Bearer token. The full interactive spec is available at `http://localhost:8000/scalar`.

### Chat endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/chat/start` | Create a new chat session |
| `POST` | `/api/chat/message` | Send a user message |
| `POST` | `/api/chat/close` | Close and archive a session |
| `GET` | `/api/chat/user/conversations` | List all sessions for the current user |
| `GET` | `/api/chat/conversation/{sessionId}` | Get session metadata |
| `GET` | `/api/chat/conversation/{sessionId}/messages` | Get all messages in a session |

**Start a session:**
```json
POST /api/chat/start
{ "title": "My first chat" }

→ 202 Accepted
{ "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

**Send a message:**
```json
POST /api/chat/message
{ "sessionId": "3fa85f64-...", "content": "What is event sourcing?" }

→ 202 Accepted
```

The response streams back via SignalR — the HTTP response for `sendMessage` is just an acknowledgement.

### SignalR hub

Connect to `/hubs/chat` with a valid Bearer token. The hub pushes three event types:

| Event | Payload | Description |
|---|---|---|
| `ReceiveToken` | `{ requestId, sessionId, token }` | A single streamed token from the LLM |
| `ReceiveCompleted` | `{ requestId, sessionId }` | LLM finished — full response now persisted |
| `ReceiveGaveUp` | `{ requestId, sessionId, reason }` | LLM failed — reason is one of `LLM_ERROR`, `LLM_TIMEOUT`, `MAX_RETRIES_EXCEEDED`, `SESSION_DELETED` |

---

## End-to-End Message Flow

```
1. User types a message and hits Send
   └─► Angular MessageStore.sendMessage()
       └─► POST /api/chat/message  (HTTP via Kong)

2. ChatService.SendMessageHandler
   └─► Creates MessageAggregate → appends MessageCreatedEvent to Marten event stream
   └─► Marten MessageProjection stores a MessageDto read model (inline, synchronous)

3. ConversationSaga receives MessageCreatedEvent
   └─► If not currently processing: calls PromptBuilder.BuildAsync()
       └─► Queries last 20 MessageDtos ordered by SentAt
       └─► Builds conversation history string
   └─► Publishes LlmResponseRequestedEvent → RabbitMQ "llm-requests" queue
   └─► If already processing: enqueues message ID in PendingMessageIds

4. AiService.Worker.GenerateAiResponseHandler receives LlmResponseRequestedEvent
   └─► Calls IChatClient.GetStreamingResponseAsync (Ollama or OpenAI)
   └─► For each token: publishes LlmTokenGeneratedEvent → RabbitMQ
   └─► On completion: publishes LlmResponseCompletedEvent → RabbitMQ
   └─► On failure (up to 3 attempts): publishes LlmResponseRetryingEvent, then
       LlmResponseGaveUpEvent with reason code

5. NotificationService receives LlmTokenGeneratedEvent
   └─► Looks up the user's SignalR connection
   └─► Calls Hub.Clients.User().SendAsync("ReceiveToken", ...)

6. Angular NotificationService receives ReceiveToken
   └─► MessageStore.appendToken(token) → streamingContent signal updates
   └─► Chat UI renders token immediately via Angular signals

7. ConversationSaga receives LlmResponseCompletedEvent
   └─► Creates a new MessageAggregate (Role = Assistant) and saves to event store
   └─► Checks PendingMessageIds — if messages were queued, starts processing next

8. NotificationService receives LlmResponseCompletedEvent
   └─► Sends "ReceiveCompleted" to client

9. Angular receives ReceiveCompleted
   └─► MessageStore.finalizeStream() — moves streamingContent to messages array
   └─► isStreaming set to false, input re-enabled
```

---

## Domain Model

```
SessionAggregate
├─ Id: Guid
├─ UserId: Guid
├─ Title: string
├─ StartedAt: DateTime
├─ LastActivityAt: DateTime
└─ DeletedAt: DateTime?

Events: SessionCreatedEvent, SessionUpdatedEvent, SessionDeletedEvent

MessageAggregate
├─ Id: Guid
├─ SessionId: Guid  (stream identity)
├─ SenderId: Guid
├─ Content: string
├─ Role: MessageRole (User=0, Assistant=1, System=2)
└─ SentAt: DateTime

Events: MessageCreatedEvent

ConversationSaga  (Wolverine persistent saga, keyed by SessionId)
├─ IsProcessing: bool
├─ ActiveRequestId: Guid?
└─ PendingMessageIds: Queue<Guid>
```

**Projections (Marten inline):**

| Projection | Type | Description |
|---|---|---|
| `ConversationProjection` | `MultiStreamProjection<ConversationDto>` | Aggregates session + message events into a read model per session |
| `MessageProjection` | `EventProjection` | Stores one `MessageDto` per `MessageCreatedEvent` |

Both projections run inline (synchronous with the write), so read queries are always consistent.

---

## Project Structure

```
AiChatPlatform/
├── AiService/
│   ├── AiService.Application/        # GenerateAiResponseHandler (retry logic, streaming)
│   ├── AiService.Infrastructure/     # IChatClient wiring (Ollama / OpenAI), options
│   └── AiService.Worker/             # Worker host, Wolverine listener setup
│
├── BuildingBlocks/
│   ├── BuildingBlocks.Contracts/     # Shared events (LlmResponse*), ChatTurn value object
│   └── BuildingBlocks.Core/          # BaseAggregate, BaseEntity, BaseEvent, IEventStoreRepository
│
├── ChatService/
│   ├── ChatService.Api/              # Controllers, JWT auth, Scalar/OpenAPI setup
│   ├── ChatService.Application/      # CQRS handlers, ConversationSaga, PromptBuilder
│   ├── ChatService.Domain/           # SessionAggregate, MessageAggregate, domain events
│   └── ChatService.Infrastructure/   # Marten config, projections, Wolverine/RabbitMQ wiring
│
├── NotificationService/
│   ├── NotificationService.Api/      # SignalR hub (ChatHub), StreamBufferService
│   ├── NotificationService.Application/ # Event handlers: token, completed, gave-up, retrying
│   └── NotificationService.Infrastructure/
│
├── WebClient/                        # Angular 21 SPA
│   └── src/app/
│       ├── core/                     # api, auth (Keycloak), config, signalr
│       ├── features/chat/            # Chat UI components + message list/input
│       ├── features/sessions/        # Session list sidebar
│       ├── models/                   # Message, Session TypeScript models
│       └── store/                    # MessageStore, SessionStore (NgRx SignalStore)
│
├── Kong/kong.yml                     # Declarative gateway config
├── Keycloak/realm-export.json        # Pre-configured realm with aichat-web client + test user
├── RabbitMQ/rabbitmq-definitions.json # Pre-defined exchanges and queues
├── Postgres/init-multiple-databases.sh
├── docker-compose.yml
├── docker-compose.override.yml       # Local overrides (ports, dev settings)
└── AiChatPlatform.slnx               # Solution file (VS 2022 17.12+ / dotnet CLI)
```

---

## Development Notes

**RabbitMQ queue topology** is pre-configured via `RabbitMQ/rabbitmq-definitions.json`. The relevant queues are:

| Queue | Producer | Consumer |
|---|---|---|
| `llm-requests` | ChatService (Wolverine publish) | AiService.Worker |
| `llm-tokens.notificationservice` | AiService.Worker | NotificationService |
| `llm-completed.chatservice` | AiService.Worker | ChatService (saga) |
| `llm-completed.notificationservice` | AiService.Worker | NotificationService |
| `llm-gave-up.chatservice` | AiService.Worker | ChatService (saga) |
| `llm-gave-up.notificationservice` | AiService.Worker | NotificationService |

**Marten async daemon** is configured in HotCold mode for the projection daemon. For development, inline projections handle all read model updates synchronously, so the daemon is a no-op unless you add async projections.

**Keycloak realm** is imported automatically on first start from `Keycloak/realm-export.json`. The `aichat-web` client is configured for the Authorization Code + PKCE flow. If you reset the Keycloak database volume, the realm will be re-imported on next start.

**Solution format:** `AiChatPlatform.slnx` uses the new XML-based solution format (`.slnx`) available from Visual Studio 2022 17.12. Use `dotnet build AiChatPlatform.slnx` if your IDE doesn't support it yet.

**Stopping everything:**
```bash
docker compose down          # stops containers, preserves volumes
docker compose down -v       # stops containers and deletes all volumes (full reset)
```
