# AiChatPlatform

AiChatPlatform is a modern, scalable backend service for managing chat sessions and messages. It is designed using advanced architectural patterns including **Domain-Driven Design (DDD)**, **Command Query Responsibility Segregation (CQRS)**, and **Event Sourcing**.

## 🚀 Technologies Used
*   **Framework**: .NET 10 Web API
*   **Architecture Patterns**: DDD, CQRS, Event Sourcing
*   **Message Broker / CQRS Pipeline**: [Wolverine](https://wolverine.netlify.app/)
*   **Event Store & Read Models**: [Marten](https://martendb.io/) (backed by PostgreSQL)
*   **Database**: PostgreSQL 15
*   **Authentication**: [Keycloak](https://www.keycloak.org/) (OAuth2 / OpenID Connect)
*   **API Documentation**: OpenAPI, Scalar, Swagger UI
*   **Containerization**: Docker & Docker Compose

---

## 🏗️ Architecture & Project Structure

The solution is divided into cohesive layers reflecting clean architecture and domain-centric design:

1.  **`BuildingBlocks.Core`**
    *   Contains the foundational, reusable abstractions for the system (`BaseAggregate`, `BaseEntity`, `BaseEvent`, `IEventStoreRepository`).
    *   Provides utilities like `ApplyAndEnqueue` for seamlessly tracking uncommitted domain events.

2.  **`ChatService.Domain`**
    *   The core of the business logic.
    *   Contains `SessionAggregate` and `MessageAggregate` which govern the behavioral rules for opening a chat and sending messages.
    *   Contains defined domain events (e.g., `SessionCreatedEvent`, `MessageCreatedEvent`).

3.  **`ChatService.Application`**
    *   Implements the **CQRS** pattern.
    *   **Commands** (e.g., `StartChatCommand`, `SendMessageCommand`) and their respective Handlers for mutating application state.
    *   **Queries** (e.g., `GetConversationQuery`, `GetMessagesQuery`) for fetching data from projections.
    *   Data Transfer Objects (DTOs).

4.  **`ChatService.Infrastructure`**
    *   Data persistence and technology-specific implementations.
    *   **Marten Configuration**: Event sourcing wire-up (`WolverineMartenConfiguration`).
    *   **Event Store Repositories**: Concrete implementations of `IEventStoreRepository` and purely asynchronous `IReadOnlyEventStore`.
    *   **Projections**: Marten projections (`ConversationProjection`, `MessageProjection`) that subscribe to the event stream and build optimized read-models natively in PostgreSQL.

5.  **`ChatService.Api`**
    *   The composition root and HTTP entry point.
    *   RESTful endpoints registered in `ChatController`.
    *   Configures Keycloak JWT Bearer authentication globally.

---

## 🏃 Getting Started

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or equivalent container runtime)

### Running Locally with Docker Compose
The easiest way to run the entire stack (PostgreSQL, Keycloak, and the API) is via Docker Compose.

1.  Open a terminal in the solution root directory.
2.  Run the docker-compose command:
    ```bash
    docker-compose up --build -d
    ```
3.  The following services will be available:
    *   **API**: `http://localhost:5000` (mapped via Docker) or directly via Kestrel on the host port.
    *   **Keycloak**: `http://localhost:8080`
    *   **PostgreSQL**: `localhost:5432`

### Testing the API (Swagger/Scalar)
1. Navigate to `/scalar/v1` or `/swagger` on the API host.
2. The endpoints are protected by OAuth2 Keycloak Authentication. Use the integrated UI authorization button to log in.
    *   *Note: Keycloak provisions a dynamic realm on startup. Be sure to import or configure the test users in your `realm-export.json` if necessary.*
3.  **Available Endpoints**:
    *   `POST /api/chat/start`: Start a new chat session.
    *   `POST /api/chat/message`: Send a message to an existing session.
    *   `POST /api/chat/close`: Terminate a session.
    *   `GET /api/chat/conversation/{sessionId}`: Fetch chat metadata.
    *   `GET /api/chat/conversation/{sessionId}/messages`: Fetch all chronological messages in a session.
    *   `GET /api/chat/user/{userId}/conversations`: List all active conversations for a specific user.

## 🛠️ Code Styles & Guidelines
*   **Primary Constructors**: Used preferentially across DI-injected classes.
*   **File-Scoped Namespaces**: Enforced to reduce nesting depth.
*   **Async/Await**: The data-access layer enforces fully asynchronous operations preventing thread-blocking scenarios.