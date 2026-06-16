# Architecture

## Internal Issue Tracking System (ITS)

**Version:** 1.0  
**Pattern:** Clean Architecture (Uncle Bob)  
**Last Updated:** June 2026

---

## 1. Layer Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ITS.Api                                     │
│  Controllers · Middleware · JWT Auth · Swagger · Serilog            │
│  Depends on: Application + Infrastructure                           │
├─────────────────────────────────────────────────────────────────────┤
│                     ITS.Infrastructure                              │
│  EF Core DbContext · AD/LDAP · File Storage · Background Jobs       │
│  Email Sender · Markdown Service · Ticket Sequences                 │
│  Depends on: Application                                            │
├─────────────────────────────────────────────────────────────────────┤
│                     ITS.Application                                 │
│  MediatR Commands & Queries · FluentValidation · Pipeline Behaviors │
│  Domain Event Handlers · Interfaces (ports)                        │
│  Depends on: Domain                                                 │
├─────────────────────────────────────────────────────────────────────┤
│                       ITS.Domain                                    │
│  Entities · Enums · Domain Events · Exceptions · Value Objects      │
│  Depends on: nothing                                                │
└─────────────────────────────────────────────────────────────────────┘
```

**Dependency Rule:** Each layer may only depend on layers below it. Domain has zero external dependencies.

---

## 2. Folder Structure

```
ITS.sln
├── src/
│   ├── ITS.Domain/
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs            ← Id + DomainEvents collection
│   │   │   ├── AuditableEntity.cs       ← + CreatedAt/By, UpdatedAt/By, SoftDelete, RowVersion
│   │   │   ├── IDomainEvent.cs
│   │   │   └── DomainEvent.cs           ← abstract record base
│   │   ├── Entities/
│   │   │   ├── Identity/                ← User, Department, Role, UserGlobalRole, AdGroupRoleMapping
│   │   │   ├── Projects/                ← Project, ProjectMember, IssueType, Priority, Label, CustomField*
│   │   │   ├── Workflow/                ← Workflow, WorkflowStatus, WorkflowTransition, Guards, Actions
│   │   │   ├── Tickets/                 ← Ticket, TicketLabel, TicketLink, TicketWatcher, CustomFieldValue, Resolution
│   │   │   ├── Content/                 ← Comment, CommentHistory, CommentMention, Attachment
│   │   │   ├── Audit/                   ← ActivityLog, AuditLog
│   │   │   └── Notifications/           ← Notification, NotificationPreference, EmailQueue
│   │   ├── DomainEvents/
│   │   │   ├── Tickets/                 ← TicketCreated, StatusChanged, Assigned
│   │   │   ├── Comments/                ← CommentAdded
│   │   │   └── Projects/                ← ProjectCreated
│   │   ├── Enums/                       ← RoleScope, StatusCategory, GuardType, ActivityType, ...
│   │   └── Exceptions/                  ← DomainException, WorkflowTransitionException
│   │
│   ├── ITS.Application/
│   │   ├── Common/
│   │   │   ├── Interfaces/              ← IApplicationDbContext, ICurrentUserService, IDateTimeService,
│   │   │   │                              IFileStorageService, INotificationService, IAuditService,
│   │   │   │                              IAdSyncService, IMarkdownService, ITicketSequenceService,
│   │   │   │                              IWorkflowEngine, IProjectAuthorizationService
│   │   │   ├── Behaviors/               ← ValidationBehavior, LoggingBehavior, PerformanceBehavior
│   │   │   ├── Exceptions/              ← NotFoundException, ForbiddenAccessException, ConflictException,
│   │   │   │                              ValidationException
│   │   │   └── Models/                  ← Result<T>, PaginatedList<T>
│   │   ├── Features/
│   │   │   ├── Tickets/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateTicket/    ← Command + Handler + Validator
│   │   │   │   │   ├── UpdateTicket/
│   │   │   │   │   ├── TransitionTicket/
│   │   │   │   │   ├── DeleteTicket/
│   │   │   │   │   ├── AssignTicket/
│   │   │   │   │   └── AddWatcher/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetTicket/       ← Query + Handler + DTO
│   │   │   │       ├── GetTickets/
│   │   │   │       └── GetKanbanBoard/
│   │   │   ├── Projects/  Comments/  Attachments/  Users/
│   │   │   ├── Workflows/  Notifications/  Reporting/  Audit/
│   │   └── DomainEventHandlers/         ← Handlers for domain events (notify, log activity)
│   │
│   ├── ITS.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/          ← One IEntityTypeConfiguration<T> per entity, by schema folder
│   │   │   ├── Interceptors/
│   │   │   │   ├── AuditableEntityInterceptor.cs    ← Sets audit fields on save
│   │   │   │   └── DomainEventDispatchInterceptor.cs ← Publishes domain events post-save
│   │   │   └── Migrations/
│   │   ├── Identity/
│   │   │   └── AdAuthenticationService.cs   ← LDAP via Novell.Directory.Ldap
│   │   ├── Services/
│   │   │   ├── DateTimeService.cs
│   │   │   ├── MarkdownService.cs           ← Markdig + Ganss.Xss sanitizer
│   │   │   ├── FileStorageService.cs        ← Network share backend (swap for Azure Blob/S3)
│   │   │   └── TicketSequenceService.cs     ← Atomic per-project ticket number via raw SQL
│   │   └── BackgroundJobs/
│   │       ├── AdSyncBackgroundService.cs
│   │       ├── EmailSenderBackgroundService.cs
│   │       └── AttachmentPurgeBackgroundService.cs
│   │
│   └── ITS.Api/
│       ├── Controllers/                 ← Auth, Tickets, Projects, Comments, Attachments,
│       │                                  Notifications, Admin, Reports, Dashboards
│       ├── Middleware/
│       │   └── GlobalExceptionHandler.cs  ← IExceptionHandler → RFC 7807 ProblemDetails
│       ├── Services/
│       │   └── CurrentUserService.cs    ← ICurrentUserService implementation from JWT claims
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs  ← JWT auth, Swagger, CORS wiring
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── ITS.Domain.Tests/                ← Entity behavior unit tests (no I/O)
    └── ITS.Application.Tests/          ← Handler tests with mocked interfaces
