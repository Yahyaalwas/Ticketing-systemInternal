# Requirements

## Internal Issue Tracking System (ITS)

**Version:** 1.0  
**Status:** Approved  
**Last Updated:** June 2026

---

## Purpose

Replace Jira as the internal issue tracking platform. Reduce licensing costs, simplify workflow and permission management, and maintain a user experience familiar to Jira users to minimize training overhead.

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 9 |
| Database | SQL Server 2022 |
| ORM | Entity Framework Core 9 |
| Frontend | React 18 + TypeScript |
| Authentication | Active Directory / LDAP |
| API Style | REST + JSON |

---

## Functional Requirements

### FR-01 — Authentication & User Management

- Users authenticate via Active Directory (LDAP). No local passwords stored.
- On first login, the user account is auto-provisioned from AD attributes.
- A background sync job (configurable interval, default 15 min) updates user records from AD.
- Deactivated AD accounts are soft-deleted; their data is preserved and attributed.
- Administrators can trigger an AD sync manually.
- User profile shows AD-sourced attributes plus ITS preferences (timezone, locale, notifications).

### FR-02 — Role-Based Authorization

- **Global roles:** System Administrator, Department Manager, Project Lead, Member, Read-Only Guest.
- **Project-level roles:** Project Admin, Developer, Viewer — override global role within a project.
- AD security groups can be mapped to ITS roles (global or project-scoped).
- Permission changes take effect on the next API request without session restart.

### FR-03 — Projects

- Projects have: unique key (e.g. `OPS`), name, description, lead, department, avatar.
- Projects belong to one primary department; can be cross-listed to others.
- Each project has independently configurable workflow, issue types, priorities, labels, custom fields.
- Projects support archiving (read-only) and soft deletion.
- Project keys are uppercase alphanumeric, immutable after creation.

### FR-04 — Tickets (Issues)

- Ticket identifier: `{ProjectKey}-{SequentialNumber}` (e.g. `OPS-42`).
- Fields: title, rich-text description (Markdown), issue type, status, priority, assignee, reporter, labels, due date, story points, estimated/actual hours, resolution, parent ticket (sub-task), epic link, custom fields, watchers.
- Sub-tasks: one level of parent-child nesting. Sub-tasks inherit their parent's epic.
- Bi-directional ticket links: *blocks / is blocked by*, *relates to*, *duplicates / is duplicated by*, *clones*.
- Optimistic concurrency via HTTP `ETag` / `If-Match` headers backed by SQL Server `ROWVERSION`.
- Soft delete: tickets are flagged `IsDeleted = true`; all data preserved.

### FR-05 — Comments

- Rich-text (Markdown rendered to sanitized HTML) comments on tickets.
- `@mention` of users by UPN triggers notifications.
- Comment edit history is stored (previous body on each edit).
- Comments support one level of reply threading.

### FR-06 — Attachments

- Files can be attached to tickets and individual comments.
- Maximum file size: configurable (default 25 MB).
- Allowed MIME types: configurable by administrators.
- Binary content stored on file storage backend (network share or cloud blob); metadata in DB.
- Deleted attachments are physically purged after a configurable retention period (default 90 days).

### FR-07 — Labels, Priorities, Custom Fields

- Labels: global or project-scoped, color-coded, searchable.
- Priorities: Critical, High, Medium, Low, None — with configurable icons and SLA targets.
- Custom fields per project: Text, Number, Date, SingleSelect, MultiSelect, UserPicker, URL.

### FR-08 — Configurable Workflows

- Each project owns one active workflow (finite state machine of statuses and transitions).
- Transitions support guards: role check, mandatory field, approval, outbound webhook.
- Transitions support post-execution actions: set field, send notification, webhook call, create comment.
- Three status categories: **ToDo**, **InProgress**, **Done** — powers cross-project reporting.
- Built-in workflow templates: Simple, Scrum, DevOps.
- Workflow edits are versioned; active tickets migrate via a configurable status-mapping step.

### FR-09 — Kanban Board

