# ITS API Documentation

**Base URL:** `https://its.yourcompany.com/api`
**Version:** 1.0.0
**Content-Type:** `application/json`

---

## Table of Contents

1. [Authentication](#authentication)
2. [Response Formats](#response-formats)
3. [Pagination](#pagination)
4. [Optimistic Concurrency](#optimistic-concurrency)
5. [Tickets](#tickets)
6. [Projects](#projects)
7. [Kanban Board](#kanban-board)
8. [Dashboard](#dashboard)
9. [Notifications](#notifications)
10. [Activity](#activity)
11. [AI Endpoints](#ai-endpoints)
12. [Reference Data](#reference-data)
13. [Attachments](#attachments)
14. [Rate Limiting](#rate-limiting)
15. [Error Codes Reference](#error-codes-reference)

---

## Authentication

ITS uses JWT Bearer tokens. Tokens are issued at login and expire after 8 hours. There is no refresh endpoint — clients must re-authenticate after expiry.

### POST /auth/login

Authenticate with Active Directory credentials and receive a JWT token.

**Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "userPrincipalName": "john.smith@yourcompany.com",
  "password": "your-password"
}
```

**Response 200 OK:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "displayName": "John Smith",
  "email": "john.smith@yourcompany.com",
  "avatarUrl": "https://its.yourcompany.com/files/avatars/john-smith.jpg",
  "roles": ["Member", "ProjectLead"]
}
```

**Response 401 Unauthorized:**
```json
{ "message": "Invalid credentials." }
```

---

### GET /auth/me

Return the current authenticated user's profile.

**Headers required:** `Authorization: Bearer <token>`

**Response 200 OK:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "upn": "john.smith@yourcompany.com",
  "displayName": "John Smith",
  "email": "john.smith@yourcompany.com",
  "employeeId": "EMP-12345",
  "avatarUrl": "https://its.yourcompany.com/files/avatars/john-smith.jpg",
  "timeZoneId": "UTC",
  "locale": "en-US",
  "roles": ["Member", "ProjectLead"]
}
```

---

### POST /auth/logout

Invalidate the current session and write an audit log entry. The JWT token itself is not revoked server-side (stateless), but the client should discard it.

**Headers required:** `Authorization: Bearer <token>`

**Response 204 No Content**

---

## Response Formats

### Success Responses

All successful responses return JSON. The shape depends on the endpoint:

- **Single resource:** returns the resource object directly.
- **Collection:** returns a paginated envelope (see [Pagination](#pagination)).
- **Create:** returns `201 Created` with the `Location` header and the created resource.
- **Update / Delete / Action:** returns `204 No Content`.

### Error Format (RFC 7807 ProblemDetails)

All error responses use the RFC 7807 `ProblemDetails` format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-abc123-def456-00",
  "errors": {
    "Title": ["The Title field is required."],
    "PriorityId": ["Priority must be between 1 and 4."]
  }
}
```

| Field | Description |
|-------|-------------|
| `type` | URI identifying the error type |
| `title` | Human-readable error summary |
| `status` | HTTP status code |
| `traceId` | Correlation ID for log lookup |
| `errors` | Validation errors keyed by field name (400 only) |
| `detail` | Additional context (business rule violations, etc.) |

---

## Pagination

List endpoints accept `page` and `pageSize` query parameters and return a standard paginated envelope:

**Request:**
```http
GET /api/tickets?page=2&pageSize=10
```

**Response:**
```json
{
  "items": [ ... ],
  "totalCount": 87,
  "page": 2,
  "pageSize": 10,
  "totalPages": 9
}
```

| Parameter | Default | Maximum |
|-----------|---------|---------|
| `page` | `1` | — |
| `pageSize` | `25` | `100` |

---

## Optimistic Concurrency

Mutation endpoints for tickets and projects use ETag-based optimistic concurrency to prevent lost updates.

**Workflow:**
1. Fetch the resource (`GET`). The response includes an `ETag` header:
   ```
   ETag: "AAAAAAAB2js="
   ```
2. Submit the mutation (`PUT`, `PATCH`, or `DELETE`) with an `If-Match` header containing the ETag value:
   ```
   If-Match: "AAAAAAAB2js="
   ```
3. If the resource was modified by another user since your GET, the server returns **409 Conflict**. Reload the resource and retry.

If you omit the `If-Match` header on an endpoint that requires it, the server returns **400 Bad Request**.

---

## Tickets

All ticket endpoints require `Authorization: Bearer <token>`.

### GET /tickets

List tickets with optional filters and pagination.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `projectId` | GUID | Filter by project |
| `assigneeUserId` | GUID | Filter by assignee |
| `priorityId` | int | Filter by priority (1–4) |
| `issueTypeId` | int | Filter by issue type |
| `statusId` | int | Filter by workflow status |
| `search` | string | Full-text search on title and description |
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Results per page (default: 25, max: 100) |

**Response 200 OK:** Paginated list of ticket summaries.

---

### GET /tickets/{ticketId}

Get full ticket detail including comments count, watchers, and related links.

**Path Parameters:** `ticketId` (GUID)

**Response 200 OK:**
```json
{
  "ticketId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ticketKey": "ALPHA-42",
  "projectId": "...",
  "projectName": "Alpha Project",
  "title": "Login fails with special characters in password",
  "description": "Steps to reproduce...",
  "issueType": { "id": 1, "name": "Bug", "iconUrl": "..." },
  "priority": { "id": 2, "name": "High", "color": "#FF6B00" },
  "status": { "id": 3, "name": "In Progress", "category": "InProgress" },
  "assignee": { "userId": "...", "displayName": "Jane Doe", "avatarUrl": "..." },
  "reporter": { "userId": "...", "displayName": "John Smith", "avatarUrl": "..." },
  "dueDate": "2026-07-01",
  "storyPoints": 3,
  "estimatedHours": 4,
  "commentCount": 5,
  "attachmentCount": 2,
  "labels": [{ "id": 1, "name": "security", "color": "#D32F2F" }],
  "watchers": [ ... ],
  "availableTransitions": [
    { "toStatusId": 4, "toStatusName": "In Review", "requiresComment": false }
  ],
  "createdAt": "2026-06-01T09:00:00Z",
  "updatedAt": "2026-06-15T14:30:00Z",
  "rowVersion": "AAAAAAAB2js="
}
```

**Response 404 Not Found:** Ticket does not exist or has been deleted.
**Response 403 Forbidden:** Current user is not a member of the ticket's project.

---

### POST /tickets

Create a new ticket.

**Request Body:**
```json
{
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Login fails with special characters in password",
  "description": "## Steps to reproduce\n1. Navigate to /login\n2. ...",
  "issueTypeId": 1,
  "priorityId": 2,
  "assigneeUserId": "...",
  "dueDate": "2026-07-01",
  "storyPoints": 3,
  "estimatedHours": 4,
  "labelIds": [1, 3]
}
```

**Response 201 Created:**
```json
{
  "ticketId": "...",
  "ticketKey": "ALPHA-42"
}
```

---

### PUT /tickets/{ticketId}

Update a ticket. Requires `If-Match` header.

**Request Body:**
```json
{
  "title": "Updated title",
  "description": "Updated description",
  "issueTypeId": 1,
  "priorityId": 3,
  "assigneeUserId": null,
  "dueDate": null,
  "storyPoints": 5,
  "estimatedHours": 8
}
```

**Response 204 No Content**
**Response 409 Conflict:** ETag mismatch (concurrent edit detected).

---

### DELETE /tickets/{ticketId}

Soft-delete a ticket (Project Lead or System Administrator only).

**Response 204 No Content**

---

### POST /tickets/{ticketId}/transitions

Transition a ticket to a new status. Requires `If-Match` header.

**Request Body:**
```json
{
  "toStatusId": 5,
  "comment": "Deploying to staging for verification.",
  "resolutionId": null
}
```

**Response 204 No Content**
**Response 422 Unprocessable Entity:** Transition not allowed by the workflow (e.g. guard condition failed).

---

### PATCH /tickets/{ticketId}/assign

Assign or unassign a ticket. Pass `null` for `assigneeUserId` to unassign.

**Request Body:**
```json
{ "assigneeUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

**Response 204 No Content**

---

### PATCH /tickets/{ticketId}/priority

Change ticket priority.

**Request Body:**
```json
{ "priorityId": 1 }
```

**Response 204 No Content**

---

### PATCH /tickets/{ticketId}/due-date

Change the due date. Pass `null` to clear.

**Request Body:**
```json
{ "dueDate": "2026-08-01" }
```

**Response 204 No Content**

---

### POST /tickets/{ticketId}/labels/{labelId}

Add a label to a ticket.

**Response 204 No Content**

---

### DELETE /tickets/{ticketId}/labels/{labelId}

Remove a label from a ticket.

**Response 204 No Content**

---

### POST /tickets/{ticketId}/watchers/{userId}

Add a watcher to a ticket.

**Response 204 No Content**

---

### DELETE /tickets/{ticketId}/watchers/{userId}

Remove a watcher from a ticket.

**Response 204 No Content**

---

### GET /tickets/{ticketId}/comments

List comments for a ticket.

**Query Parameters:** `page`, `pageSize`

**Response 200 OK:** Paginated list of comment objects.

---

### POST /tickets/{ticketId}/comments

Add a comment.

**Request Body:**
```json
{
  "parentCommentId": null,
  "body": "I reproduced this on Edge 124. Assigning to backend team."
}
```

**Response 201 Created:**
```json
{
  "commentId": "...",
  "createdAt": "2026-06-21T10:00:00Z"
}
```

---

### PUT /tickets/{ticketId}/comments/{commentId}

Edit a comment (author or admin only).

**Request Body:**
```json
{ "body": "Updated comment text." }
```

**Response 204 No Content**

---

### DELETE /tickets/{ticketId}/comments/{commentId}

Delete a comment (author or admin only).

**Response 204 No Content**

---

## Projects

### GET /projects

List projects. Supports filters: `search`, `departmentId`, `isArchived`.

**Response 200 OK:** Paginated list of project summaries.

---

### GET /projects/{projectId}

Get project detail. Response includes `ETag` header for optimistic concurrency.

**Response 200 OK:** Full project object including members and configuration.

---

### POST /projects

Create a project (Project Lead or System Administrator only).

**Request Body:**
```json
{
  "projectKey": "ALPHA",
  "name": "Alpha Project",
  "description": "Main software delivery project",
  "leadUserId": "...",
  "departmentId": 1,
  "workflowId": null
}
```

**Response 201 Created**

---

### PUT /projects/{projectId}

Update a project. Requires `If-Match` header.

**Response 204 No Content**

---

### DELETE /projects/{projectId}

Soft-delete a project (System Administrator only).

**Response 204 No Content**

---

### POST /projects/{projectId}/archive

Archive a project (Project Lead or System Administrator only).

**Response 204 No Content**

---

### POST /projects/{projectId}/members

Add a member to a project.

**Request Body:**
```json
{ "userId": "...", "roleId": 3 }
```

**Response 204 No Content**

---

### DELETE /projects/{projectId}/members/{userId}

Remove a member from a project.

**Response 204 No Content**

---

### PATCH /projects/{projectId}/members/{userId}/role

Change a member's role in a project.

**Request Body:**
```json
{ "roleId": 2 }
```

**Response 204 No Content**

---

### PUT /projects/{projectId}/lead

Assign a new project lead.

**Request Body:**
```json
{ "newLeadUserId": "..." }
```

**Response 204 No Content**

---

## Kanban Board

### GET /kanban/{projectId}

Get the Kanban board for a project, with all tickets organised into columns by status.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `assigneeUserId` | GUID | Filter cards by assignee |
| `priorityId` | int | Filter cards by priority |
| `labelId` | int | Filter cards by label |
| `epicTicketId` | GUID | Filter cards by epic |
| `issueTypeId` | int | Filter cards by issue type |
| `search` | string | Text search on card titles |

**Response 200 OK:**
```json
{
  "projectId": "...",
  "projectName": "Alpha Project",
  "columns": [
    {
      "statusId": 1,
      "statusName": "Backlog",
      "category": "Todo",
      "wipLimit": null,
      "cards": [
        {
          "ticketId": "...",
          "ticketKey": "ALPHA-1",
          "title": "...",
          "priority": { "id": 2, "name": "High", "color": "#FF6B00" },
          "issueType": { "id": 1, "name": "Bug" },
          "assignee": { "userId": "...", "displayName": "Jane", "avatarUrl": "..." },
          "dueDate": "2026-07-01",
          "storyPoints": 3,
          "commentCount": 2,
          "labels": [],
          "rowVersion": "AAAAAAAB2js="
        }
      ]
    }
  ]
}
```

---

## Dashboard

### GET /dashboard

Get the dashboard summary for the currently authenticated user.

**Response 200 OK:**
```json
{
  "myOpenTickets": [ ... ],
  "watchingTickets": [ ... ],
  "overdueTickets": [ ... ],
  "recentActivity": [ ... ],
  "projectSummaries": [
    {
      "projectId": "...",
      "projectName": "Alpha",
      "openTicketCount": 14,
      "overdueTicketCount": 2
    }
  ]
}
```

---

## Notifications

### GET /notifications

Get notifications for the current user.

**Query Parameters:** `unreadOnly` (bool), `page`, `pageSize`

**Response 200 OK:**
```json
{
  "items": [
    {
      "id": 1001,
      "type": "TicketAssigned",
      "title": "ALPHA-42 assigned to you",
      "body": "John Smith assigned ticket ALPHA-42 to you.",
      "ticketId": "...",
      "projectId": "...",
      "sourceUserId": "...",
      "isRead": false,
      "readAt": null,
      "createdAt": "2026-06-21T09:00:00Z"
    }
  ],
  "totalCount": 12,
  "unreadCount": 5,
  "page": 1,
  "pageSize": 25
}
```

---

### POST /notifications/{notificationId}/read

Mark a single notification as read.

**Response 204 No Content**

---

### POST /notifications/read-all

Mark all notifications for the current user as read.

**Response 204 No Content**

---

## Activity

### GET /tickets/{ticketId}/activity

Get the chronological activity timeline for a ticket (field changes, transitions, comments, attachments).

**Query Parameters:** `page`, `pageSize` (default: 50)

**Response 200 OK:** Paginated list of activity entries.

```json
{
  "items": [
    {
      "activityId": "...",
      "actorUserId": "...",
      "actorDisplayName": "Jane Doe",
      "actorAvatarUrl": "...",
      "activityType": "StatusChanged",
      "description": "Changed status from \"To Do\" to \"In Progress\"",
      "oldValue": "To Do",
      "newValue": "In Progress",
      "createdAt": "2026-06-21T10:00:00Z"
    }
  ],
  "totalCount": 18,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

---

## AI Endpoints

All AI endpoints require `Authorization: Bearer <token>`. They may return `503 Service Unavailable` if the configured AI provider is unreachable or rate-limited.

### POST /ai/tickets/{ticketId}/summarize

Generate an AI summary of a ticket.

**Query Parameters:** `forceRefresh` (bool, default: false) — bypass the cache.

**Response 200 OK:** `TicketAiSummaryDto`

---

### POST /ai/tickets/{ticketId}/generate-comment

Generate a draft comment. **Never posts automatically.**

**Request Body:**
```json
{
  "instruction": "Write a status update for the stakeholders.",
  "tone": "Professional"
}
```

Tone values: `Professional`, `Casual`, `Technical`

**Response 200 OK:** `GeneratedCommentDto` with `draftText` field.

---

### POST /ai/parse-meeting-notes

Parse meeting notes into structured tasks and decisions.

**Request Body:**
```json
{
  "rawNotes": "Discussed the login bug. Alice to fix by Friday...",
  "meetingTitle": "Sprint Review 2026-06-20",
  "meetingDate": "2026-06-20"
}
```

**Response 200 OK:** `MeetingParseResultDto` with arrays of `actionItems`, `decisions`, `risks`, `dependencies`.

---

### POST /ai/draft-ticket

Draft a ticket from free text.

**Request Body:**
```json
{
  "rawText": "The export button on the reports page crashes Safari 17.",
  "projectId": "..."
}
```

**Response 200 OK:** `TicketDraftDto` with suggested `title`, `description`, `issueTypeId`, `priorityId`.

---

### POST /ai/search

Natural language ticket search.

**Request Body:**
```json
{
  "query": "Critical bugs assigned to Bob that are overdue",
  "projectId": "..."
}
```

**Response 200 OK:** `NaturalLanguageSearchResult` with `tickets` array and `interpretedFilters` explanation.

---

### POST /ai/find-duplicates

Detect potential duplicate tickets before creation.

**Request Body:**
```json
{
  "title": "Login page crash",
  "description": "App crashes when logging in with SSO",
  "projectId": "..."
}
```

**Response 200 OK:** `DuplicateDetectionResult` with `potentialDuplicates` array (each includes a `similarityScore`).

---

### GET /ai/tickets/{ticketId}/similar

Get semantically similar tickets.

**Response 200 OK:** `SimilarTicketsDto` with `similarTickets` array.

---

### GET /ai/executive-report

Generate an AI executive report.

**Query Parameters:**
- `period`: `Weekly` | `Monthly` | `Quarterly` (default: `Weekly`)
- `projectId` (optional GUID)
- `departmentId` (optional GUID)
- `forceRefresh` (bool)

**Response 200 OK:** `ExecutiveReportDto`

---

### GET /ai/sprint-intelligence/{projectId}

Sprint-level AI intelligence for a project.

**Response 200 OK:** `SprintIntelligenceDto` with at-risk tickets, workload analysis, SLA warnings.

---

### POST /ai/knowledge-assistant

Answer natural language questions using ticket data as context.

**Request Body:**
```json
{
  "question": "What are the most common causes of production incidents this quarter?",
  "projectId": "..."
}
```

**Response 200 OK:** `KnowledgeAnswerDto` with `answer` and `sourceTickets` citations.

---

### GET /ai/risk-analysis

Rule-based risk analysis.

**Query Parameters:** `projectId` (GUID, optional), `ticketId` (GUID, optional)

**Response 200 OK:** `RiskAnalysisDto`

---

## Reference Data

### GET /reference/priorities

Get all ticket priorities.

**Response 200 OK:**
```json
[
  { "id": 1, "name": "Critical", "color": "#D32F2F", "sortOrder": 1 },
  { "id": 2, "name": "High",     "color": "#FF6B00", "sortOrder": 2 },
  { "id": 3, "name": "Medium",   "color": "#FFC107", "sortOrder": 3 },
  { "id": 4, "name": "Low",      "color": "#388E3C", "sortOrder": 4 }
]
```

---

### GET /reference/issue-types

Get issue types. Optionally filter by `projectId` to get project-specific types.

**Query Parameters:** `projectId` (string, optional)

**Response 200 OK:** Array of `IssueTypeDto` objects.

---

### GET /reference/users

Search users (for assignee/watcher pickers).

**Query Parameters:** `search` (string), `limit` (int, default: 20)

**Response 200 OK:** Array of `UserRefDto` objects with `userId`, `displayName`, `email`, `avatarUrl`.

---

### GET /reference/roles

Get available roles.

**Query Parameters:** `scope` (string: `system` | `project`)

**Response 200 OK:** Array of `RoleRefDto` objects.

---

## Attachments

### POST /attachments/upload

Upload an attachment for a ticket. Use `multipart/form-data`.

**Form Fields:**
- `file` (binary) — the file to upload
- `ticketId` (GUID) — the ticket to attach to

**Response 201 Created:**
```json
{
  "attachmentId": "...",
  "fileName": "screenshot.png",
  "contentType": "image/png",
  "sizeBytes": 245760,
  "downloadUrl": "https://its.yourcompany.com/files/..."
}
```

**Response 400 Bad Request:** File too large (max 25 MB) or unsupported content type.

---

### DELETE /attachments/{attachmentId}

Delete an attachment (uploader, Project Lead, or System Administrator only).

**Response 204 No Content**

---

## Rate Limiting

ITS does not impose application-level rate limiting in the current release. Rate limiting is expected to be handled at the reverse proxy (nginx) or network layer.

AI endpoints (`/api/ai/*`) are subject to rate limits imposed by the upstream AI provider (OpenAI or Azure OpenAI). When the provider rate limit is hit, the API returns **503 Service Unavailable** with a `Retry-After` header.

---

## Error Codes Reference

| HTTP Status | Meaning | Common Cause |
|-------------|---------|-------------|
| `400 Bad Request` | Validation error | Missing required field, invalid format, If-Match header absent |
| `401 Unauthorized` | Not authenticated | Missing or expired JWT token |
| `403 Forbidden` | Not authorised | User lacks the required role or project membership |
| `404 Not Found` | Resource missing | Ticket/project deleted or never existed |
| `409 Conflict` | Concurrency conflict | ETag mismatch — resource modified since last GET |
| `422 Unprocessable Entity` | Business rule violation | Workflow transition not allowed, SLA guard failed |
| `429 Too Many Requests` | Rate limited | Upstream AI provider rate limit hit |
| `500 Internal Server Error` | Unhandled exception | Check API logs with the `traceId` from the response |
| `503 Service Unavailable` | Dependency unavailable | AI provider down or database connection lost |