```

---

## 3. CQRS with MediatR

Every user operation is a **Command** (mutating) or **Query** (read-only) routed through MediatR.

```
HTTP Request
    ↓
Controller.Action()
    ↓
mediator.Send(command/query)
    ↓ Pipeline behaviors (in order):
    │  1. LoggingBehavior      — request/response structured log
    │  2. ValidationBehavior   — FluentValidation (throws ValidationException on failure)
    │  3. PerformanceBehavior  — logs warning if > 500ms
    ↓
CommandHandler / QueryHandler
    ↓ (for commands) SaveChangesAsync()
    ↓ DomainEventDispatchInterceptor fires
    ↓ INotification → Domain event handlers (notifications, activity logs)
HTTP Response
```

**Commands** return `void` or a typed response record. They never return EF entities.  
**Queries** return flat DTOs built via projections. They use `AsNoTracking()` on all reads.

---

## 4. Domain Events

Domain events decouple side effects from the core business operation:

```
Ticket.Create()
  → RaiseDomainEvent(TicketCreatedDomainEvent)

SaveChangesAsync()
  → DomainEventDispatchInterceptor.SavedChangesAsync()
    → IPublisher.Publish(TicketCreatedDomainEvent)
      → TicketCreatedEventHandler
          → creates ActivityLog
          → notifies watchers
```

Domain events fire **after** `SaveChanges` succeeds. The entity's state is committed before side effects run.

---

## 5. Authentication Flow

```
Client → POST /api/auth/login { upn, password }
           ↓
         AdAuthenticationService.AuthenticateAsync() — LDAP bind
           ↓ success
         Query AD for user attributes
           ↓
         Provision / update User record in DB
           ↓
         Issue JWT (HS256, 8h expiry by default)
           ↓
         Return { token, userId, roles, ... }

