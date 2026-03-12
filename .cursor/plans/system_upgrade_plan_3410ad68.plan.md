---
name: System Upgrade Plan
overview: A phased plan to fix critical issues, improve performance, and upgrade the SocialTechsy system from its current 5.25/10 production readiness to 8+/10, organized into 4 phases by priority.
todos:
  - id: p0-redis-seq
    content: "Phase 0.1: Fix Redis sequence sync on startup to prevent duplicate IDs after Redis restart"
    status: completed
  - id: p0-presence
    content: "Phase 0.2: Replace PresenceHub static ConcurrentDictionary with Redis-backed presence service"
    status: completed
  - id: p0-validation
    content: "Phase 0.3: Add input validation, message size limits, and rate limiting to ChatHub"
    status: completed
  - id: p1-mediatr
    content: "Phase 1.1: Add MediatR pipeline to Application layer, convert all handlers to IRequestHandler"
    status: completed
  - id: p1-chathub
    content: "Phase 1.2: Extract ChatHub business logic (445 lines) into Application layer command handlers"
    status: completed
  - id: p1-getall
    content: "Phase 1.3: Remove GetAllAsync and audit all repositories for unbounded queries"
    status: completed
  - id: p2-n1
    content: "Phase 2.1: Fix N+1 query in GetUserConversationsAsync using MongoDB aggregation"
    status: completed
  - id: p2-redis-batch
    content: "Phase 2.2: Add Redis IBatch for bulk unread count operations"
    status: completed
  - id: p2-broadcast
    content: "Phase 2.3: Fix ChatHub broadcast storm - use conversation groups instead of per-user sends"
    status: completed
  - id: p2-cursor
    content: "Phase 2.4: Implement cursor-based pagination for MongoDB messages"
    status: completed
  - id: p2-fts
    content: "Phase 2.5: Add SQL Server full-text search index for Questions"
    status: completed
  - id: p3-dlq
    content: "Phase 3.1: Add Dead Letter Queue to RabbitMQ with retry count limit"
    status: completed
  - id: p3-outbox
    content: "Phase 3.2: Implement Outbox pattern for MongoDB + RabbitMQ event consistency"
    status: completed
  - id: p3-idempotency
    content: "Phase 3.3: Add idempotency checks to RabbitMQ event consumer"
    status: completed
  - id: p3-health
    content: "Phase 3.4: Add health checks for all infrastructure dependencies"
    status: completed
  - id: p4-docker
    content: "Phase 4.1: Create Docker Compose setup for local development"
    status: completed
  - id: p4-otel
    content: "Phase 4.2: Add OpenTelemetry tracing and metrics"
    status: completed
  - id: p4-logging
    content: "Phase 4.3: Add Serilog structured logging with correlation IDs"
    status: completed
  - id: p4-gitignore
    content: "Phase 4.4: Fix .gitignore to exclude bin/obj/dll build artifacts"
    status: completed
isProject: false
---

# SocialTechsy System Upgrade Plan

## Current State: 5.25/10 Production Readiness

## Target State: 8+/10 Production Readiness

---

## Phase 0 - Critical Fixes (Must-do, breaks at any scale)

These are bugs that will cause **data corruption or system failure** even at small scale.

### 0.1 Fix Redis Sequence Sync on Startup

**Problem**: When Redis restarts, `INCR` starts from 0, causing duplicate MessageId/ConversationId and data corruption.

**File**: [backend/src/SocialTechsy.SocialNetwork.Infrastructure/Redis/RedisChatCacheService.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/Redis/RedisChatCacheService.cs)

**Fix**: Add a startup sync method and call it from DI registration:

```csharp
public async Task EnsureSequenceSyncedAsync(IMongoCollection<CounterDocument> counters)
{
    var allCounters = await counters.Find(_ => true).ToListAsync();
    foreach (var counter in allCounters)
    {
        await SyncSequenceAsync(counter.Id, counter.SequenceValue);
    }
}
```

Call from [DependencyInjection.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/DependencyInjection.cs) after Redis + MongoDB are both registered.

### 0.2 Fix PresenceHub Static Dictionary

