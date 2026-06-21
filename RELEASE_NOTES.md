# ITS — Internal Issue Tracking System
# Release Notes — Version 1.0.0

**Release Date:** 2026-06-21  
**Type:** General Availability (GA)

---

## Summary

ITS 1.0.0 is the first production release of the Internal Issue Tracking System — an enterprise-grade ticket tracking platform built for internal teams. It provides project-scoped ticket management, configurable workflow automation, Kanban boards, AI-assisted features, and deep Active Directory integration, suitable for replacing external tools such as Jira within an on-premises or private-cloud environment.

---

## Key Features

### Core Ticket Management
- Create, view, edit, and delete tickets with rich metadata: title, description (Markdown), issue type, priority, status, assignee, reporter, due date, story points, and estimated hours
- Ticket key system (e.g. `ALPHA-42`) for easy reference
- Optimistic concurrency via `ETag`/`If-Match` headers — prevents silent overwrites in multi-user environments
- File attachments (JPEG, PNG, PDF, Word, Excel, ZIP, MP4 supported; 25 MB limit)
- Labels, watchers, ticket links (blocks/blocked-by, relates-to, duplicates)
- Custom fields per project (text, number, date, list)
- Soft-delete with full audit trail

### Projects & Workflows
- Multi-project with project keys
- Role-based project membership (System Administrator, Project Lead, Member)
- Configurable workflow engine: statuses, allowed transitions, transition guards, required comments on specific transitions
- Per-project workflow templates
- WIP limits per Kanban column
- SLA breach tracking with `SlaBreachAt` timestamps

### Kanban Board
- Drag-and-drop card movement between status columns via `@dnd-kit`
- Real-time WIP limit enforcement (visual warning on exceeded limits)
- Full filter support: assignee, priority, label, epic, issue type, free-text search
- Drag overlay for smooth UX

### Collaboration
- Threaded comments with Markdown rendering
- Comment edit history
- @mention support (parsed and stored)
- Per-user notification preferences
- In-app notification panel (mark individual / mark all as read)
- Email notification queue (background delivery)

### AI Features
- **AI Summary**: Automatic ticket summarization (key facts, reproduction steps, impact)
- **Similar Tickets**: Semantic similarity search to surface related/duplicate tickets
- **Natural Language Search**: Query tickets using plain English
- **Ticket Drafter**: Generate a well-structured ticket from a brief description
- **Meeting Parser**: Extract action items and tickets from meeting notes
- **Executive Report Generator**: Produce stakeholder-ready project summaries
- All AI operations are rate-limited, cached, data-masked, and audited
- Pluggable provider: OpenAI, Azure OpenAI, or built-in Mock (for dev/testing)

### Authentication & Security
- Active Directory (LDAP) authentication — no local password store
- JWT Bearer tokens (configurable expiry, default 8 hours)
- Automatic user provisioning and role sync from AD group memberships
- Role-based access control with three roles: System Administrator, Project Lead, Member
- Project-scoped authorization checks on every operation
- Security response headers: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`, `Strict-Transport-Security`

### Administration & Observability
- Structured logging with Serilog (console + rolling file, configurable minimum level)
- HTTP request logging with user ID enrichment
- Health check endpoint at `GET /health` (JSON, anonymous)
- SQL Server connectivity health check
- Audit log on every state-changing operation
- Background jobs: AD sync, email dispatch, attachment purge
- Global exception handler returning RFC 7807 `ProblemDetails`

---

## Technical Highlights

| Concern | Technology |
|---|---|
| Architecture | Clean Architecture (Domain / Application / Infrastructure / Api) |
| Backend | ASP.NET Core 9, C# 13, MediatR (CQRS), FluentValidation |
| ORM / Database | EF Core 9, SQL Server 2019+ |
| Frontend | React 19, TypeScript 6, Vite 8, MUI v9 |
| State / Fetching | Zustand, TanStack Query v5 |
| Charts | Recharts |
| Drag-and-drop | @dnd-kit |
| Auth | JWT + Active Directory LDAP |
| Logging | Serilog |
| Testing | xUnit, Moq, FluentAssertions, FluentValidation.TestHelper |

---

## Performance Notes

- All list queries use `AsNoTracking()` and project to DTOs — no tracked entity materialisation
- Reference data (users, priorities, statuses) is batch-loaded in parallel (`Task.WhenAll`) to avoid N+1 patterns
- Kanban board and Dashboard handlers use `GroupBy`/`CountAsync` aggregations pushed to SQL
- EF Core connection resiliency: up to 5 retries with 30-second delay
- MUI component tree optimised with `React.memo` on frequently-rendered cards

---

## Known Limitations

| Limitation | Notes |
|---|---|
| No JWT refresh endpoint | Tokens are valid for the configured `ExpiryHours` (default 8 h). Users must log in again when expired. A refresh endpoint is planned for v1.1. |
| No SSO / SAML / OAuth | Authentication is LDAP-only. SAML 2.0 federation is planned for v1.2. |
| No mobile application | The responsive web frontend is accessible on mobile browsers but there is no native iOS/Android app. |
| Empty EF migrations folder | The database schema is applied via `Database.MigrateAsync()` in Development and must be managed via explicit migration scripts in Production. See the Deployment Guide. |
| Integration tests empty | The integration test project scaffold exists but has no test implementations in v1.0. |
| File storage is local disk | Attachments are stored on the API host's filesystem. S3/Azure Blob storage adapters are planned for v1.1. |

---

## System Requirements

**API Server**
- OS: Windows Server 2022 or Ubuntu 22.04+
- Runtime: .NET 9 ASP.NET Core hosting bundle
- RAM: 2 GB minimum, 4 GB recommended
- Disk: 20 GB for application + attachment storage

**Database Server**
- SQL Server 2019 (or 2022) Standard or Enterprise edition
- RAM: 4 GB minimum
- Disk: sized to data volume

**Frontend** (static files served by nginx or IIS)
- Built with Vite; output is pure HTML/CSS/JS
- No server-side rendering required

---

## Upgrade Path

This is the initial release. No upgrade path applies.

---

## Contributors

Built by the Internal Engineering Platform team.

---

*For deployment instructions see [`docs/deployment.md`](docs/deployment.md).*  
*For API reference see [`docs/api.md`](docs/api.md).*
