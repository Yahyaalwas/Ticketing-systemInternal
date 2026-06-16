# Database Design

## Internal Issue Tracking System (ITS)

**Engine:** SQL Server 2022  
**ORM:** Entity Framework Core 9  
**Version:** 1.0

---

## Schema Organization

SQL Server schemas are used as logical namespaces for modular boundary enforcement and schema-level permission grants.

| Schema | Contains |
|--------|----------|
| `identity` | Users, Departments, Roles, AD group mappings |
| `projects` | Projects, members, issue types, priorities, labels, custom fields |
| `workflow` | Workflows, statuses, transitions, guards, actions |
| `tickets` | Tickets, labels, links, watchers, custom field values |
| `content` | Comments, comment history, mentions, attachments |
| `audit` | Activity logs (ticket timeline), system audit trail |
| `notifications` | In-app notifications, email queue, user preferences |
| `config` | System settings, per-project ticket number sequences |

---

## Standard Conventions

### Primary Keys

| Tier | Type | Used For |
|------|------|----------|
| A | `UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID()` | Core business entities (User, Project, Ticket, Comment) |
| B | `INT IDENTITY(1,1)` | Lookup / reference tables (Role, Status, Priority, Label) |
| C | `BIGINT IDENTITY(1,1)` | High-volume append tables (AuditLog, ActivityLog, Notification) |

`NEWSEQUENTIALID()` avoids clustered index fragmentation of random `NEWID()` while keeping GUID benefits (enumeration prevention, future merge safety).

### Standard Audit Columns (all mutable entities)

```sql
CreatedAt         DATETIME2(7)     NOT NULL  DEFAULT SYSUTCDATETIME()
CreatedByUserId   UNIQUEIDENTIFIER NOT NULL
UpdatedAt         DATETIME2(7)     NOT NULL  DEFAULT SYSUTCDATETIME()
UpdatedByUserId   UNIQUEIDENTIFIER NOT NULL
IsDeleted         BIT              NOT NULL  DEFAULT 0
DeletedAt         DATETIME2(7)     NULL
DeletedByUserId   UNIQUEIDENTIFIER NULL
RowVersion        ROWVERSION       NOT NULL
```

### Concurrency Token

`ROWVERSION` (8-byte auto-incrementing binary counter, updated on every `UPDATE`) is the concurrency token.  
EF Core maps this with `.IsRowVersion().IsConcurrencyToken()`.

---

## Tables by Schema

### identity.Departments

```
DepartmentId            INT IDENTITY PK
AdOuDistinguishedName   NVARCHAR(512) NULL
Name                    NVARCHAR(256) NOT NULL
Description             NVARCHAR(1024) NULL
ParentDepartmentId      INT NULL → Departments (self-ref)
HeadUserId              UNIQUEIDENTIFIER NULL → Users
[standard audit columns]

UX_Departments_Name: UNIQUE (Name) WHERE IsDeleted = 0
```

### identity.Users

```
UserId                  UNIQUEIDENTIFIER PK NEWSEQUENTIALID()
AdObjectId              NVARCHAR(256) NOT NULL UNIQUE        ← stable AD anchor
UserPrincipalName       NVARCHAR(256) NOT NULL               ← login key (mutable)
Email                   NVARCHAR(256) NOT NULL
DisplayName             NVARCHAR(256) NOT NULL
FirstName               NVARCHAR(128) NULL
LastName                NVARCHAR(128) NULL
DepartmentId            INT NULL → Departments
ManagerUserId           UNIQUEIDENTIFIER NULL → Users (self-ref)
JobTitle                NVARCHAR(256) NULL
PhoneNumber             NVARCHAR(50) NULL
AvatarUrl               NVARCHAR(512) NULL
IsActive                BIT NOT NULL DEFAULT 1
LastLoginAt             DATETIME2(7) NULL
LastAdSyncAt            DATETIME2(7) NULL
TimeZoneId              NVARCHAR(128) NOT NULL DEFAULT 'UTC'
Locale                  NVARCHAR(10) NOT NULL DEFAULT 'en-US'
[standard audit columns]

UX_Users_AdObjectId: UNIQUE (AdObjectId)
UX_Users_UserPrincipalName: UNIQUE (UserPrincipalName) WHERE IsDeleted = 0
UX_Users_Email: UNIQUE (Email) WHERE IsDeleted = 0
IX_Users_IsActive_IsDeleted: (IsActive, IsDeleted)
IX_Users_DisplayName: (DisplayName)       ← user picker type-ahead
```

