-- ============================================================
-- ITS Demo Data Seed Script
--
-- PURPOSE:  Populate the database with realistic demo data for
--           development, QA, and demonstration environments.
--
-- WARNING:  DO NOT run this script against a production database.
--           It inserts demo user accounts with well-known passwords
--           and synthetic ticket data that may pollute real data.
--
-- IDEMPOTENT: This script uses IF NOT EXISTS / MERGE patterns so
--             it can be run multiple times safely.
--
-- PREREQUISITES:
--   1. Run EF Core migrations first:
--      dotnet ef database update --project src/ITS.Infrastructure ...
--   2. The ITS database must already exist.
--   3. Reference data (priorities, issue types, statuses, workflows)
--      must already be seeded by ApplicationDbContextSeed.
--
-- DEMO ACCOUNTS:
--   admin@demo.its   (System Administrator)  Password: Demo@Admin1!
--   lead@demo.its    (Project Lead)           Password: Demo@Lead1!
--   member@demo.its  (Member)                 Password: Demo@Member1!
--
--   NOTE: Passwords below are BCrypt hashes of the values above.
--   If you change the hash algorithm or cost factor in appsettings,
--   generate new hashes using a tool like https://bcrypt-generator.com
-- ============================================================

USE ITS;
GO

BEGIN TRANSACTION;

-- ──────────────────────────────────────────────────────────────
-- 1. DEMO USERS
--    UPNs intentionally use @demo.its to avoid clashing with
--    real AD accounts. The IsLdapUser flag is false so these
--    accounts authenticate against the local password hash.
-- ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Users WHERE Upn = 'admin@demo.its')
BEGIN
    INSERT INTO Users (
        Id, Upn, DisplayName, Email, EmployeeId,
        PasswordHash, IsActive, IsLdapUser,
        TimeZoneId, Locale, CreatedAt, UpdatedAt
    ) VALUES (
        'A0000000-0000-0000-0000-000000000001',
        'admin@demo.its',
        'Demo Administrator',
        'admin@demo.its',
        'DEMO-001',
        -- BCrypt hash of "Demo@Admin1!" (cost=12)
        '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/lewKyNiLXCXubhCyO',
        1, 0,
        'UTC', 'en-US',
        GETUTCDATE(), GETUTCDATE()
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Upn = 'lead@demo.its')
BEGIN
    INSERT INTO Users (
        Id, Upn, DisplayName, Email, EmployeeId,
        PasswordHash, IsActive, IsLdapUser,
        TimeZoneId, Locale, CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000-0000-0000-0000-000000000002',
        'lead@demo.its',
        'Demo Project Lead',
        'lead@demo.its',
        'DEMO-002',
        -- BCrypt hash of "Demo@Lead1!" (cost=12)
        '$2a$12$PwxM7HGEuOsGHjk5kQkOcezm5H8K4b9yOoFw5TlFfvkbH6Wa5MHMi',
        1, 0,
        'UTC', 'en-US',
        GETUTCDATE(), GETUTCDATE()
    );
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Upn = 'member@demo.its')
BEGIN
    INSERT INTO Users (
        Id, Upn, DisplayName, Email, EmployeeId,
        PasswordHash, IsActive, IsLdapUser,
        TimeZoneId, Locale, CreatedAt, UpdatedAt
    ) VALUES (
        'C0000000-0000-0000-0000-000000000003',
        'member@demo.its',
        'Demo Team Member',
        'member@demo.its',
        'DEMO-003',
        -- BCrypt hash of "Demo@Member1!" (cost=12)
        '$2a$12$YnPVtxqX1r3zMH6K8s2HNuFhV7cJqP9aOlKw3MfExkRdHuGLnSmBa',
        1, 0,
        'UTC', 'en-US',
        GETUTCDATE(), GETUTCDATE()
    );
END

-- ──────────────────────────────────────────────────────────────
-- 2. SYSTEM ROLE ASSIGNMENTS
--    Assumes a UserRoles join table (UserId, RoleId)
--    and Roles seeded with Id: 1=SystemAdministrator, 2=ProjectLead, 3=Member
-- ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = 'A0000000-0000-0000-0000-000000000001' AND RoleId = 1)
    INSERT INTO UserRoles (UserId, RoleId) VALUES ('A0000000-0000-0000-0000-000000000001', 1);

IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = 'B0000000-0000-0000-0000-000000000002' AND RoleId = 2)
    INSERT INTO UserRoles (UserId, RoleId) VALUES ('B0000000-0000-0000-0000-000000000002', 2);

IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = 'C0000000-0000-0000-0000-000000000003' AND RoleId = 3)
    INSERT INTO UserRoles (UserId, RoleId) VALUES ('C0000000-0000-0000-0000-000000000003', 3);

-- ──────────────────────────────────────────────────────────────
-- 3. DEMO PROJECTS
-- ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Projects WHERE ProjectKey = 'ALPHA')
BEGIN
    INSERT INTO Projects (
        Id, ProjectKey, Name, Description,
        LeadUserId, IsArchived, IsDeleted,
        TicketCounter, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA',
        'Alpha - Core Platform',
        'Main software delivery project for the core platform team. Covers backend APIs, frontend UI, and infrastructure work.',
        'B0000000-0000-0000-0000-000000000002',
        0, 0,
        15,
        DATEADD(MONTH, -3, GETUTCDATE()), GETUTCDATE(),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Projects WHERE ProjectKey = 'BETA')
BEGIN
    INSERT INTO Projects (
        Id, ProjectKey, Name, Description,
        LeadUserId, IsArchived, IsDeleted,
        TicketCounter, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'D0000000-0000-0000-0000-000000000002',
        'BETA',
        'Beta - IT Operations',
        'IT Operations project tracking infrastructure requests, incidents, and maintenance tasks.',
        'A0000000-0000-0000-0000-000000000001',
        0, 0,
        6,
        DATEADD(MONTH, -2, GETUTCDATE()), GETUTCDATE(),
        CAST(NEWID() AS BINARY(8))
    );
END

-- ──────────────────────────────────────────────────────────────
-- 4. PROJECT MEMBERS
-- ──────────────────────────────────────────────────────────────

-- ALPHA project members
IF NOT EXISTS (SELECT 1 FROM ProjectMembers WHERE ProjectId = 'D0000000-0000-0000-0000-000000000001' AND UserId = 'B0000000-0000-0000-0000-000000000002')
    INSERT INTO ProjectMembers (ProjectId, UserId, RoleId, JoinedAt)
    VALUES ('D0000000-0000-0000-0000-000000000001', 'B0000000-0000-0000-0000-000000000002', 2, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM ProjectMembers WHERE ProjectId = 'D0000000-0000-0000-0000-000000000001' AND UserId = 'C0000000-0000-0000-0000-000000000003')
    INSERT INTO ProjectMembers (ProjectId, UserId, RoleId, JoinedAt)
    VALUES ('D0000000-0000-0000-0000-000000000001', 'C0000000-0000-0000-0000-000000000003', 3, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM ProjectMembers WHERE ProjectId = 'D0000000-0000-0000-0000-000000000001' AND UserId = 'A0000000-0000-0000-0000-000000000001')
    INSERT INTO ProjectMembers (ProjectId, UserId, RoleId, JoinedAt)
    VALUES ('D0000000-0000-0000-0000-000000000001', 'A0000000-0000-0000-0000-000000000001', 1, GETUTCDATE());

-- BETA project members
IF NOT EXISTS (SELECT 1 FROM ProjectMembers WHERE ProjectId = 'D0000000-0000-0000-0000-000000000002' AND UserId = 'A0000000-0000-0000-0000-000000000001')
    INSERT INTO ProjectMembers (ProjectId, UserId, RoleId, JoinedAt)
    VALUES ('D0000000-0000-0000-0000-000000000002', 'A0000000-0000-0000-0000-000000000001', 2, GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM ProjectMembers WHERE ProjectId = 'D0000000-0000-0000-0000-000000000002' AND UserId = 'C0000000-0000-0000-0000-000000000003')
    INSERT INTO ProjectMembers (ProjectId, UserId, RoleId, JoinedAt)
    VALUES ('D0000000-0000-0000-0000-000000000002', 'C0000000-0000-0000-0000-000000000003', 3, GETUTCDATE());

