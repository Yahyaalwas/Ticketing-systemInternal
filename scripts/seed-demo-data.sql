-- ============================================================
-- ITS Demo Data Seed Script
-- Version: 1.0.0
-- ⚠️  FOR DEVELOPMENT AND DEMONSTRATION ONLY — DO NOT RUN IN PRODUCTION
-- ============================================================
-- This script inserts realistic demo data into an ITS database
-- that already has the schema and reference data applied.
--
-- Prerequisites:
--   1. Database created and EF Core migrations applied
--   2. ApplicationDbContextSeed.SeedAsync() run (creates default workflow)
--
-- Idempotent: uses IF NOT EXISTS / MERGE guards so it is safe to
-- run multiple times.
--
-- Usage:
--   sqlcmd -S <server> -d ITS -i scripts/seed-demo-data.sql -C
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- ──────────────────────────────────────────────────────────────
-- 0. Declare well-known GUIDs for reproducibility
-- ──────────────────────────────────────────────────────────────
DECLARE @AdminId   UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @LeadId    UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @MemberId  UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @Member2Id UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';

DECLARE @ProjectAlphaId UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';
DECLARE @ProjectBetaId  UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000002';

-- Grab workflow and status IDs seeded by the application seeder
DECLARE @WorkflowId    INT;
DECLARE @StatusNew     INT;
DECLARE @StatusOpen    INT;
DECLARE @StatusInProg  INT;
DECLARE @StatusReview  INT;
DECLARE @StatusDone    INT;
DECLARE @StatusClosed  INT;

SELECT @WorkflowId = Id FROM dbo.Workflows WHERE Name = 'Default Software Workflow';

SELECT @StatusNew    = Id FROM dbo.WorkflowStatuses WHERE WorkflowId = @WorkflowId AND Name = 'New';
SELECT @StatusOpen   = Id FROM dbo.WorkflowStatuses WHERE WorkflowId = @WorkflowId AND Name = 'Open';
SELECT @StatusInProg = Id FROM dbo.WorkflowStatuses WHERE WorkflowId = @WorkflowId AND Name = 'In Progress';
SELECT @StatusReview = Id FROM dbo.WorkflowStatuses WHERE WorkflowId = @WorkflowId AND Name = 'Review';
SELECT @StatusDone   = Id FROM dbo.WorkflowStatuses WHERE WorkflowId = @WorkflowId AND Name = 'Done';
SELECT @StatusClosed = Id FROM dbo.WorkflowStatuses WHERE WorkflowId = @WorkflowId AND Name = 'Closed';

-- Grab issue type and priority IDs
DECLARE @ItBug    INT; SELECT @ItBug    = Id FROM dbo.IssueTypes WHERE Name = 'Bug';
DECLARE @ItStory  INT; SELECT @ItStory  = Id FROM dbo.IssueTypes WHERE Name = 'Story';
DECLARE @ItTask   INT; SELECT @ItTask   = Id FROM dbo.IssueTypes WHERE Name = 'Task';
DECLARE @ItEpic   INT; SELECT @ItEpic   = Id FROM dbo.IssueTypes WHERE Name = 'Epic';

DECLARE @PrioCrit INT; SELECT @PrioCrit = Id FROM dbo.Priorities WHERE Name = 'Critical';
DECLARE @PrioHigh INT; SELECT @PrioHigh = Id FROM dbo.Priorities WHERE Name = 'High';
DECLARE @PrioMed  INT; SELECT @PrioMed  = Id FROM dbo.Priorities WHERE Name = 'Medium';
DECLARE @PrioLow  INT; SELECT @PrioLow  = Id FROM dbo.Priorities WHERE Name = 'Low';

-- ──────────────────────────────────────────────────────────────
-- 1. Demo Users (AD auth — passwords not stored in ITS)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @AdminId)
INSERT INTO dbo.Users (
    Id, AdObjectId, UserPrincipalName, Email, DisplayName,
    FirstName, LastName, JobTitle, IsActive, TimeZoneId, Locale,
    CreatedAt, UpdatedAt)
