# ITS Administrator Guide

This guide is for system administrators responsible for deploying, configuring, and maintaining the Internal Ticketing System (ITS).

---

## Table of Contents

1. [System Architecture Overview](#system-architecture-overview)
2. [User Management](#user-management)
3. [Role Definitions](#role-definitions)
4. [Project Administration](#project-administration)
5. [Workflow Configuration](#workflow-configuration)
6. [SLA Policies](#sla-policies)
7. [Email Notification Configuration](#email-notification-configuration)
8. [Attachment Storage Management](#attachment-storage-management)
9. [AI Feature Configuration](#ai-feature-configuration)
10. [Audit Log Access](#audit-log-access)
11. [Background Job Monitoring](#background-job-monitoring)
12. [Performance Monitoring](#performance-monitoring)
13. [Security Hardening Checklist](#security-hardening-checklist)

---

## System Architecture Overview

```
                        ┌─────────────────────────────────────┐
                        │           Internet / LAN            │
                        └──────────────┬──────────────────────┘
                                       │ HTTPS :443
                        ┌──────────────▼──────────────────────┐
                        │         nginx (reverse proxy)        │
                        │   TLS termination, static files      │
                        └──────┬───────────────────┬──────────┘
                   /api/*      │                   │  /
          ┌────────────────────▼──┐    ┌───────────▼───────────┐
          │   ITS API (ASP.NET 9) │    │  Frontend (React 19)  │
          │   Port 5000 (HTTP)    │    │  Port 3000 (nginx)    │
          │   JWT authentication  │    │  Vite build, MUI v9   │
          │   MediatR / CQRS      │    └───────────────────────┘
          │   Clean Architecture  │
          └──────────┬────────────┘
                     │
         ┌───────────┴────────────────────────────┐
         │                                        │
┌────────▼───────┐                    ┌───────────▼────────────┐
│  SQL Server    │                    │  Active Directory /     │
│  2022 (Docker) │                    │  LDAP (your DC)        │
│  Port 1433     │                    │  Port 389 / 636        │
└────────────────┘                    └────────────────────────┘
         │
┌────────▼──────────────────────────────────────────────────────┐
│                   Background Jobs (hosted services)            │
│  - AD Sync          sync users from LDAP on a schedule        │
│  - Email Queue      deliver outbound notification emails      │
│  - Attachment Purge remove expired / orphaned files           │
└────────────────────────────────────────────────────────────────┘
         │
┌────────▼───────┐
│  AI Provider   │
│  (optional)    │
│  OpenAI /      │
│  Azure OpenAI  │
└────────────────┘
```

**Data flow summary:**
1. The browser loads the React SPA from the nginx frontend container.
2. API calls use the JWT token in the `Authorization: Bearer` header.
3. The API authenticates users against Active Directory via LDAP.
4. Business logic runs through MediatR command/query handlers.
5. Data is persisted in SQL Server via EF Core.
6. Background jobs run within the API process as `IHostedService` instances.

---

## User Management

### LDAP Synchronisation

ITS synchronises user accounts from Active Directory automatically via the **AD Sync** background job. Synced attributes include:

| AD Attribute | ITS Field |
|-------------|-----------|
| `userPrincipalName` | Username / login identifier |
| `displayName` | Display name |
| `mail` | Email address |
| `employeeID` | Employee ID |
| `thumbnailPhoto` | Avatar image |
| `memberOf` | Group membership (used for role mapping) |

The sync interval is configurable in `appsettings.json` under `AdSync__IntervalMinutes` (default: 60 minutes).

To trigger an immediate sync, restart the API container:
```bash
docker compose restart api
```

### Manual User Operations

User accounts cannot be created manually — all users must exist in Active Directory. If a user cannot log in:

1. Verify the user exists in the configured `ActiveDirectory__SearchBase` OU.
2. Confirm the user's account is enabled in AD.
3. Check that the user's UPN matches the format expected by ITS (`username@domain.com`).
4. Review API logs for LDAP authentication errors.

### Deactivating a User

Disabling a user in Active Directory will prevent them from logging in on their next attempt. Existing sessions remain valid until the JWT token expires (up to 8 hours). There is no forced session revocation endpoint.

To immediately prevent access:
1. Disable the user in Active Directory.
2. Change the `Jwt__Key` to invalidate all existing tokens (this affects all users — use with caution).

### Role Assignment

Roles are assigned at the **system level** and **project level** independently:

- **System roles** are assigned in the database directly or by editing group membership in AD (if AD group→role mapping is configured).
- **Project roles** are assigned through the Project Administration UI or via the API (`POST /api/projects/{projectId}/members`).

---

## Role Definitions

ITS has three built-in roles:

### System Administrator

- Full access to all projects, tickets, and configuration
- Can create and delete projects
- Can assign system-level roles to users
- Can access audit logs
- Can configure workflows and SLA policies
- Can access AI executive reports

### Project Lead

- Full control over a specific project and its tickets
- Can add/remove project members and change their roles within the project
- Can configure project workflows, issue types, and SLA policies
- Can create and archive the project
- Can view project-level AI intelligence reports

### Member

- Can view and create tickets in projects they are a member of
- Can edit tickets assigned to them
- Can add comments and attachments
- Can use AI features (summarize, similar tickets, NL search) on tickets they can access
- Cannot modify project settings or manage members

---

## Project Administration

### Creating a Project

1. Log in as System Administrator or Project Lead.
2. Navigate to **Projects** → **New Project**.
3. Fill in:
   - **Project Key**: 2–6 uppercase letters used as the ticket prefix (e.g. `ALPHA`). Once set, this cannot be changed.
   - **Name**: human-readable project name.
   - **Project Lead**: the user who will own the project.
   - **Department**: associates the project with a department (used in reports).
   - **Workflow**: select a workflow that defines the ticket statuses and transitions for this project.
4. Click **Create Project**.

### Configuring Project Members

In **Project Settings** → **Members**:

- **Add Member**: search by name or UPN, select a role (Project Lead or Member).
- **Change Role**: click the role badge next to a member's name.
- **Remove Member**: click the remove button. Tickets currently assigned to this member retain the assignment.

### Archiving a Project

Archived projects:
- Are hidden from the default project list (use the `isArchived=true` filter to view them).
- Do not accept new tickets.
- Remain fully readable for audit and reporting purposes.

To archive: **Project Settings** → **Archive Project** → Confirm.

---

## Workflow Configuration

A **Workflow** defines the set of ticket statuses and the allowed transitions between them for a project.

### Default Workflow Statuses

| Status | Category | Description |
|--------|----------|-------------|
| Backlog | Todo | Ticket created but not scheduled |
| To Do | Todo | Scheduled for work |
| In Progress | InProgress | Actively being worked on |
| In Review | InProgress | Awaiting code/peer review |
| Done | Done | Work complete |
| Cancelled | Cancelled | Will not be worked on |

### Workflow Transitions

A transition moves a ticket from one status to another. Each transition can optionally require:

- **Required Resolution**: when transitioning to Done or Cancelled, a resolution must be selected (e.g. Fixed, Won't Fix, Duplicate).
- **Required Comment**: the user must provide a comment explaining the transition.
- **Assignee Restriction**: only the currently assigned user may perform the transition.
- **Role Restriction**: only users with a specific project role may perform the transition.

### Editing Workflows

Workflow configuration is seeded into the database and can be modified directly in the `Workflows`, `WorkflowStatuses`, and `WorkflowTransitions` tables. A workflow editor UI is planned for a future release.

---

## SLA Policies

SLA (Service Level Agreement) policies define the time limits for ticket resolution based on priority.

Default SLA targets:

| Priority | Response Time | Resolution Time |
|----------|--------------|----------------|
| Critical | 1 hour | 4 hours |
| High | 4 hours | 1 business day |
| Medium | 1 business day | 3 business days |
| Low | 3 business days | 10 business days |

SLA policies are stored in the `SlaPolicies` database table. Modify them directly or through the API (admin endpoint planned for a future release).

---

## Email Notification Configuration

ITS sends email notifications for:
- Ticket assigned to you
- Ticket status changed
- Comment added to a ticket you are watching
- Ticket due date approaching
- Mention in a comment (`@username`)

### SMTP Configuration

Set the following environment variables (see `.env.example` for details):

```
Email__SmtpHost
Email__SmtpPort
Email__EnableSsl
Email__FromAddress
Email__SmtpUsername   (leave blank for anonymous relay)
Email__SmtpPassword
```

### Testing Email Delivery

Create a test ticket and assign it to yourself. You should receive an assignment notification within a few minutes. Check the API logs for email queue activity:

```bash
docker compose logs api | grep -i "email\|smtp\|notification"
```

### Disabling Email Notifications

To disable email entirely, set `Email__SmtpHost` to an empty string. Notifications will still be stored in-system but will not be delivered via email.

---

## Attachment Storage Management

Attachments are stored on the filesystem inside the `api_attachments` Docker volume, mounted at `/app/attachments` inside the API container.

### Size Limits

| Setting | Default |
|---------|---------|
| Maximum file size | 25 MB (26,214,400 bytes) |
| Allowed content types | Images, PDF, Office docs, text, ZIP, MP4 |

### Retention Policy

The **Attachment Purge** background job removes:
- Attachments associated with soft-deleted tickets older than `Attachments__RetentionDays` days (default: 90).
- Orphaned files (uploaded but never associated with a ticket) older than 24 hours.

### Monitoring Storage Usage

```bash
docker run --rm -v its_api_attachments:/data alpine du -sh /data
```

---

## AI Feature Configuration

### Enabling / Disabling AI

AI features are enabled when `Ai__Provider` is set to `OpenAI` or `AzureOpenAI`. Set `Ai__Provider=Mock` to disable real AI calls (the UI will show demo/canned responses).

### Provider Selection

| Provider | When to Use |
|----------|------------|
| `Mock` | Development and testing — no API calls, instant responses |
| `OpenAI` | Production with a direct OpenAI account |
| `AzureOpenAI` | Production within Azure enterprise environment |

### AI Features Available

| Feature | Endpoint | Description |
|---------|----------|-------------|
| Ticket Summarizer | `POST /api/ai/tickets/{id}/summarize` | Executive summary, blockers, action items |
| Comment Generator | `POST /api/ai/tickets/{id}/generate-comment` | Draft comment (never auto-posted) |
| Meeting Note Parser | `POST /api/ai/parse-meeting-notes` | Extract tasks and decisions from meeting notes |
| Ticket Drafter | `POST /api/ai/draft-ticket` | Create ticket draft from free text |
| NL Search | `POST /api/ai/search` | Natural language ticket search |
| Duplicate Detector | `POST /api/ai/find-duplicates` | Find similar existing tickets |
| Similar Tickets | `GET /api/ai/tickets/{id}/similar` | Semantically similar tickets |
| Executive Report | `GET /api/ai/executive-report` | AI-written periodic report |
| Sprint Intelligence | `GET /api/ai/sprint-intelligence/{projectId}` | At-risk tickets, workload analysis |
| Knowledge Assistant | `POST /api/ai/knowledge-assistant` | Q&A over ticket data |
| Risk Analysis | `GET /api/ai/risk-analysis` | Rule-based risk scoring |

### Data Masking

AI prompts sent to the provider include ticket titles, descriptions, and comments. If your tickets contain sensitive data:

1. Use `Ai__Provider=Mock` to prevent any data from leaving the system.
2. Use Azure OpenAI with a data privacy agreement in place.
3. Review what data is included in prompts via application logs at `Debug` level.

---

## Audit Log Access

ITS records an audit entry for every significant action (ticket created, status changed, member added, etc.) in the `AuditLogs` database table.

### Querying Audit Logs

```sql
-- All actions on a specific ticket in the last 7 days
SELECT *
FROM AuditLogs
WHERE EntityType = 'Ticket'
  AND EntityId = '<ticket-guid>'
  AND CreatedAt >= DATEADD(DAY, -7, GETUTCDATE())
ORDER BY CreatedAt DESC;

-- All actions by a specific user
SELECT *
FROM AuditLogs
WHERE ActorUserId = '<user-guid>'
ORDER BY CreatedAt DESC;
```

The in-app **Ticket Activity** view (`GET /api/tickets/{ticketId}/activity`) also surfaces audit events per ticket.

---

## Background Job Monitoring

ITS runs three background jobs as `IHostedService` instances within the API process:

| Job | Default Schedule | Description |
|-----|-----------------|-------------|
| **AD Sync** | Every 60 minutes | Pulls user data from LDAP and updates the `Users` table |
| **Email Queue** | Every 30 seconds | Dequeues and delivers pending notification emails |
| **Attachment Purge** | Daily at midnight | Removes expired and orphaned attachment files |

### Monitoring Job Execution

```bash
# Filter job-related log entries
docker compose logs api | grep -iE "adSync|emailQueue|attachmentPurge|background"
```

If a job fails repeatedly, the error is logged at `Error` level with a stack trace. Jobs are self-healing — they retry on the next scheduled run.

---

## Performance Monitoring

### Recommended Metrics to Track

| Metric | Collection Method |
|--------|------------------|
| API response times | Serilog request logging (`Elapsed` field in logs) |
| Database query times | EF Core logs at `Information` level (enable in appsettings) |
| Container resource usage | `docker stats` |
| Disk usage (attachments, logs) | `df -h` on host, `docker volume inspect` |
| SQL Server performance | SQL Server DMVs (`sys.dm_exec_query_stats`) |
| Health check status | Poll `GET /health` with an external monitor |

### Slow Query Investigation

```sql
-- Top 10 slowest queries
SELECT TOP 10
    total_elapsed_time / execution_count AS avg_elapsed_us,
    execution_count,
    SUBSTRING(st.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text)
          ELSE qs.statement_end_offset END - qs.statement_start_offset)/2)+1) AS statement_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
ORDER BY avg_elapsed_us DESC;
```

---

## Security Hardening Checklist

- [ ] **Change the default SA password** — use a strong, unique password not reused elsewhere.
- [ ] **Rotate `Jwt__Key`** periodically (e.g. quarterly). Rotating invalidates all active sessions.
- [ ] **Do not expose SQL Server port 1433** to the internet. In production, remove the port mapping or restrict it with firewall rules.
- [ ] **Enable LDAPS (port 636)** instead of plain LDAP to encrypt AD credentials in transit. Set `ActiveDirectory__Port=636`.
- [ ] **Use HTTPS only** — ensure the nginx configuration enforces HTTPS and sets HSTS.
- [ ] **Restrict AI provider API keys** — use project-scoped API keys with spending limits.
- [ ] **Restrict CORS** — `Cors__AllowedOrigins` should list only the exact production frontend URL.
- [ ] **Limit attachment content types** — review and restrict `Attachments__AllowedContentTypes` to the minimum required.
- [ ] **Enable log monitoring** — ship logs to a SIEM or centralized log platform.
- [ ] **Enable SQL Server Auditing** — configure SQL Server audit logs for privileged operations.
- [ ] **Keep images updated** — regularly rebuild Docker images to pull base image security patches.
- [ ] **Backup encryption** — encrypt database backup files at rest before storing offsite.
- [ ] **Principle of least privilege for LDAP service account** — the bind account should have read-only access to user objects only.
- [ ] **Monitor failed login attempts** — filter API logs for `401 Unauthorized` responses on `/api/auth/login`.
