# AiChatPlatform

AiChatPlatform is a modern, scalable, event-driven ecosystem for real-time AI-assisted chat. It leverages **Event Sourcing**, **CQRS**, and **Sagas** to deliver a responsive, decoupled architecture.

## 🚀 Technologies Used
*   **Backend**: .NET 10 Web API & Worker services
*   **Frontend**: Angular (v19+) with Material Design
*   **Messaging**: [Wolverine](https://wolverine.netlify.app/) (RabbitMQ provider)
*   **Database**: PostgreSQL 15 with [Marten](https://martendb.io/) (Event Store & Projections)
*   **Real-time Notifications**: ASP.NET Core SignalR
*   **Authentication**: [Keycloak](https://www.keycloak.org/) (OAuth2 / OIDC)
*   **AI Engine**: [Ollama](https://ollama.com/) (Llama3) & OpenAI compatibility
*   **API Gateway**: [Kong](https://docs.konghq.com/)

---

## 🏗️ Architecture Overview

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

## 📂 Project Structure

1.  **`ChatService`**: Core domain logic using **Marten** for event sourcing. Manages chat sessions and message aggregates.
2.  **`AiService`**: Decoupled worker that interacts with LLMs (Ollama/OpenAI) via `Microsoft.Extensions.AI`.
3.  **`NotificationService`**: ASP.NET Core SignalR hub that bridges the asynchronous event bus to the client.
4.  **`WebClient`**: Modern Angular SPA with specialized SignalR transport management for gateway compatibility.
5.  **`BuildingBlocks`**: Shared contracts, base entities, and event store abstractions.

---

## 🔬 Advanced Technical Highlights

### 1. Resilient Saga Orchestration
The `ConversationSaga` (Wolverine) ensures reliable interaction between users and the AI:
- **Message Queuing**: If a user sends messages while the AI is currently generating a response, the saga queues these requests and processes them sequentially once the current generation completes.
- **Fail-Safe Terminal States**: Handles `SessionDeletedEvent` by proactively canceling active AI requests and cleaning up saga state.

### 2. Deterministic Token Streaming
Real-time delivery is hardened against race conditions:
- **StreamBufferService**: The `NotificationService` tracks expected vs. delivered tokens. If the "Completion" signal arrives via RabbitMQ before all tokens have been pushed to SignalR, it buffers the terminal signal until the stream is complete.
- **Transport Reliability**: The WebClient strategically uses **Long Polling** and **Server-Sent Events** to maintain stable connections through the Kong Gateway.

### 3. Event Sourcing & Concurrency
- **Optimistic Concurrency**: Marten enforces stream versioning. Any attempt to update a session or message with an stale version results in a `ConcurrencyException`, preventing "lost updates" in a distributed environment.
- **Inline Projections**: Read models (Session lists, Message history) are updated **In-Line** with the event storage, providing strong consistency for the primary user view.

---

## 🏃 Getting Started

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Quick Start (Docker)
```bash
docker-compose up --build -d
```
All services are accessible via the **Kong Gateway** at `http://localhost:8000`:
*   **Web Application**: `http://localhost:8000/`
*   **API Documentation (Scalar)**: `http://localhost:8000/scalar`
*   **SignalR Hub**: `http://localhost:8000/hubs/chat`
*   **Auth (Keycloak)**: `http://localhost:8080` (User: `testuser` / Pwd: `password`)

---

## 🛠️ Design Principles
*   **Isolation**: The AI processing is physically and logically decoupled from the Chat management via RabbitMQ.
*   **Observability**: Detailed event tracking across the entire lifecycle (Requested -> Tokens -> Completed/GaveUp).
*   **Performance**: Optimized for sub-second feedback using direct token streaming.