**Problem**: `static ConcurrentDictionary<string, HashSet<string>> OnlineUsers` is process-local. With 2+ API instances (even behind Redis backplane), each instance sees different online users.

**File**: [backend/src/SocialTechsy.SocialNetwork.Api/Hubs/PresenceHub.cs](backend/src/SocialTechsy.SocialNetwork.Api/Hubs/PresenceHub.cs)

**Fix**: Create `RedisPresenceService` in Infrastructure/Redis/ that uses Redis SETs:

- `presence:online:{userId}` -> SET of connectionIds, with TTL 5 minutes
- Heartbeat via periodic ping from client
- `GetOnlineUsers()` uses Redis SCAN

### 0.3 Add Input Validation and Sanitization in ChatHub

**Problem**: `SendMessage(int conversationId, string content)` accepts raw content with no size limit or XSS sanitization.

**File**: [backend/src/SocialTechsy.SocialNetwork.Api/Hubs/ChatHub.cs](backend/src/SocialTechsy.SocialNetwork.Api/Hubs/ChatHub.cs)

**Fix**:

- Max message length: 10,000 characters
- HTML encode content before storage
- Rate limit: max 30 messages per minute per user

---

## Phase 1 - Architecture Cleanup (High impact, improves maintainability)

### 1.1 Add MediatR Pipeline

**Problem**: Controllers inject 8-9 handlers directly. No cross-cutting concerns pipeline.

**Changes**:

- Add `MediatR` NuGet package to Application project
- Convert all Commands/Queries to implement `IRequest<T>`
- Convert all Handlers to implement `IRequestHandler<TRequest, TResponse>`
- Add pipeline behaviors: `ValidationBehavior`, `LoggingBehavior`
- Simplify all controller constructors to single `IMediator` dependency

**Files affected**:

- [Application/Application.csproj](backend/src/SocialTechsy.SocialNetwork.Application/SocialTechsy.SocialNetwork.Application.csproj) - add MediatR
- All files in `Application/Commands/`, `Application/CommandHandlers/`, `Application/Queries/`, `Application/QueryHandlers/`
- All files in `Api/Controllers/`

### 1.2 Extract ChatHub Business Logic to Application Layer

**Problem**: ChatHub.cs (445 lines) contains authorization, persistence, DTO mapping, broadcasting logic - violates Clean Architecture.

**Approach**:

```
ChatHub (thin adapter)
  └── IMediator.Send(SendChatMessageCommand)
        └── SendChatMessageCommandHandler (Application layer)
              ├── IChatRepository.AddMessageAsync()
              ├── IChatMessageBroker.PublishAsync()
              └── Returns ChatMessageResult

ChatHub then handles ONLY SignalR broadcasting with the result.
```

**New files**:

- `Application/Commands/Chat/SendChatMessageCommand.cs`
- `Application/CommandHandlers/Chat/SendChatMessageCommandHandler.cs`
- `Application/Interfaces/Services/IChatBroadcaster.cs` (interface for SignalR broadcasting)

**Modified files**:

- [Api/Hubs/ChatHub.cs](backend/src/SocialTechsy.SocialNetwork.Api/Hubs/ChatHub.cs) - reduce to ~100 lines

### 1.3 Remove GetAllAsync and Enforce Pagination Everywhere

**Problem**: `QuestionRepository.GetAllAsync()` loads ALL records into memory - OOM at scale.

**File**: [backend/src/SocialTechsy.SocialNetwork.Infrastructure/Persistence/Repositories/QuestionRepository.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/Persistence/Repositories/QuestionRepository.cs)

**Fix**: Remove `GetAllAsync()` from `IQuestionRepository` interface and implementation. Audit all other repositories for similar methods. Ensure all list endpoints use `GetPaginatedAsync`.

---

## Phase 2 - Performance Optimization (Fixes bottlenecks under load)

### 2.1 Fix N+1 Query in GetUserConversationsAsync

**Problem**: For N conversations, makes N+1 MongoDB queries (1 list + N last-message lookups).

**File**: [backend/src/SocialTechsy.SocialNetwork.Infrastructure/MongoDB/MongoChatRepository.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/MongoDB/MongoChatRepository.cs) lines 130-171