Client → subsequent requests with Authorization: Bearer {token}
           ↓
         JwtBearerMiddleware validates token
           ↓
         CurrentUserService reads claims from HttpContext.User
           ↓
         Handlers receive ICurrentUserService with UserId, Roles, etc.
```

JWT is stateless. Logout is client-side token discard (audit log entry written server-side).  
For short-lived tokens with refresh, add a `RefreshToken` table and `/api/auth/refresh` endpoint.

---

## 6. Authorization Model

Two dimensions of authorization are enforced:

**Global role check** — enforced by `[Authorize(Policy = "SystemAdmin")]` attributes.

**Project-level check** — enforced inside each handler via `IProjectAuthorizationService`:

```csharp
if (!await authz.CanCreateTicketAsync(currentUser.UserId, projectId, ct))
    throw new ForbiddenAccessException(...);
```

`IProjectAuthorizationService` queries `ProjectMembers` and `UserGlobalRoles` to resolve effective permissions. The resolution order is:
1. Project-level role (most specific)
2. Global role (fallback)
3. AD group → role mapping

---

## 7. Workflow Engine

```
Ticket.StatusId → WorkflowTransition (FromStatusId, ToStatusId)
                       ↓
                 TransitionGuards (evaluated in order)
                       │ RoleCheck — query ProjectMembers for required role
                       │ FieldRequired — check ticket field is non-null
                       │ WebhookCall — HTTP POST, expect 2xx
                       ↓ all pass
                 Ticket.TransitionTo(newStatusId)
                       ↓
                 TransitionActions (executed in order)
                       │ SetField — update ticket field
                       │ SendNotification — queue notification
                       │ WebhookCall — fire-and-forget outbound
                       │ CreateComment — auto-comment
                       ↓
                 SaveChanges → TicketStatusChangedDomainEvent dispatched
```

Guard and action configurations are stored as JSON in `TransitionGuards.Configuration` and `TransitionActions.Configuration`. New guard/action types require only a new implementation class registered by type name — no schema migration.

---

## 8. Soft Delete Strategy

All mutable entities carry:

```
IsDeleted BIT NOT NULL DEFAULT 0
DeletedAt DATETIME2(7) NULL
DeletedByUserId UNIQUEIDENTIFIER NULL
```

EF Core **global query filters** are applied per entity type in `OnModelCreating`:

```csharp
builder.HasQueryFilter(t => !t.IsDeleted);
```

This means `db.Tickets.ToList()` automatically excludes soft-deleted rows. To query deleted records (admin/audit only): `db.Tickets.IgnoreQueryFilters().Where(t => t.IsDeleted)`.

---

## 9. Concurrency Control

```
GET /api/tickets/{id}
  Response: ETag: "AAAAAAAAB9E="   ← base64(RowVersion)

PUT /api/tickets/{id}/transitions
  Request header: If-Match: "AAAAAAAAB9E="
  
  Handler: if (!ticket.RowVersion.SequenceEqual(request.RowVersion))
               throw new ConflictException(...)
  
  SaveChanges: EF Core also checks RowVersion → throws DbUpdateConcurrencyException
               which the handler catches and re-throws as ConflictException → HTTP 409
```

Two layers of protection: application-level precheck + database-level enforcement.

---

## 10. Observability

- **Structured logging:** Serilog with context enrichment (UserId, RequestPath, TraceId).
- **Request logging:** `UseSerilogRequestLogging` with request duration and user ID.
- **Slow query detection:** `PerformanceBehavior` logs warning at > 500ms.
- **Correlation ID:** `HttpContext.TraceIdentifier` included in all ProblemDetails responses and audit logs.
- **Health check:** `GET /health` returns `{ status: "healthy", timestamp }` — mount behind load balancer health probe.

For production add: Application Insights / OpenTelemetry exporter, SQL Server query store monitoring.
