# API Reference

## Internal Issue Tracking System (ITS)

**Base URL:** `https://its.yourcompany.com/api`  
**Version:** v1  
**Auth:** JWT Bearer (obtain via `POST /api/auth/login`)  
**Content-Type:** `application/json`  
**Swagger UI:** `https://its.yourcompany.com/swagger`

---

## Authentication

### POST /api/auth/login

Authenticate with Active Directory credentials. Returns a JWT token.

**Request:**
```json
{
  "userPrincipalName": "john.doe@yourcompany.com",
  "password": "••••••••"
}
```

**Response 200:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "displayName": "John Doe",
  "email": "john.doe@yourcompany.com",
  "avatarUrl": null,
  "roles": ["Member"]
}
```

**Response 401:** Invalid credentials.

---

### GET /api/auth/me

Returns the profile of the currently authenticated user.

**Response 200:**
```json
{
  "userId": "...",
  "upn": "john.doe@yourcompany.com",
  "displayName": "John Doe",
  "email": "john.doe@yourcompany.com",
  "timeZoneId": "GMT Standard Time",
  "locale": "en-GB",
  "roles": ["Member", "Project Lead"]
}
```

### POST /api/auth/logout

Writes an audit log entry. The JWT must be discarded client-side (stateless).

**Response 204**

---

## Projects

### GET /api/projects

Returns projects accessible to the authenticated user.

**Response 200:** Array of `ProjectSummaryDto`
```json
[
  {
    "id": "...",
    "projectKey": "OPS",
    "name": "Operations",
    "description": "IT Operations team tickets",
    "avatarUrl": null,
    "leadUserId": "...",
    "isArchived": false,
    "createdAt": "2026-01-15T09:00:00Z"
  }
]
```

### GET /api/projects/{projectId}

Returns full project details including issue types and member count.

**Response 200:** `ProjectDetailDto`  
**Response 404:** Project not found.  
**Response 403:** Access denied.

---

## Tickets

### GET /api/tickets/{ticketId}

Returns full ticket detail including links, watchers, and content counts.

**Response 200:** `TicketDetailDto`

Key fields:
```json
{
  "id": "...",
  "projectKey": "OPS",
  "ticketNumber": 42,
  "ticketKey": "OPS-42",
  "title": "Login page 500 error on IE11",
  "statusName": "In Progress",
  "statusCategory": "InProgress",
  "assigneeName": "Jane Smith",
  "dueDate": "2026-07-01",
  "storyPoints": 3.0,
  "slaBreachAt": "2026-06-17T09:00:00Z",
  "rowVersion": "AAAAAAAAB9E=",
  "labels": [{ "id": 1, "name": "Bug", "color": "#FF0000" }],
  "links": [],
  "watchers": [],
  "commentCount": 4,
  "attachmentCount": 2
}
```

**Response Headers:** `ETag: "AAAAAAAAB9E="` — use in `If-Match` for updates.

### POST /api/tickets

Create a ticket. Ticket key is returned in the response.

**Request:**
```json
{
  "projectId": "...",
  "title": "Cannot export report to PDF",
  "description": "## Steps to reproduce\n\n1. Go to Reports...",
  "issueTypeId": 2,
  "priorityId": 1,
  "assigneeUserId": null,
  "dueDate": "2026-07-15",
  "storyPoints": 2.0,
  "labelIds": [3, 7]
}
```

**Response 201:**
```json
{
  "ticketId": "...",
  "ticketKey": "OPS-43",
  "ticketNumber": 43
}
```

### POST /api/tickets/{ticketId}/transitions

Move a ticket to a new status via a workflow transition.

**Request Headers:** `If-Match: "AAAAAAAAB9E="`

**Request:**
```json
{
  "toStatusId": 3,
  "comment": "Deployed fix to staging for review.",
  "resolutionId": null
}
```

**Response 204** — transition successful.  
**Response 400** — validation error (missing required comment, etc.).  
**Response 409** — concurrency conflict; refresh the ticket and retry.  
**Response 422** — workflow guard blocked the transition.

### DELETE /api/tickets/{ticketId}

Soft-delete a ticket. Requires `Delete Ticket` permission on the project.

**Response 204**

---

## Comments

### POST /api/tickets/{ticketId}/comments

Add a comment. Supports Markdown. `@upn` triggers notification to the mentioned user.

**Request:**
```json
{
  "parentCommentId": null,
  "body": "Reproduced on Chrome 124. @jane.smith@company.com please review."
}
```

**Response 201:**
```json
{
  "commentId": "...",
  "bodyHtml": "<p>Reproduced on Chrome 124. <a href=\"/users/...\">@jane.smith</a> please review.</p>"
}
```

---

## Attachments

### GET /api/tickets/{ticketId}/attachments

**Response 200:** Array of `AttachmentDto`
```json
[
  {
    "id": "...",
    "fileName": "screenshot.png",
    "contentType": "image/png",
    "fileSizeBytes": 204800,
    "url": "https://its.yourcompany.com/files/2026/06/16/abc123_screenshot.png",
    "thumbnailUrl": null,
    "uploaderUserId": "...",
    "uploadedAt": "2026-06-16T10:30:00Z"
  }
]
```

### POST /api/tickets/{ticketId}/attachments

Upload a file using `multipart/form-data`.

**Form field:** `file` (binary)  
**Max size:** 25 MB (configurable)  
**Response 201:** `AttachmentDto`

### GET /api/tickets/{ticketId}/attachments/{attachmentId}/download

Streams the file with original `Content-Type` and `Content-Disposition: attachment`.

---

## Notifications

### GET /api/notifications

**Query params:** `unreadOnly=true`, `page=1`, `pageSize=25`

**Response 200:**
```json
{
  "items": [
    {
      "id": 1001,
      "type": "CommentAdded",
      "title": "New comment on OPS-42",
      "body": "Reproduced on Chrome 124...",
      "ticketId": "...",
      "isRead": false,
      "createdAt": "2026-06-16T10:35:00Z"
    }
  ],
  "totalCount": 7,
  "unreadCount": 3,
  "page": 1,
  "pageSize": 25
}
```

### POST /api/notifications/{id}/read — Mark one as read. **204**
### POST /api/notifications/read-all — Mark all as read. **204**

---

## Kanban Board

### GET /api/tickets/kanban/{projectId}

**Query params:** `assigneeUserId`, `priorityId`, `labelId`, `epicTicketId`

**Response 200:**
```json
{
  "projectKey": "OPS",
  "projectName": "Operations",
  "columns": [
    {
      "statusId": 1,
      "statusName": "To Do",
      "statusCategory": "ToDo",
      "statusColor": "#DFE1E6",
      "displayOrder": 0,
      "wipLimit": null,
      "tickets": [
        {
          "id": "...",
          "ticketKey": "OPS-42",
          "title": "Login page 500 error",
          "priorityName": "High",
          "assigneeName": "Jane Smith",
          "dueDate": "2026-07-01",
          "storyPoints": 3.0,
          "isSlaBreached": false,
          "labelNames": ["Bug", "Frontend"],
          "commentCount": 4,
          "rowVersion": "AAAAAAAAB9E="
        }
      ]
    }
  ]
}
```

---

## Error Responses

All errors follow **RFC 7807 Problem Details**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Ticket with key 'OPS-999' was not found.",
  "instance": "/api/tickets/...",
  "traceId": "00-4bf92f3577b34da6-00f067aa0ba902b7-01"
}
```

| HTTP Status | Meaning |
|-------------|---------|
| 200 | OK |
| 201 | Created |
| 204 | No Content |
| 400 | Validation failed — body contains `errors` map |
| 401 | Not authenticated |
| 403 | Authenticated but not authorized |
| 404 | Resource not found |
| 409 | Concurrency conflict — refresh and retry |
| 422 | Business rule violation (workflow guard, domain exception) |
| 500 | Internal server error — check server logs |

---

## Pagination

List endpoints accept `page` (1-based, default 1) and `pageSize` (default 25, max 100).

Response envelope:
```json
{
  "items": [...],
  "totalCount": 143,
  "page": 1,
  "pageSize": 25
}
```

---

## Rate Limiting

Not implemented in v1. Recommend adding via `Microsoft.AspNetCore.RateLimiting` middleware at reverse proxy / API gateway layer for production.

---

## Versioning

Current version: `v1`. Future breaking changes will increment to `v2` via URL path versioning (`/api/v2/...`). The `v1` prefix is omitted in v1 for brevity.
