# AiChatPlatform

AiChatPlatform is a modern, scalable backend service for managing chat sessions and messages. It is designed using advanced architectural patterns including **Domain-Driven Design (DDD)**, **Command Query Responsibility Segregation (CQRS)**, and **Event Sourcing**.

## 🚀 Technologies Used
*   **Framework**: .NET 10 Web API
*   **Architecture Patterns**: DDD, CQRS, Event Sourcing
*   **Message Broker / CQRS Pipeline**: [Wolverine](https://wolverine.netlify.app/)
*   **Event Store & Read Models**: [Marten](https://martendb.io/) (backed by PostgreSQL)
*   **Database**: PostgreSQL 15
*   **Authentication**: [Keycloak](https://www.keycloak.org/) (OAuth2 / OpenID Connect)
*   **AI Engine**: [Ollama](https://ollama.com/) (running Llama3)
*   **API Documentation**: OpenAPI, Scalar, Swagger UI
*   **Containerization**: Docker & Docker Compose

---

## 🏗️ Architecture & Project Structure

The solution is divided into cohesive layers reflecting clean architecture and domain-centric design:

1.  **`BuildingBlocks.Core`**
    *   Foundational abstractions (`BaseAggregate`, `BaseEvent`, `IEventStoreRepository`).
2.  **`ChatService.Domain`**
    *   Business logic, aggregates (`Session`, `Message`), and domain events.
3.  **`ChatService.Application`**
    *   CQRS Handlers, Sagas (`ConversationSaga`), and DTOs.
4.  **`ChatService.Infrastructure`**
    *   Marten/Wolverine configuration, Projections, and Repositories.
5.  **`ChatService.Api`**
    *   ASP.NET Core Controllers and Middleware.
6.  **`AiService.Worker`**
    *   Background worker for executing LLM requests via Ollama.
7.  **`BuildingBlocks.Contracts`**
    *   Shared cross-service events and message definitions.

---

## 🧠 AI Orchestration Flow

The system uses a **Wolverine Saga** (`ConversationSaga`) for asynchronous AI interactions:

1.  **User Message**: Appended to the event stream in `ChatService`.
2.  **Saga Reaction**: Triggered by `MessageCreatedEvent`, sends a request to `AiService`.
3.  **AI Response**: `AiService` streams tokens and publishes completion events.
4.  **Completion**: Saga persists the final AI response to the database.

---

## 🏃 Getting Started

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Running Locally with Docker Compose
```bash
docker-compose up --build -d
```
*   **API**: `http://localhost:5000`
*   **Keycloak**: `http://localhost:8080`
*   **PostgreSQL**: `localhost:5432`

### Testing the API (Swagger/Scalar)
1. Navigate to `/scalar/v1` or `/swagger`.
2. Authenticate using the `OAuth2` button (Test user: `testuser` / `password`).
3. **Available Endpoints**:
    *   `POST /api/chat/start`: Start a new chat session.
    *   `POST /api/chat/message`: Send a user message.
    *   `GET /api/chat/conversation/{sessionId}`: Fetch chat history.

## 🛠️ Code Styles & Guidelines
*   **Primary Constructors**: Used for DI.
*   **File-Scoped Namespaces**: Required.
*   **Async/Await**: Enforced across all layers.