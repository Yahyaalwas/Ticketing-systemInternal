# Release Notes — ITS v1.0.0

**Release Date:** 2026-06-21
**Release Type:** General Availability (GA)

---

## Summary

ITS (Internal Ticketing System) version 1.0.0 is the first general availability release of the enterprise-grade internal issue tracking platform built for corporate environments with Active Directory authentication. ITS provides end-to-end ticket lifecycle management, Kanban-based project boards, team collaboration features, AI-powered productivity tools, and comprehensive administrative controls — all deployable on-premises with zero external dependencies beyond the optional AI provider.

---

## What's New in 1.0.0

### Core Ticket Tracking

- **Full ticket lifecycle management** — create, assign, transition, and resolve tickets with configurable workflow states and transitions
- **Sequential ticket keys** per project (e.g. `ALPHA-42`) for unambiguous cross-team references
- **Issue types** — Bug, Story, Task, Epic with per-project configuration
- **Priority levels** — Critical, High, Medium, Low with visual indicators throughout the UI
- **Story points and time estimates** for capacity planning
- **Due dates** with overdue detection across dashboard, list view, and Kanban board
- **Labels** for cross-cutting tagging and filtering
- **Soft-delete** preserves tickets for audit compliance

### Projects and Workflows

- **Multi-project support** with isolated ticket namespaces and project keys
- **Configurable workflows** — define custom statuses, allowed transitions, and transition guards (require comment, require resolution, role restriction)
- **WIP limits** per Kanban column with visual overflow indicators
- **Project archiving** preserves history while hiding from active views
- **Department association** for organisational reporting

### Kanban Board

- **Drag-and-drop** status transitions with guard enforcement dialogs
- **Column grouping** by workflow status category (Todo / In Progress / Done)
- **Real-time board filters** — by assignee, priority, label, issue type, epic, and text search
- **Card details** — shows priority badge, assignee avatar, due date, story points, and comment count inline

### Collaboration

- **Threaded comments** with Markdown rendering and inline editing
- **Watcher subscriptions** — users can watch any ticket and receive notifications for all changes
- **Attachment support** — up to 25 MB per file; images, PDFs, Office documents, ZIP, and MP4 supported
- **Mention notifications** — `@username` in comments triggers direct notifications
- **Notification bell** with unread badge count

### AI Features

- **Ticket Summariser** — generates executive summary, open blockers, and action items for any ticket
- **AI Comment Generator** — drafts comments from an instruction and tone preference (Professional, Casual, Technical); never auto-posts
- **Natural Language Search** — translates plain-English queries into structured ticket filters
- **Duplicate Detector** — checks for similar existing tickets before you create a new one
- **Similar Tickets** — surfaces semantically related tickets when viewing any ticket
- **Ticket Drafter** — converts free text (emails, notes) into a structured ticket draft
- **Meeting Note Parser** — extracts action items, decisions, risks, and dependencies from meeting notes
- **AI Executive Report** — weekly/monthly/quarterly narrative report with trend analysis
- **Sprint Intelligence** — identifies at-risk tickets, workload imbalances, and SLA warnings
- **Knowledge Assistant** — Q&A over your ticket data using natural language

### Administration and Security

- **Active Directory / LDAP authentication** — users log in with corporate credentials; no separate password management
- **Automatic AD sync** — user profiles sync hourly from LDAP
- **Role-based access control** — System Administrator, Project Lead, Member with fine-grained project-level permissions
- **JWT authentication** — 8-hour token expiry; stateless, no session storage required
- **Optimistic concurrency** — ETag / If-Match headers prevent lost updates on concurrent edits
- **Audit logging** — every ticket mutation, status change, and member action is recorded with actor, timestamp, and before/after values
- **Health check endpoint** at `/health` for monitoring integration
- **Serilog structured logging** with daily rolling files and console output

### Dashboard and Reporting