**Fix**: Replace the foreach loop with a MongoDB aggregation pipeline using `$lookup` to fetch last messages in a single query, or batch-fetch all last messages using `$in` filter:

```csharp
var conversationIds = docs.Select(d => d.ConversationId).ToList();
var lastMessages = await _messages.Aggregate()
    .Match(m => conversationIds.Contains(m.ConversationId))
    .SortByDescending(m => m.SentDate)
    .Group(m => m.ConversationId, g => new { ConvId = g.Key, Last = g.First() })
    .ToListAsync();
```

### 2.2 Redis Pipeline for Batch Operations

**Problem**: Sequential Redis calls for incrementing unread counts per participant.

**File**: [backend/src/SocialTechsy.SocialNetwork.Infrastructure/Redis/RedisChatCacheService.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/Redis/RedisChatCacheService.cs)

**Fix**: Add batch method using `IBatch`:

```csharp
public async Task IncrementUnreadBatchAsync(IEnumerable<int> userIds, int conversationId)
{
    var batch = _db.CreateBatch();
    var tasks = userIds.Select(uid =>
        batch.StringIncrementAsync($"chat:unread:{uid}:{conversationId}")).ToList();
    batch.Execute();
    await Task.WhenAll(tasks);
}
```

### 2.3 Fix ChatHub Broadcast Storm

**Problem**: ChatHub iterates participants and sends 2N-1 messages instead of using conversation group.

**File**: [backend/src/SocialTechsy.SocialNetwork.Api/Hubs/ChatHub.cs](backend/src/SocialTechsy.SocialNetwork.Api/Hubs/ChatHub.cs) lines 135-150

**Fix**: Auto-join users to conversation groups on connection, then broadcast once:

```csharp
await Clients.Group($"conversation_{conversationId}")
    .SendAsync("ReceiveMessage", messageDto);
```

### 2.4 Cursor-Based Pagination for MongoDB Messages

**Problem**: Skip/Limit pagination is O(n) for large offsets.

**File**: [backend/src/SocialTechsy.SocialNetwork.Infrastructure/MongoDB/MongoChatRepository.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/MongoDB/MongoChatRepository.cs) lines 256-297

**Fix**: Add `GetMessagesAfterAsync(conversationId, afterMessageId, limit)` using `MessageId > afterMessageId` filter.

### 2.5 Add SQL Server Full-Text Search Index

**Problem**: `LIKE '%term%'` queries cause full table scans.

**File**: [backend/src/SocialTechsy.SocialNetwork.Infrastructure/Persistence/Repositories/QuestionRepository.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/Persistence/Repositories/QuestionRepository.cs) lines 58-63

**Fix**: Add EF Core migration for full-text index on Questions(Title, Body) and use `EF.Functions.FreeText()` or `EF.Functions.Contains()` instead of `LIKE`.

---

## Phase 3 - Reliability and Distributed Systems (Production hardening)

### 3.1 Add Dead Letter Queue to RabbitMQ

**Problem**: `BasicNackAsync` with `requeue: true` causes infinite retry loops for poison messages.

**Files**:

- [backend/src/SocialTechsy.SocialNetwork.Infrastructure/RabbitMQ/ChatMessageConsumerService.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/RabbitMQ/ChatMessageConsumerService.cs)
- [backend/src/SocialTechsy.SocialNetwork.Infrastructure/RabbitMQ/RabbitMqChatMessageBroker.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/RabbitMQ/RabbitMqChatMessageBroker.cs)

**Fix**:

- Declare `chat.events.dlq` queue
- Set `x-dead-letter-exchange` and `x-dead-letter-routing-key` on main queue
- Add retry count header, move to DLQ after 3 retries
- Add DLQ monitoring endpoint

### 3.2 Implement Outbox Pattern for Event Consistency

**Problem**: Message saved to MongoDB but RabbitMQ publish can silently fail, causing lost notifications.

**New files**:

- `Infrastructure/MongoDB/Models/OutboxDocument.cs`
- `Infrastructure/MongoDB/OutboxProcessor.cs` (BackgroundService)

**Approach**: Write message + outbox event in a MongoDB transaction. Background worker polls outbox and publishes to RabbitMQ.