VALUES
  (@AdminId,  NEWID(), 'admin@demo.its',   'admin@demo.its',   'Alice Admin',
   'Alice',  'Admin',  'System Administrator', 1, 'UTC', 'en-GB', GETUTCDATE(), GETUTCDATE()),
  (@LeadId,   NEWID(), 'lead@demo.its',    'lead@demo.its',    'Bob Lead',
   'Bob',    'Lead',   'Engineering Manager',  1, 'UTC', 'en-GB', GETUTCDATE(), GETUTCDATE()),
  (@MemberId, NEWID(), 'dev1@demo.its',    'dev1@demo.its',    'Carol Developer',
   'Carol',  'Developer', 'Software Engineer', 1, 'UTC', 'en-GB', GETUTCDATE(), GETUTCDATE()),
  (@Member2Id,NEWID(), 'dev2@demo.its',    'dev2@demo.its',    'Dave Developer',
   'Dave',   'Developer', 'Software Engineer', 1, 'UTC', 'en-GB', GETUTCDATE(), GETUTCDATE());

-- ──────────────────────────────────────────────────────────────
-- 2. Assign global roles
-- ──────────────────────────────────────────────────────────────
DECLARE @RoleAdminId INT; SELECT @RoleAdminId = Id FROM dbo.Roles WHERE Name = 'System Administrator';
DECLARE @RoleLeadId  INT; SELECT @RoleLeadId  = Id FROM dbo.Roles WHERE Name = 'Project Lead';
DECLARE @RoleMembId  INT; SELECT @RoleMembId  = Id FROM dbo.Roles WHERE Name = 'Member';

IF NOT EXISTS (SELECT 1 FROM dbo.UserGlobalRoles WHERE UserId = @AdminId AND RoleId = @RoleAdminId)
    INSERT INTO dbo.UserGlobalRoles (UserId, RoleId, GrantedByUserId, GrantedAt)
    VALUES (@AdminId, @RoleAdminId, @AdminId, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.UserGlobalRoles WHERE UserId = @LeadId AND RoleId = @RoleLeadId)
    INSERT INTO dbo.UserGlobalRoles (UserId, RoleId, GrantedByUserId, GrantedAt)
    VALUES (@LeadId, @RoleLeadId, @AdminId, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.UserGlobalRoles WHERE UserId = @MemberId AND RoleId = @RoleMembId)
    INSERT INTO dbo.UserGlobalRoles (UserId, RoleId, GrantedByUserId, GrantedAt)
    VALUES (@MemberId, @RoleMembId, @AdminId, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM dbo.UserGlobalRoles WHERE UserId = @Member2Id AND RoleId = @RoleMembId)
    INSERT INTO dbo.UserGlobalRoles (UserId, RoleId, GrantedByUserId, GrantedAt)
    VALUES (@Member2Id, @RoleMembId, @AdminId, GETUTCDATE());

-- ──────────────────────────────────────────────────────────────
-- 3. Demo Projects
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE Id = @ProjectAlphaId)
BEGIN
    INSERT INTO dbo.Projects (
        Id, ProjectKey, Name, Description, LeadUserId,
        ActiveWorkflowId, IsArchived, IsDeleted,
        CreatedAt, UpdatedAt, CreatedByUserId)
    VALUES
      (@ProjectAlphaId, 'ALPHA', 'Alpha Platform',
       'Core platform product — customer-facing web application.',
       @LeadId, @WorkflowId, 0, 0, GETUTCDATE(), GETUTCDATE(), @AdminId),
      (@ProjectBetaId,  'BETA',  'Beta Ops',
       'Internal operations and infrastructure improvements.',
       @AdminId, @WorkflowId, 0, 0, GETUTCDATE(), GETUTCDATE(), @AdminId);

    -- Initialise sequence counters
    INSERT INTO dbo.ProjectSequences (ProjectId, LastTicketNumber)
    VALUES (@ProjectAlphaId, 0), (@ProjectBetaId, 0);
END

-- ──────────────────────────────────────────────────────────────
-- 4. Project Members
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.ProjectMembers WHERE ProjectId = @ProjectAlphaId AND UserId = @LeadId)
INSERT INTO dbo.ProjectMembers (ProjectId, UserId, RoleId, JoinedAt)
VALUES
  (@ProjectAlphaId, @LeadId,    @RoleLeadId,  GETUTCDATE()),
  (@ProjectAlphaId, @MemberId,  @RoleMembId,  GETUTCDATE()),
  (@ProjectAlphaId, @Member2Id, @RoleMembId,  GETUTCDATE()),
  (@ProjectBetaId,  @AdminId,   @RoleLeadId,  GETUTCDATE()),
  (@ProjectBetaId,  @MemberId,  @RoleMembId,  GETUTCDATE());

-- ──────────────────────────────────────────────────────────────
-- 5. Demo Tickets (ALPHA project, tickets 1–10)
-- ──────────────────────────────────────────────────────────────
-- Helper: advance sequence and return next number
-- We insert tickets manually with fixed numbers for demo reproducibility.
UPDATE dbo.ProjectSequences SET LastTicketNumber = 10 WHERE ProjectId = @ProjectAlphaId;