-- ──────────────────────────────────────────────────────────────
-- 5. SAMPLE TICKETS -- ALPHA PROJECT
--    IssueTypeId: 1=Bug, 2=Story, 3=Task, 4=Epic
--    PriorityId:  1=Critical, 2=High, 3=Medium, 4=Low
--    StatusId:    1=Backlog, 2=ToDo, 3=InProgress, 4=InReview, 5=Done, 6=Cancelled
-- ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-1')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000001',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-1',
        'Login page throws 500 error when password contains special characters',
        'Steps to reproduce: navigate to /login, enter a password with & or < characters. Expected: successful login. Actual: HTTP 500 — XML parsing exception in the LDAP bind request.',
        1, 1, 3,
        'B0000000-0000-0000-0000-000000000002',
        'C0000000-0000-0000-0000-000000000003',
        DATEADD(DAY, -2, GETUTCDATE()), 3, 4.0,
        0,
        DATEADD(DAY, -10, GETUTCDATE()),
        DATEADD(DAY, -3, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-2')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000002',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-2',
        'Implement Kanban board drag-and-drop for status transitions',
        'Add drag-and-drop support to the Kanban board. Cards should be draggable between status columns. Guarded transitions (those requiring a comment or resolution) must prompt a dialog before confirming. WIP limit violations should highlight the column header in red.',
        2, 2, 4,
        'B0000000-0000-0000-0000-000000000002',
        'C0000000-0000-0000-0000-000000000003',
        DATEADD(DAY, 5, GETUTCDATE()), 8, 12.0,
        0,
        DATEADD(DAY, -14, GETUTCDATE()),
        DATEADD(DAY, -1, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-3')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000003',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-3',
        'Add AI ticket summarisation feature',
        'Integrate with OpenAI GPT-4o-mini to generate structured summaries of tickets including executive summary, blockers, and action items. Summaries should be cached for 1 hour to reduce API costs.',
        2, 2, 5,
        'A0000000-0000-0000-0000-000000000001',
        'B0000000-0000-0000-0000-000000000002',
        DATEADD(DAY, -5, GETUTCDATE()), 5, 8.0,
        0,
        DATEADD(DAY, -21, GETUTCDATE()),
        DATEADD(DAY, -5, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-4')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000004',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-4',
        'Notifications bell badge not updating in real time',
        'The notification badge in the top navigation only updates on page refresh. Expected: count updates dynamically as new notifications arrive without requiring a full reload.',
        1, 3, 2,
        'C0000000-0000-0000-0000-000000000003',
        NULL,
        NULL, 2, 3.0,
        0,
        DATEADD(DAY, -7, GETUTCDATE()),
        DATEADD(DAY, -7, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-5')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000005',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-5',
        'Export ticket list to CSV',
        'Users need to export the current filtered ticket list to a CSV file for reporting. The export should include all visible columns: key, title, status, priority, assignee, due date.',
        2, 3, 1,
        'B0000000-0000-0000-0000-000000000002',
        NULL,
        NULL, 3, 6.0,
        0,
        DATEADD(DAY, -3, GETUTCDATE()),
        DATEADD(DAY, -3, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-6')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000006',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-6',
        'Attachment upload hangs on files larger than 10 MB',
        'When uploading files between 10 MB and 25 MB, the progress bar reaches 100% but the UI freezes and never confirms success. Files under 10 MB work correctly.',
        1, 2, 3,
        'C0000000-0000-0000-0000-000000000003',
        'C0000000-0000-0000-0000-000000000003',
        DATEADD(DAY, 3, GETUTCDATE()), 2, 3.0,
        0,
        DATEADD(DAY, -5, GETUTCDATE()),
        DATEADD(DAY, -2, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-7')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000007',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-7',
        'Implement optimistic concurrency using ETag / If-Match headers',
        'Add row-version based optimistic concurrency control to ticket and project mutation endpoints to prevent lost updates when multiple users edit the same resource simultaneously.',
        3, 2, 5,
        'A0000000-0000-0000-0000-000000000001',
        'B0000000-0000-0000-0000-000000000002',
        DATEADD(DAY, -10, GETUTCDATE()), 5, 8.0,
        0,
        DATEADD(DAY, -30, GETUTCDATE()),
        DATEADD(DAY, -10, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-8')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000008',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-8',
        'Dashboard shows incorrect overdue ticket count',
        'The Overdue count on the dashboard includes tickets in Done and Cancelled status. It should only count open tickets that are past their due date.',
        1, 3, 5,
        'C0000000-0000-0000-0000-000000000003',
        'B0000000-0000-0000-0000-000000000002',
        NULL, 1, 1.0,
        0,
        DATEADD(DAY, -15, GETUTCDATE()),
        DATEADD(DAY, -8, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-9')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000009',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-9',
        'Add natural language ticket search using AI',
        'Allow users to search tickets using plain English queries such as "show me all critical bugs assigned to Alice from last month". The AI translates the query into structured API filter parameters.',
        2, 2, 3,
        'B0000000-0000-0000-0000-000000000002',
        'B0000000-0000-0000-0000-000000000002',
        DATEADD(DAY, 7, GETUTCDATE()), 8, 10.0,
        0,
        DATEADD(DAY, -12, GETUTCDATE()),
        DATEADD(DAY, -1, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'ALPHA-10')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0001-000000000010',
        'D0000000-0000-0000-0000-000000000001',
        'ALPHA-10',
        'Write deployment guide and release documentation for v1.0.0',
        'Create comprehensive deployment guide, admin guide, user guide, API documentation, release notes, and demo seed script for the 1.0.0 release.',
        3, 3, 4,
        'A0000000-0000-0000-0000-000000000001',
        'A0000000-0000-0000-0000-000000000001',
        GETUTCDATE(), 5, 8.0,
        0,
        DATEADD(DAY, -2, GETUTCDATE()),
        GETUTCDATE(),
        CAST(NEWID() AS BINARY(8))
    );
END

-- ──────────────────────────────────────────────────────────────
-- 6. SAMPLE TICKETS -- BETA PROJECT
-- ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'BETA-1')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0002-000000000001',
        'D0000000-0000-0000-0000-000000000002',
        'BETA-1',
        'Set up production SQL Server with Always On availability group',
        'Configure SQL Server 2022 Always On AG with primary and secondary replicas for the ITS production database. Includes automated failover and read-only secondary routing.',
        3, 1, 3,
        'A0000000-0000-0000-0000-000000000001',
        'A0000000-0000-0000-0000-000000000001',
        DATEADD(DAY, 2, GETUTCDATE()), NULL, 16.0,
        0,
        DATEADD(DAY, -8, GETUTCDATE()),
        DATEADD(DAY, -1, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'BETA-2')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0002-000000000002',
        'D0000000-0000-0000-0000-000000000002',
        'BETA-2',
        'Configure nginx reverse proxy with SSL termination',
        'Install and configure nginx on the production host with SSL certificates from the internal CA. Set up HTTP-to-HTTPS redirect, HSTS headers, and proxy rules for the API and frontend containers.',
        3, 2, 5,
        'A0000000-0000-0000-0000-000000000001',
        'C0000000-0000-0000-0000-000000000003',
        DATEADD(DAY, -3, GETUTCDATE()), NULL, 4.0,
        0,
        DATEADD(DAY, -14, GETUTCDATE()),
        DATEADD(DAY, -3, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'BETA-3')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0002-000000000003',
        'D0000000-0000-0000-0000-000000000002',
        'BETA-3',
        'Schedule automated daily database backups',
        'Configure a cron job on the production server to perform a daily SQL Server backup and copy the backup file to network-attached storage. Retention: 30 days.',
        3, 2, 2,
        'A0000000-0000-0000-0000-000000000001',
        NULL,
        DATEADD(DAY, 10, GETUTCDATE()), NULL, 3.0,
        0,
        DATEADD(DAY, -4, GETUTCDATE()),
        DATEADD(DAY, -4, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'BETA-4')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0002-000000000004',
        'D0000000-0000-0000-0000-000000000002',
        'BETA-4',
        'Create IT service account for LDAP bind',
        'Provision a dedicated service account in Active Directory for ITS LDAP authentication. Account requires read-only access to the Users OU. Password rotation policy: 365 days.',
        3, 3, 5,
        'C0000000-0000-0000-0000-000000000003',
        'A0000000-0000-0000-0000-000000000001',
        NULL, NULL, 1.0,
        0,
        DATEADD(DAY, -20, GETUTCDATE()),
        DATEADD(DAY, -15, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

IF NOT EXISTS (SELECT 1 FROM Tickets WHERE TicketKey = 'BETA-5')
BEGIN
    INSERT INTO Tickets (
        Id, ProjectId, TicketKey, Title, Description,
        IssueTypeId, PriorityId, StatusId,
        ReporterUserId, AssigneeUserId,
        DueDate, StoryPoints, EstimatedHours,
        IsDeleted, CreatedAt, UpdatedAt, RowVersion
    ) VALUES (
        'E0000000-0000-0000-0002-000000000005',
        'D0000000-0000-0000-0000-000000000002',
        'BETA-5',
        'Server disk usage alert at 80% — expand attachment volume',
        'The production server disk is at 80% capacity primarily due to attachment storage growth. Expand the Docker volume or migrate attachments to object storage.',
        1, 2, 1,
        'A0000000-0000-0000-0000-000000000001',
        NULL,
        DATEADD(DAY, 14, GETUTCDATE()), NULL, 2.0,
        0,
        DATEADD(DAY, -1, GETUTCDATE()),
        DATEADD(DAY, -1, GETUTCDATE()),
        CAST(NEWID() AS BINARY(8))
    );
END

-- ──────────────────────────────────────────────────────────────
-- 7. SAMPLE COMMENTS
-- ──────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM Comments WHERE TicketId = 'E0000000-0000-0000-0001-000000000001' AND AuthorUserId = 'B0000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO Comments (Id, TicketId, AuthorUserId, ParentCommentId, Body, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (
        'F0000000-0000-0000-0001-000000000001',
        'E0000000-0000-0000-0001-000000000001',
        'B0000000-0000-0000-0000-000000000002',
        NULL,
        'Confirmed reproducible. The LDAP ADsOpenObject call does not encode the password correctly when it contains XML special characters. Assigning to Demo Member to fix the encoding in the LDAP authentication service.',
        0,
        DATEADD(DAY, -9, GETUTCDATE()),
        DATEADD(DAY, -9, GETUTCDATE())
    );
END

IF NOT EXISTS (SELECT 1 FROM Comments WHERE TicketId = 'E0000000-0000-0000-0001-000000000001' AND AuthorUserId = 'C0000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO Comments (Id, TicketId, AuthorUserId, ParentCommentId, Body, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (
        'F0000000-0000-0000-0001-000000000002',
        'E0000000-0000-0000-0001-000000000001',
        'C0000000-0000-0000-0000-000000000003',
        'F0000000-0000-0000-0001-000000000001',
        'Fix in progress. The password must be passed as raw bytes to the LDAP library rather than as a string — XML encoding should not be applied here. ETA: end of day.',
        0,
        DATEADD(DAY, -8, GETUTCDATE()),
        DATEADD(DAY, -8, GETUTCDATE())
    );
END

IF NOT EXISTS (SELECT 1 FROM Comments WHERE TicketId = 'E0000000-0000-0000-0001-000000000002' AND AuthorUserId = 'C0000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO Comments (Id, TicketId, AuthorUserId, ParentCommentId, Body, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (
        'F0000000-0000-0000-0002-000000000001',
        'E0000000-0000-0000-0001-000000000002',
        'C0000000-0000-0000-0000-000000000003',
        NULL,
        'Drag-and-drop prototype is working locally using @hello-pangea/dnd. Drop targets highlight correctly and the transition dialog fires for guarded transitions. Moving to In Review — please test on Safari.',
        0,
        DATEADD(DAY, -1, GETUTCDATE()),
        DATEADD(DAY, -1, GETUTCDATE())
    );
END

IF NOT EXISTS (SELECT 1 FROM Comments WHERE TicketId = 'E0000000-0000-0000-0002-000000000001' AND AuthorUserId = 'A0000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO Comments (Id, TicketId, AuthorUserId, ParentCommentId, Body, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (
        'F0000000-0000-0000-0003-000000000001',
        'E0000000-0000-0000-0002-000000000001',
        'A0000000-0000-0000-0000-000000000001',
        NULL,
        'Primary replica is online and synchronising. Secondary replica has joined the AG and health checks are passing. Completing final failover test today before marking done.',
        0,
        DATEADD(HOUR, -6, GETUTCDATE()),
        DATEADD(HOUR, -6, GETUTCDATE())
    );
END

-- ──────────────────────────────────────────────────────────────
-- 8. VERIFY SEED
-- ──────────────────────────────────────────────────────────────

SELECT
    'Users'           AS [Table], COUNT(*) AS [DemoRows] FROM Users    WHERE Upn LIKE '%@demo.its'
UNION ALL
SELECT 'Projects',                COUNT(*) FROM Projects  WHERE ProjectKey IN ('ALPHA', 'BETA')
UNION ALL
SELECT 'Tickets (ALPHA)',         COUNT(*) FROM Tickets   WHERE ProjectId = 'D0000000-0000-0000-0000-000000000001'
UNION ALL
SELECT 'Tickets (BETA)',          COUNT(*) FROM Tickets   WHERE ProjectId = 'D0000000-0000-0000-0000-000000000002'
UNION ALL
SELECT 'Comments',                COUNT(*) FROM Comments  WHERE TicketId IN (
    SELECT Id FROM Tickets WHERE ProjectId IN (
        'D0000000-0000-0000-0000-000000000001',
        'D0000000-0000-0000-0000-000000000002'
    )
);

COMMIT TRANSACTION;

PRINT 'Demo data seed completed successfully.';