- **Personal dashboard** showing assigned tickets, watched tickets, overdue items, and project summaries
- **AI Executive Report** for leadership visibility across projects and departments
- **Activity timeline** per ticket showing the complete change history

---

## Technical Highlights

| Layer | Technology |
|-------|-----------|
| Backend framework | ASP.NET Core 9 |
| Architecture | Clean Architecture — Domain / Application / Infrastructure / API |
| ORM | Entity Framework Core 9 with SQL Server provider |
| CQRS / Mediator | MediatR |
| Logging | Serilog (structured, rolling file + console) |
| Authentication | JWT Bearer, LDAP/AD via Novell.Directory.Ldap |
| Background jobs | IHostedService (AD sync, email queue, attachment purge) |
| Database | SQL Server 2019 / 2022 |
| Frontend framework | React 19 with TypeScript |
| UI component library | Material UI (MUI) v9 |
| State management | Zustand (auth/UI state), TanStack Query v5 (server state) |
| Build tool | Vite |
| Containerisation | Docker Compose (multi-stage builds) |
| AI providers | OpenAI, Azure OpenAI, Mock (configurable) |

---

## Known Limitations

The following limitations are known in this release and are targeted for resolution in future versions:

- **No JWT refresh endpoint.** Tokens are valid for 8 hours. After expiry, users must log in again. Silent token refresh is planned for v1.1.
- **No SSO / SAML support.** Authentication is exclusively via LDAP / Active Directory username and password. SAML 2.0 and OIDC integration are on the roadmap.
- **No mobile application.** The web UI is responsive on mobile browsers, but there is no dedicated iOS or Android application.
- **No real-time WebSocket push.** Notifications and board updates require polling or manual refresh. WebSocket support is planned for v1.2.
- **AI features require internet access** when using the OpenAI or Azure OpenAI providers. Air-gapped deployments must use `Ai__Provider=Mock` or self-host a compatible model endpoint.
- **Single-node deployment.** The Docker Compose configuration does not support horizontal scaling of the API. Multi-instance deployment is planned for v2.0.
- **No built-in SMTP authentication UI.** SMTP credentials must be configured via environment variables.

---

## System Requirements

### Production Deployment (Docker Compose)

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Disk | 20 GB | 100 GB |
| Docker Engine | 24.0 | latest |
| OS | Ubuntu 22.04 LTS | Ubuntu 24.04 LTS |

### Client Browsers

| Browser | Minimum Version |
|---------|----------------|
| Chrome | 120 |
| Edge | 120 |
| Firefox | 121 |
| Safari | 17 |

### External Dependencies

| Service | Required | Notes |
|---------|----------|-------|
| SQL Server | Yes | 2019 or 2022; provided via Docker or external instance |
| Active Directory / LDAP | Yes | Any RFC 4511-compliant LDAP server |
| SMTP relay | No | Required only for email notifications |
| OpenAI or Azure OpenAI | No | Required only for AI features |

---

## Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/your-org/Ticketing-systemInternal.git /opt/its
cd /opt/its

# 2. Configure environment
cp .env.example .env
# Edit .env with your values

# 3. Start the stack
docker compose up -d --build

# 4. Apply database migrations
dotnet ef database update \
  --project src/ITS.Infrastructure \
  --startup-project src/ITS.Api

# 5. Verify
curl http://localhost:5000/health
```

See [docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md) for the complete deployment guide.

---

## Documentation

| Document | Location |
|----------|----------|
| Deployment Guide | `docs/DEPLOYMENT_GUIDE.md` |
| Administrator Guide | `docs/ADMIN_GUIDE.md` |
| User Guide | `docs/USER_GUIDE.md` |
| API Documentation | `docs/API_DOCUMENTATION.md` |
| Architecture Overview | `docs/architecture.md` |
| Local Development Setup | `docs/local-development.md` |
| Demo Data Seed Script | `scripts/seed-demo-data.sql` |