DECLARE @T1 UNIQUEIDENTIFIER = NEWID();
DECLARE @T2 UNIQUEIDENTIFIER = NEWID();
DECLARE @T3 UNIQUEIDENTIFIER = NEWID();
DECLARE @T4 UNIQUEIDENTIFIER = NEWID();
DECLARE @T5 UNIQUEIDENTIFIER = NEWID();
DECLARE @T6 UNIQUEIDENTIFIER = NEWID();
DECLARE @T7 UNIQUEIDENTIFIER = NEWID();
DECLARE @T8 UNIQUEIDENTIFIER = NEWID();
DECLARE @T9 UNIQUEIDENTIFIER = NEWID();
DECLARE @T10 UNIQUEIDENTIFIER = NEWID();

IF NOT EXISTS (SELECT 1 FROM dbo.Tickets WHERE ProjectId = @ProjectAlphaId AND TicketNumber = 1)
INSERT INTO dbo.Tickets (
    Id, ProjectId, TicketNumber, Title, Description,
    IssueTypeId, StatusId, PriorityId,
    ReporterUserId, AssigneeUserId, CreatedByUserId,
    DueDate, StoryPoints, IsDeleted, CreatedAt, UpdatedAt)
VALUES
  (@T1,  @ProjectAlphaId, 1,
   'Users cannot reset password via email link',
   '## Steps to reproduce' + CHAR(10) + '1. Click Forgot Password on login page' + CHAR(10) + '2. Enter email and submit' + CHAR(10) + '3. Email is received with reset link' + CHAR(10) + '4. Click link — page shows "Token expired" immediately' + CHAR(10) + CHAR(10) + '## Expected behaviour' + CHAR(10) + 'User lands on password reset form and can set a new password.' + CHAR(10) + CHAR(10) + '## Actual behaviour' + CHAR(10) + '"Token expired" error appears even though link was just generated.',
   @ItBug, @StatusInProg, @PrioCrit,
   @Member2Id, @MemberId, @MemberId,
   DATEADD(DAY, -1, GETUTCDATE()), 3, 0, DATEADD(DAY,-5,GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE())),

  (@T2,  @ProjectAlphaId, 2,
   'Dashboard chart does not render on Safari 17',
   '## Environment' + CHAR(10) + '- Browser: Safari 17.2 on macOS Sonoma' + CHAR(10) + '- Reproduced by 3 users in the finance team' + CHAR(10) + CHAR(10) + '## Description' + CHAR(10) + 'The pie chart on the main dashboard is blank. Console shows `TypeError: ctx.roundRect is not a function`.' + CHAR(10) + CHAR(10) + '## Workaround' + CHAR(10) + 'Switch to Chrome or Firefox.',
   @ItBug, @StatusNew, @PrioHigh,
   @LeadId, NULL, @LeadId,
   DATEADD(DAY, 3, GETUTCDATE()), 2, 0, DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE())),

  (@T3,  @ProjectAlphaId, 3,
   'Add export to CSV on the ticket list page',
   'Users have requested the ability to export filtered ticket lists to CSV for reporting in Excel.' + CHAR(10) + CHAR(10) + '## Acceptance criteria' + CHAR(10) + '- Export button visible when at least one ticket is in the list' + CHAR(10) + '- CSV includes: Key, Title, Status, Priority, Assignee, Due Date, Created' + CHAR(10) + '- Filename format: `ITS-export-YYYY-MM-DD.csv`' + CHAR(10) + '- All active filters are respected in the export',
   @ItStory, @StatusOpen, @PrioMed,
   @LeadId, @MemberId, @LeadId,
   DATEADD(DAY, 14, GETUTCDATE()), 5, 0, DATEADD(DAY,-10,GETUTCDATE()), DATEADD(DAY,-2,GETUTCDATE())),

  (@T4,  @ProjectAlphaId, 4,
   'Performance: ticket list takes >4s to load with >500 tickets',
   '## Profiling results' + CHAR(10) + 'SQL trace shows the list query is doing a full table scan on `Tickets`. The `ProjectId` column is not indexed (added after initial schema).' + CHAR(10) + CHAR(10) + '## Proposed fix' + CHAR(10) + 'Add index: `CREATE INDEX IX_Tickets_ProjectId_StatusId ON dbo.Tickets (ProjectId, StatusId) INCLUDE (Title, UpdatedAt)`',
   @ItBug, @StatusReview, @PrioHigh,
   @MemberId, @MemberId, @MemberId,
   DATEADD(DAY, 7, GETUTCDATE()), 2, 0, DATEADD(DAY,-8,GETUTCDATE()), DATEADD(HOUR,-3,GETUTCDATE())),

  (@T5,  @ProjectAlphaId, 5,
   'Implement AI ticket summary on ticket detail page',
   'Integrate the AI summarization endpoint into the ticket detail page. Show a collapsible "AI Summary" card in the right sidebar with a refresh button.',
   @ItStory, @StatusDone, @PrioMed,
   @LeadId, @Member2Id, @LeadId,
   DATEADD(DAY, -7, GETUTCDATE()), 8, 0, DATEADD(DAY,-20,GETUTCDATE()), DATEADD(DAY,-2,GETUTCDATE())),

  (@T6,  @ProjectAlphaId, 6,
   'Set up CI/CD pipeline for automated deployment',
   'Configure GitHub Actions (or equivalent) to: build the .NET API, run unit tests, build the React frontend, and deploy to staging on every push to `main`.',
   @ItTask, @StatusInProg, @PrioHigh,
   @AdminId, @Member2Id, @AdminId,
   DATEADD(DAY, 5, GETUTCDATE()), 5, 0, DATEADD(DAY,-12,GETUTCDATE()), GETUTCDATE()),

  (@T7,  @ProjectAlphaId, 7,
   'Kanban drag-and-drop does not work on touchscreen devices',
   '## Affected devices' + CHAR(10) + '- iPad Pro (Safari)' + CHAR(10) + '- Android tablet (Chrome)' + CHAR(10) + CHAR(10) + '## Behaviour' + CHAR(10) + 'Touch drag starts but card is dropped immediately without moving to a new column.' + CHAR(10) + CHAR(10) + '## Notes' + CHAR(10) + '@dnd-kit requires PointerSensor or TouchSensor. Currently only PointerSensor is configured with a 5px activation distance which may not fire correctly on touch.',
   @ItBug, @StatusOpen, @PrioMed,
   @MemberId, NULL, @MemberId,
   DATEADD(DAY, 10, GETUTCDATE()), 3, 0, DATEADD(DAY,-6,GETUTCDATE()), DATEADD(DAY,-6,GETUTCDATE())),

  (@T8,  @ProjectAlphaId, 8,
   'Q3 Platform Reliability Epic',
   'Parent epic for all reliability and performance work in Q3. Tracks: DB indexing, caching layer, load testing, and observability improvements.',
   @ItEpic, @StatusOpen, @PrioHigh,
   @LeadId, @LeadId, @LeadId,
   DATEADD(DAY, 45, GETUTCDATE()), NULL, 0, DATEADD(DAY,-15,GETUTCDATE()), DATEADD(DAY,-15,GETUTCDATE())),

  (@T9,  @ProjectAlphaId, 9,
   'Add SLA breach indicator to ticket list view',
   'Tickets that have breached their SLA should show a red clock icon in the ticket list. The due date cell already turns red for overdue tickets — extend this to include an SLA icon when `SlaBreachAt` is in the past.',
   @ItStory, @StatusNew, @PrioLow,
   @LeadId, NULL, @LeadId,
   DATEADD(DAY, 21, GETUTCDATE()), 2, 0, DATEADD(DAY,-2,GETUTCDATE()), DATEADD(DAY,-2,GETUTCDATE())),

  (@T10, @ProjectAlphaId, 10,
   'Session expires without warning — users lose unsaved work',
   'When the JWT token expires after 8 hours, the next API call fails silently and the user loses any form data they had not submitted.' + CHAR(10) + CHAR(10) + '## Proposed solution' + CHAR(10) + 'Show a banner 15 minutes before token expiry with a "Refresh session" button that re-authenticates.',
   @ItBug, @StatusOpen, @PrioCrit,
   @Member2Id, NULL, @Member2Id,
   DATEADD(DAY, 2, GETUTCDATE()), NULL, 0, DATEADD(DAY,-1,GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE()));