### identity.Roles

```
RoleId          INT IDENTITY PK
Name            NVARCHAR(100) NOT NULL UNIQUE
Description     NVARCHAR(512) NULL
Scope           NVARCHAR(10) NOT NULL  CHECK IN ('Global','Project')
IsSystemRole    BIT NOT NULL DEFAULT 0
```

### identity.UserGlobalRoles

```
UserGlobalRoleId    INT IDENTITY PK
UserId              UNIQUEIDENTIFIER NOT NULL → Users
RoleId              INT NOT NULL → Roles
GrantedAt           DATETIME2(7) NOT NULL
GrantedByUserId     UNIQUEIDENTIFIER NOT NULL → Users

UX_UserGlobalRoles_UserRole: UNIQUE (UserId, RoleId)
```

### identity.AdGroupRoleMappings

```
MappingId                   INT IDENTITY PK
AdGroupDistinguishedName    NVARCHAR(512) NOT NULL
AdGroupName                 NVARCHAR(256) NOT NULL
RoleId                      INT NOT NULL → Roles
ProjectId                   UNIQUEIDENTIFIER NULL → Projects

UX: UNIQUE (AdGroupDistinguishedName, RoleId, ProjectId)
```

---

### projects.Projects

```
ProjectId           UNIQUEIDENTIFIER PK NEWSEQUENTIALID()
ProjectKey          NVARCHAR(10) NOT NULL  ← uppercase alphanumeric
Name                NVARCHAR(256) NOT NULL
Description         NVARCHAR(2000) NULL
AvatarUrl           NVARCHAR(512) NULL
LeadUserId          UNIQUEIDENTIFIER NOT NULL → Users
DepartmentId        INT NOT NULL → Departments
ActiveWorkflowId    UNIQUEIDENTIFIER NULL → Workflows
IsArchived          BIT NOT NULL DEFAULT 0
ArchivedAt          DATETIME2(7) NULL
ArchivedByUserId    UNIQUEIDENTIFIER NULL → Users
[standard audit columns]

UX_Projects_ProjectKey: UNIQUE (ProjectKey) WHERE IsDeleted = 0
CK: ProjectKey = UPPER(ProjectKey) AND no special chars
```

### projects.ProjectMembers

```
ProjectMemberId     UNIQUEIDENTIFIER PK
ProjectId           UNIQUEIDENTIFIER NOT NULL → Projects
UserId              UNIQUEIDENTIFIER NOT NULL → Users
RoleId              INT NOT NULL → Roles (Project-scoped only)
GrantedAt           DATETIME2(7) NOT NULL
GrantedByUserId     UNIQUEIDENTIFIER NOT NULL → Users

UX: UNIQUE (ProjectId, UserId)
IX_ProjectMembers_UserId: (UserId)    ← "which projects am I on?"
```

### projects.IssueTypes, Priorities, Labels, CustomFieldDefinitions, CustomFieldOptions

See full DDL in `src/ITS.Infrastructure/Persistence/Configurations/`.

---

### workflow.WorkflowStatuses

```
StatusId        INT IDENTITY PK
WorkflowId      UNIQUEIDENTIFIER NOT NULL → Workflows
Name            NVARCHAR(100) NOT NULL
Category        NVARCHAR(10) NOT NULL  CHECK IN ('ToDo','InProgress','Done')
Color           NVARCHAR(7) NULL
DisplayOrder    INT NOT NULL DEFAULT 0
IsInitial       BIT NOT NULL DEFAULT 0
IsFinal         BIT NOT NULL DEFAULT 0
```

The `Category` column is the key to cross-project reporting without knowing each project's status names.

### workflow.WorkflowTransitions

```
TransitionId        INT IDENTITY PK
WorkflowId          UNIQUEIDENTIFIER NOT NULL → Workflows
Name                NVARCHAR(100) NOT NULL
FromStatusId        INT NOT NULL → WorkflowStatuses
ToStatusId          INT NOT NULL → WorkflowStatuses
RequiresComment     BIT NOT NULL DEFAULT 0

CK: FromStatusId <> ToStatusId
UX: UNIQUE (WorkflowId, FromStatusId, ToStatusId)
```

