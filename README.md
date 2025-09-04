
# Chat Support Backend System

This project is a backend system designed to manage a customer support chat service. It handles incoming chat requests, queues them based on dynamic team capacity and business hours, and assigns them to available agents according to a seniority-based priority system. The entire application is built using .NET, following CQRS principles with Autofac for dependency injection, and includes a suite of unit and integration tests to ensure correctness.

## Key Features

-   **Dynamic Shift Management:** The system operates on a 3-shift basis (Team A, B, C), automatically activating the correct team based on the time of day.
-   **Capacity-Based Queueing:** The maximum queue size is dynamically calculated based on the active team's capacity (defined as `TeamCapacity * 1.5`).
-   **Office Hours & Overflow Logic:** During peak office hours, if the main queue is full, an overflow team is automatically activated to handle the excess load. This team is deactivated when the load subsides.
-   **Seniority-Based Chat Assignment:** New chats are assigned in a specific round-robin order that prioritizes lower-seniority agents first (Junior -> Team Lead -> Mid-Level -> Senior) to keep senior staff free for escalations.
-   **Session Lifecycle Management:** The system manages the full lifecycle of a chat session, from creation and polling to automatic cleanup of abandoned sessions.
-   **CQRS Architecture:** The application logic is cleanly separated into Commands (actions that change state) and Queries (requests that read state).
-   **Highly Testable Design:** Built with Dependency Injection from the ground up, allowing for robust unit and integration testing.

## Architectural Overview

The system is designed with a clean separation of concerns, leveraging several key patterns and components.

### 1. CQRS (Command Query Responsibility Segregation)

The core business logic is organized into Commands and Queries.

-   **Commands:** Represent an intent to change the system's state.
    -   `StartChatSessionCommand`: Initiates a new chat session request.
    -   `PollChatSessionCommand`: A "keep-alive" signal from the client to keep their session active.
-   **Queries:** Represent a request to read data without changing state.
    -   `GetQueueStatusQuery`: Retrieves the current status of the chat queue.
    -   `GetChatSessionQuery`: Retrieves the details of a specific chat session.

### 2. Dependency Injection (with Autofac)

The application is wired together using Autofac. Components depend on abstractions (interfaces) rather than concrete implementations, making the system loosely coupled and easy to test. All services and repositories are registered as singletons for this implementation.

### 3. Core Services

-   **`ShiftManager`**: This is the "brain" of the system. It is responsible for:
    -   Determining the active shift team based on the current time (`IDateTimeProvider`).
    -   Calculating the current team's capacity and maximum queue size.
    -   Implementing the logic for activating and deactivating the overflow team based on queue load and office hours.
    -   Updating the `IsOnShift` status of all agents.
-   **`ChatAssignmentService`**: This service acts as the dispatcher. It finds the next available agent according to the defined seniority priority rules.
-   **`ChatMonitoringService` (`IHostedService`)**: This is a background service that runs continuously:
    -   It periodically calls `ShiftManager` to update the status of all agent shifts.
    -   It cleans up abandoned sessions (both `Queued` and `Active`) that have not been polled within the 3-second timeout.
    -   It processes the chat queue, assigning waiting sessions to available agents.

## API Endpoints

The API provides the following endpoints, which can be explored via Swagger UI when running the project.

### `POST /api/chat/start`

Initiates a new chat session.

**Request Body:**
```json
{
  "userId": "some-unique-user-id"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "sessionId": "a-unique-session-guid",
  "errorMessage": null,
  "queuePosition": 5
}
```

**Failure Response (400 Bad Request):** (When queue is full)
```json
{
  "success": false,
  "sessionId": null,
  "errorMessage": "Chat queue is full. Please try again later.",
  "queuePosition": 0
}
```

### `POST /api/chat/poll/{sessionId}`

Allows a client to keep their session alive while waiting in the queue or in an active chat.

**Success Response (200 OK):** An empty body.

**Failure Response (404 Not Found):** If the session ID does not exist or has been cleaned up.

### `GET /api/chat/status`

Retrieves the current status of the chat system.

**Success Response (200 OK):**
```json
{
  "currentQueueSize": 35, // Total Active + Queued sessions
  "maxQueueSize": 67,     // Main team queue + overflow buffer (if active)
  "totalCapacity": 45,    // Main team capacity + overflow capacity (if active)
  "isOfficeHours": true,
  "overflowActive": true
}
```

## How to Run

### Prerequisites

-   .NET 8 SDK (or the version targeted by the project).
-   An IDE like Visual Studio or VS Code.

### Steps

1.  Clone the repository.
2.  Open a terminal in the root directory.
3.  Navigate to the main API project folder (e.g., `cd ChatSupport.API`).
4.  Run `dotnet restore` to install dependencies.
5.  Run `dotnet run` to start the application.
6.  The API will be available at `https://localhost:xxxx` and `http://localhost:yyyy`.
7.  Access the Swagger UI for interactive API testing at `https://localhost:xxxx/swagger`.

## Testing Strategy

The project includes both unit and integration tests to ensure the correctness of the complex business logic.

### Unit Tests

Unit tests are used to test individual services in isolation. The most critical component, `ShiftManager`, is thoroughly tested.

-   **`ShiftManager_UnitTests.cs`**:
    -   Verifies that the correct base shift team (A, B, or C) is activated based on a mocked time.
    -   Verifies that the overflow team is correctly activated only when the queue is full AND it is during office hours.
    -   Verifies the hysteresis logic for deactivating the overflow team when the queue load drops below the 75% threshold.

### Integration Tests

Integration tests validate end-to-end flows, from the API endpoint down to the services and repositories. These tests use `WebApplicationFactory` to run the application in memory.

-   **`SessionLifecycle_RealTime_Test.cs`**:
    -   A no-mocking, real-time test that validates the complete session lifecycle.
    -   It creates a session, polls it, waits for 4 real-world seconds, and then polls again, asserting that the `ChatMonitoringService` has correctly cleaned up the abandoned session.
-   **`QueueStatus_DynamicTime_Test.cs`**:
    -   A smart, self-adapting test that validates the overflow logic.
    -   It first determines the current time to calculate the expected state (which team is active, is it office hours?).
    -   It then floods the API with enough requests to fill the queue and trigger the overflow logic.
    -   Finally, it calls the `/api/chat/status` endpoint and asserts that the API's response matches the expected state, correctly verifying the office hours rule.

### How to Run the Tests

1.  Open the solution in Visual Studio.
2.  Go to **Test -> Test Explorer**.
3.  Click **"Run All Tests"** in the Test Explorer window.
4.  Alternatively, from the root of the solution in a terminal, run the command: `dotnet test`.