-- ──────────────────────────────────────────────────────────────
-- 6. Sample Comments
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Comments WHERE TicketId = @T1)
INSERT INTO dbo.Comments (Id, TicketId, AuthorUserId, Body, BodyHtml, IsDeleted, CreatedAt, UpdatedAt)
VALUES
  (NEWID(), @T1, @MemberId,
   'Reproduced locally. The token expiry is set to 1 minute in the dev config instead of 24 hours. Fixing the config value should resolve this.',
   '<p>Reproduced locally. The token expiry is set to 1 minute in the dev config instead of 24 hours. Fixing the config value should resolve this.</p>',
   0, DATEADD(HOUR,-4,GETUTCDATE()), DATEADD(HOUR,-4,GETUTCDATE())),

  (NEWID(), @T1, @LeadId,
   'Good catch. Also make sure the production environment variable `Email__TokenExpiryMinutes` is set correctly before deploying the fix.',
   '<p>Good catch. Also make sure the production environment variable <code>Email__TokenExpiryMinutes</code> is set correctly before deploying the fix.</p>',
   0, DATEADD(HOUR,-2,GETUTCDATE()), DATEADD(HOUR,-2,GETUTCDATE())),

  (NEWID(), @T4, @Member2Id,
   'Index added in migration `20260620_AddTicketProjectIndex`. Query time dropped from 4.2s to 180ms on the staging database (500k rows).',
   '<p>Index added in migration <code>20260620_AddTicketProjectIndex</code>. Query time dropped from 4.2s to 180ms on the staging database (500k rows).</p>',
   0, DATEADD(HOUR,-3,GETUTCDATE()), DATEADD(HOUR,-3,GETUTCDATE()));