- Columns map to workflow statuses, ordered by `DisplayOrder`.
- Drag-and-drop triggers a workflow transition (with guard validation).
- Board filters: assignee, priority, label, epic.
- WIP limits configurable per column with visual warning.
- Board view presets saveable per user.

### FR-10 — Activity History

- Every field change, status transition, comment, and attachment event is recorded in an immutable per-ticket timeline.
- Timeline entries show actor, timestamp, and field-level diff.

### FR-11 — Notifications

- In-app notification bell for: assignment, @mention, comment added, status changed, due date approaching, SLA breaching.
- Email notifications with per-user digest mode: immediate, hourly, daily, never.
- User-configurable preferences per event type per project.

### FR-12 — Dashboards

- Personal dashboard with configurable widget grid.
- Built-in widgets: My Open Tickets, Recently Viewed, Priority Distribution, Team Velocity, Sprint Burndown, SLA Compliance, Overdue Tickets, Created vs. Resolved.
- Dashboards shareable via read-only token link.

### FR-13 — Reporting

- Built-in reports: Burndown Chart, Velocity Chart, Cumulative Flow Diagram, Created vs. Resolved, Time in Status, SLA Compliance.
- Filters: project, date range, team, assignee, priority, label.
- Export to CSV and PDF.
- Custom report builder: column selection, grouping, sorting, saved presets.

### FR-14 — Search & Filtering

- Full-text search across ticket titles, descriptions, and comments.
- ITS Query Language (IQL): field predicates, boolean operators, ordering — modeled on JQL.
- Saved filters: per-user or globally shared.

### FR-15 — Audit Logs

- All creates, updates, deletes, logins, permission changes, and admin actions are logged.
- Entries include: timestamp (UTC), actor, IP address, user agent, operation, entity, before/after state (JSON).
- Audit log is append-only and queryable by System Administrators.

### FR-16 — Department-Based Organization

- Departments sync from AD organizational units.
- Departments support a hierarchy (parent/child).
- Department Managers have elevated rights within their department's projects.

### FR-17 — Soft Deletes

- All entities (tickets, projects, comments, attachments, users) use soft delete.
- Standard queries exclude soft-deleted records.
- Physical deletion only for attachments (after retention period) via background purge job.

### FR-18 — Optimistic Concurrency

- All mutable entities have a SQL Server `ROWVERSION` column.
- API GET responses include `ETag` header (base64-encoded row version).
- API PUT/PATCH requests require `If-Match` header; mismatch returns `409 Conflict`.

### FR-19 — Scalability Target

- Support 500–2,000 initial users.
- Architecture scales horizontally to 10,000+ users with no code changes.

---

## Non-Functional Requirements

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-01 | Performance | API p95 ≤ 300 ms under 500 concurrent users (CRUD) |
| NFR-02 | Performance | Kanban board initial load ≤ 1.5 s (≤ 500 tickets) |
| NFR-03 | Performance | Full-text search ≤ 2 s (1 million ticket corpus) |
| NFR-04 | Performance | Standard report generation ≤ 5 s (12-month range) |
| NFR-05 | Availability | 99.9% uptime (≤ 8.7 hours/year downtime) |
| NFR-06 | Reliability | Zero-downtime deployments (blue-green or rolling) |
| NFR-07 | Reliability | DB failover RTO ≤ 2 minutes (SQL Server Always On AG) |
| NFR-08 | Reliability | RPO ≤ 15 minutes |
| NFR-09 | Security | TLS 1.2+ for all traffic |
| NFR-10 | Security | Secrets in vault; never in source control or app config files |
| NFR-11 | Security | OWASP Top 10 mitigations applied |
| NFR-12 | Security | SAST and dependency scans in CI pipeline |
| NFR-13 | Scalability | Horizontal scale to 10,000+ users without code changes |
| NFR-14 | Scalability | DB supports 10 million+ tickets without degradation |
| NFR-15 | Maintainability | Clean Architecture with strict layer boundaries |
| NFR-16 | Maintainability | All business logic in Domain + Application layers (testable without HTTP) |
| NFR-17 | Observability | Structured logging (Serilog), correlation IDs, request timing |
| NFR-18 | Compliance | Audit log retained indefinitely; never purged |