### workflow.TransitionGuards / TransitionActions

Both store `Configuration NVARCHAR(MAX) NOT NULL` as JSON. New guard/action types add behavior without schema changes.

---

### tickets.Tickets

The central entity. Key columns:

```
TicketId            UNIQUEIDENTIFIER PK
ProjectId           UNIQUEIDENTIFIER NOT NULL → Projects
TicketNumber        INT NOT NULL              ← sequential per project
Title               NVARCHAR(500) NOT NULL
Description         NVARCHAR(MAX) NULL        ← Markdown source
DescriptionHtml     NVARCHAR(MAX) NULL        ← pre-rendered HTML cache
IssueTypeId         INT NOT NULL → IssueTypes
StatusId            INT NOT NULL → WorkflowStatuses
PriorityId          INT NULL → Priorities
AssigneeUserId      UNIQUEIDENTIFIER NULL → Users
ReporterUserId      UNIQUEIDENTIFIER NOT NULL → Users
ParentTicketId      UNIQUEIDENTIFIER NULL → Tickets (self-ref, sub-task)
EpicTicketId        UNIQUEIDENTIFIER NULL → Tickets (self-ref, epic)
DueDate             DATE NULL
StoryPoints         DECIMAL(5,1) NULL
SlaBreachAt         DATETIME2(7) NULL         ← CreatedAt + Priority.SlaTargetHours
RowVersion          ROWVERSION NOT NULL
[standard audit columns]

CK: ParentTicketId <> TicketId
CK: NOT (ParentTicketId IS NOT NULL AND EpicTicketId IS NOT NULL)
UX: UNIQUE (ProjectId, TicketNumber)
IX_Tickets_ProjectId_Status: (ProjectId, StatusId) INCLUDE (Title, AssigneeUserId, PriorityId)
IX_Tickets_AssigneeUserId: (AssigneeUserId) WHERE IsDeleted = 0
IX_Tickets_SlaBreachAt: (SlaBreachAt) WHERE IsDeleted = 0 AND SlaBreachAt IS NOT NULL
```

### config.ProjectSequences

```
ProjectId       UNIQUEIDENTIFIER PK → Projects
CurrentNumber   INT NOT NULL DEFAULT 0
UpdatedAt       DATETIME2(7) NOT NULL
```

Ticket number generation uses an atomic `UPDATE ... OUTPUT` against this table with `ROWLOCK` hint, avoiding sequence gaps under concurrent inserts.

---

## Indexing Strategy

1. **All foreign key columns** have supporting indexes (SQL Server doesn't auto-create these).
2. **Filtered indexes** on `IsDeleted = 0` reduce index size and match query predicates exactly.
3. **Covering indexes** (`INCLUDE`) on hot paths eliminate key lookups (Kanban board, My Tickets).
4. **Descending order** on time-series indexes (`OccurredAt DESC`) matches query sort direction.
5. **Partial indexes** on nullable columns (`WHERE column IS NOT NULL`) exclude majority-null rows.

---

## Full-Text Search

```sql
CREATE FULLTEXT CATALOG ITS_FTS_Catalog;

CREATE FULLTEXT INDEX ON tickets.Tickets(Title, Description)
    KEY INDEX UX_Tickets_ProjectNumber
    ON ITS_FTS_Catalog WITH CHANGE_TRACKING AUTO;

CREATE FULLTEXT INDEX ON content.Comments(Body)
    KEY INDEX ... WITH CHANGE_TRACKING AUTO;
```

At 10,000+ users / millions of tickets, migrate to Elasticsearch with ticket `Id` as the document key.

---

## Soft Delete Pattern

```sql
-- Standard query (automatic via EF Core global query filter):
SELECT * FROM tickets.Tickets WHERE IsDeleted = 0

-- Admin restore:
UPDATE tickets.Tickets
SET IsDeleted = 0, DeletedAt = NULL, DeletedByUserId = NULL
WHERE TicketId = @Id
```

Unique constraints use filtered indexes (`WHERE IsDeleted = 0`) to allow key reuse after deletion and avoid constraint violations on deleted records.