-- ──────────────────────────────────────────────────────────────
-- 7. BETA project tickets (3 tickets for variety)
-- ──────────────────────────────────────────────────────────────
UPDATE dbo.ProjectSequences SET LastTicketNumber = 3 WHERE ProjectId = @ProjectBetaId;

IF NOT EXISTS (SELECT 1 FROM dbo.Tickets WHERE ProjectId = @ProjectBetaId AND TicketNumber = 1)
INSERT INTO dbo.Tickets (
    Id, ProjectId, TicketNumber, Title, Description,
    IssueTypeId, StatusId, PriorityId,
    ReporterUserId, AssigneeUserId, CreatedByUserId,
    DueDate, StoryPoints, IsDeleted, CreatedAt, UpdatedAt)
VALUES
  (NEWID(), @ProjectBetaId, 1,
   'Upgrade SQL Server from 2019 to 2022',
   'Plan and execute in-place upgrade of the production SQL Server instance. Includes: pre-upgrade checklist, maintenance window scheduling, post-upgrade verification.',
   @ItTask, @StatusOpen, @PrioMed,
   @AdminId, @AdminId, @AdminId,
   DATEADD(DAY, 30, GETUTCDATE()), 8, 0, DATEADD(DAY,-7,GETUTCDATE()), DATEADD(DAY,-7,GETUTCDATE())),

  (NEWID(), @ProjectBetaId, 2,
   'Set up centralised log aggregation (ELK or Seq)',
   'ITS API logs to rolling files. We need a centralised log viewer accessible to the ops team. Evaluate Seq (lightweight, .NET-native) vs ELK (more powerful).',
   @ItStory, @StatusNew, @PrioLow,
   @AdminId, NULL, @AdminId,
   DATEADD(DAY, 60, GETUTCDATE()), 13, 0, DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE())),

  (NEWID(), @ProjectBetaId, 3,
   'Document disaster recovery runbook for ITS',
   'Create a step-by-step DR runbook covering: database restore from backup, API redeployment, DNS failover, and smoke test checklist.',
   @ItTask, @StatusInProg, @PrioHigh,
   @AdminId, @AdminId, @AdminId,
   DATEADD(DAY, 14, GETUTCDATE()), 5, 0, DATEADD(DAY,-5,GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE()));

-- ──────────────────────────────────────────────────────────────
-- 8. Verify output
-- ──────────────────────────────────────────────────────────────
SELECT
    'Users'    AS [Table], COUNT(*) AS [Seeded] FROM dbo.Users    WHERE Id IN (@AdminId, @LeadId, @MemberId, @Member2Id)
UNION ALL SELECT 'Projects', COUNT(*) FROM dbo.Projects WHERE Id IN (@ProjectAlphaId, @ProjectBetaId)
UNION ALL SELECT 'Tickets (ALPHA)', COUNT(*) FROM dbo.Tickets WHERE ProjectId = @ProjectAlphaId
UNION ALL SELECT 'Tickets (BETA)',  COUNT(*) FROM dbo.Tickets WHERE ProjectId = @ProjectBetaId
UNION ALL SELECT 'Comments', COUNT(*) FROM dbo.Comments;

COMMIT TRANSACTION;
PRINT 'Demo data seeded successfully.';