### 3.3 Add Idempotency to Event Consumer

**Problem**: Re-queued messages can be processed multiple times, creating duplicate notifications.

**File**: [backend/src/SocialTechsy.SocialNetwork.Infrastructure/RabbitMQ/ChatMessageConsumerService.cs](backend/src/SocialTechsy.SocialNetwork.Infrastructure/RabbitMQ/ChatMessageConsumerService.cs)

**Fix**: Add `MessageId` to `ChatEvent`, check Redis for processed events before handling:

```csharp
var idempotencyKey = $"evt:processed:{chatEvent.MessageId}";
if (await _redis.KeyExistsAsync(idempotencyKey)) return;
// ... process
await _redis.StringSetAsync(idempotencyKey, "1", TimeSpan.FromHours(24));
```

### 3.4 Add Health Checks

**New file**: Health check registrations in `Program.cs`

**Fix**: Add health checks for SQL Server, MongoDB, Redis, RabbitMQ using `AspNetCore.HealthChecks.`* packages. Expose `/health` endpoint.

---

## Phase 4 - DevOps and Observability (Production deployment)

### 4.1 Docker Compose for Local Development

**New files**:

- `docker-compose.yml` (root) - SQL Server, MongoDB, Redis, RabbitMQ, API, Frontend
- `backend/Dockerfile`
- `frontend/Dockerfile`

### 4.2 Add OpenTelemetry

**Changes**:

- Add `OpenTelemetry.Extensions.Hosting` + exporters to API project
- Configure traces for HTTP, SignalR, EF Core, MongoDB, Redis
- Configure metrics for request duration, connection counts, queue depth
- Export to Prometheus + Jaeger (or OTLP collector)

### 4.3 Structured Logging

**Changes**:

- Add Serilog with structured JSON output
- Configure sinks: Console (dev), File, Seq/Loki (production)
- Add correlation IDs across SignalR + RabbitMQ events

### 4.4 Add .gitignore for Build Artifacts

**Problem**: Git status shows 50+ `bin/` and `obj/` files tracked as untracked. These should be gitignored.

**Fix**: Add proper `bin/`, `obj/`, `.dll`, `.pdb`, `.exe` patterns to `.gitignore`.

---

## Phase Dependency Diagram

```mermaid
graph TD
    P0_1["0.1 Redis Sequence Sync"]
    P0_2["0.2 PresenceHub Redis"]
    P0_3["0.3 Input Validation"]

    P1_1["1.1 Add MediatR"]
    P1_2["1.2 Extract ChatHub Logic"]
    P1_3["1.3 Remove GetAllAsync"]

    P2_1["2.1 Fix N+1 Query"]
    P2_2["2.2 Redis Pipeline Batch"]
    P2_3["2.3 Fix Broadcast Storm"]
    P2_4["2.4 Cursor Pagination"]
    P2_5["2.5 Full-Text Search"]

    P3_1["3.1 Dead Letter Queue"]
    P3_2["3.2 Outbox Pattern"]
    P3_3["3.3 Idempotency"]
    P3_4["3.4 Health Checks"]

    P4_1["4.1 Docker Compose"]
    P4_2["4.2 OpenTelemetry"]
    P4_3["4.3 Structured Logging"]
    P4_4["4.4 Fix .gitignore"]

    P0_1 --> P2_2
    P0_2 --> P2_3
    P1_1 --> P1_2
    P1_2 --> P2_3
    P2_2 --> P3_2
    P3_1 --> P3_3
    P3_4 --> P4_2
    P4_4 --> P4_1
```



## Estimated Impact


| Phase   | Duration | Score Before | Score After |
| ------- | -------- | ------------ | ----------- |
| Phase 0 | 2-3 days | 5.25         | 6.0         |
| Phase 1 | 5-7 days | 6.0          | 7.0         |
| Phase 2 | 5-7 days | 7.0          | 8.0         |
| Phase 3 | 5-7 days | 8.0          | 8.5         |
| Phase 4 | 3-5 days | 8.5          | 9.0         |


**Total estimated: 20-29 working days for 5.25 -> 9.0/10**