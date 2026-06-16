# Deployment Guide

## Internal Issue Tracking System (ITS)

**Version:** 1.0  
**Last Updated:** June 2026

---

## Prerequisites

| Component | Minimum Version | Notes |
|-----------|----------------|-------|
| Windows Server | 2022 | Or Linux (Ubuntu 22.04+) for API servers |
| .NET Runtime | 9.0 | ASP.NET Core hosting bundle |
| SQL Server | 2022 | Developer/Standard/Enterprise |
| IIS or nginx | IIS 10 / nginx 1.24+ | Reverse proxy |
| Node.js | 20 LTS | For React frontend build only |

---

## 1. SQL Server Setup

```sql
-- Create database
CREATE DATABASE ITS COLLATE Latin1_General_CI_AS;
GO

-- Create application login (least privilege)
CREATE LOGIN its_app WITH PASSWORD = 'StrongPassword!';
GO

USE ITS;
CREATE USER its_app FOR LOGIN its_app;
GO

-- Grant minimal permissions (EF migrations need db_owner initially, downgrade after)
ALTER ROLE db_datareader ADD MEMBER its_app;
ALTER ROLE db_datawriter ADD MEMBER its_app;
GRANT EXECUTE TO its_app;
GRANT CREATE TABLE TO its_app;  -- for migrations
GO

-- Create schemas
CREATE SCHEMA identity;
CREATE SCHEMA projects;
CREATE SCHEMA workflow;
CREATE SCHEMA tickets;
CREATE SCHEMA content;
CREATE SCHEMA audit;
CREATE SCHEMA notifications;
CREATE SCHEMA config;
GO
```

### SQL Server Always On (High Availability)

Configure a 2-node Always On Availability Group for the ITS database:
- Primary: `sql-its-01.yourcompany.com`
- Secondary (readable): `sql-its-02.yourcompany.com`
- Listener: `sql-its-ag.yourcompany.com,1433`

Connection string uses the AG listener and enables read intent:
```
Server=sql-its-ag.yourcompany.com;Database=ITS;User Id=its_app;Password=...;
MultipleActiveResultSets=True;ApplicationIntent=ReadWrite;
```

---

## 2. EF Core Migrations

Run migrations from the API project directory:

```bash
# Apply all pending migrations
dotnet ef database update --project src/ITS.Infrastructure --startup-project src/ITS.Api

# Create a new migration (development only)
dotnet ef migrations add <MigrationName> --project src/ITS.Infrastructure --startup-project src/ITS.Api

# Generate idempotent SQL script for DBA-controlled environments
dotnet ef migrations script --idempotent --output migrations.sql \
  --project src/ITS.Infrastructure --startup-project src/ITS.Api
```

**Production workflow:** Generate the idempotent SQL script, review with DBA, apply in a change window with a backup taken first.

### Seed Data

After applying migrations, seed the system:

```sql
-- System Roles
INSERT INTO identity.Roles (Name, Description, Scope, IsSystemRole)
VALUES
  ('System Administrator', 'Full system access', 'Global', 1),
  ('Department Manager',   'Department-level access', 'Global', 1),
  ('Project Lead',         'Project configuration access', 'Global', 1),
  ('Member',               'Standard member access', 'Global', 1),
  ('Read-Only Guest',      'View-only access', 'Global', 1),
  ('Project Admin',        'Project admin access', 'Project', 1),
  ('Developer',            'Developer access', 'Project', 1),
  ('Viewer',               'View-only project access', 'Project', 1);

-- Priorities
INSERT INTO projects.Priorities (Name, Color, DisplayOrder, SlaTargetHours, IsActive, IsSystemPriority)
VALUES
  ('Critical', '#FF0000', 1, 4,   1, 1),
  ('High',     '#FF8C00', 2, 8,   1, 1),
  ('Medium',   '#FFA500', 3, 24,  1, 1),
  ('Low',      '#0070D1', 4, 72,  1, 1),
  ('None',     '#8C8C8C', 5, NULL, 1, 1);

-- Resolutions
INSERT INTO tickets.Resolutions (Name, DisplayOrder, IsActive, IsSystemDefault)
VALUES
  ('Fixed',             1, 1, 1),
  ('Won''t Fix',        2, 1, 0),
  ('Duplicate',         3, 1, 0),
  ('Cannot Reproduce',  4, 1, 0),
  ('Done',              5, 1, 0);

-- Ticket Link Types
INSERT INTO tickets.TicketLinkTypes (Name, InwardName, OutwardName)
VALUES
  ('blocks',     'is blocked by', 'blocks'),
  ('relates',    'relates to',    'relates to'),
  ('duplicates', 'is duplicated by', 'duplicates'),
  ('clones',     'is cloned by',  'clones');

-- System user (sentinel for background jobs)
INSERT INTO identity.Users (UserId, AdObjectId, UserPrincipalName, Email, DisplayName, IsActive,
  TimeZoneId, Locale, CreatedAt, UpdatedAt, CreatedByUserId, UpdatedByUserId)
VALUES (
  '00000000-0000-0000-0000-000000000001', 'SYSTEM', 'system@its.local',
  'system@its.local', 'ITS System', 1, 'UTC', 'en-US',
  SYSUTCDATETIME(), SYSUTCDATETIME(),
  '00000000-0000-0000-0000-000000000001',
  '00000000-0000-0000-0000-000000000001'
);
```

---

## 3. API Server Deployment

### Build

```bash
cd /path/to/repo
dotnet publish src/ITS.Api/ITS.Api.csproj \
  --configuration Release \
  --output /deploy/its-api \
  --runtime win-x64 \
  --self-contained false
```

### Configuration (never put secrets in appsettings.json)

Use one of:

**Option A — Environment variables** (recommended for containers):
```bash
Jwt__Key=your-secret-key-here
ActiveDirectory__BindPassword=service-account-password
ConnectionStrings__DefaultConnection=Server=...
```

**Option B — Windows DPAPI / User Secrets** (dev only):
```bash
dotnet user-secrets set "Jwt:Key" "your-secret-key" --project src/ITS.Api
```

**Option C — Azure Key Vault / HashiCorp Vault** (recommended for production):
Wire via `builder.Configuration.AddAzureKeyVault(...)` or Vault agent sidecar.

### IIS Deployment

1. Install the [.NET 9 Hosting Bundle](https://dotnet.microsoft.com/download) on IIS server.
2. Create an IIS Application Pool: **No Managed Code**, Identity = `ITS_AppPool` (domain service account).
3. Create a website pointing to `/deploy/its-api`.
4. Add `web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\ITS.Api.dll"
                stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

### Windows Service (alternative to IIS)

```bash
sc create "ITS-API" binPath="C:\deploy\its-api\ITS.Api.exe" start=auto
sc start "ITS-API"
```

---

## 4. Frontend Deployment

```bash
cd frontend
npm ci
npm run build          # outputs to frontend/dist/

# Copy dist/ to IIS static site or serve via nginx
```

**nginx reverse proxy:**

```nginx
server {
    listen 443 ssl;
    server_name its.yourcompany.com;

    ssl_certificate     /etc/ssl/certs/its.crt;
    ssl_certificate_key /etc/ssl/private/its.key;

    # Serve React SPA
    root /var/www/its-frontend;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Proxy API
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Serve uploaded files
    location /files/ {
        alias /var/its/attachments/;
        add_header Content-Disposition "attachment";
        expires 1y;
    }
}
```

---

## 5. File Storage

### Network Share (default)

```
\\fileserver01\ITS-Attachments\
```

Grant the API service account `Modify` permission to the share and NTFS folder.

Set in `appsettings.json`:
```json
"FileStorage": {
  "BasePath": "\\\\fileserver01\\ITS-Attachments",
  "BaseUrl": "https://its.yourcompany.com/files"
}
```

### Azure Blob Storage (future)

Swap `FileStorageService` implementation for `AzureBlobStorageService` implementing `IFileStorageService`. No other code changes required (interface is the boundary).

---

## 6. Scaling Recommendations

### Horizontal API Scaling

The API tier is stateless (JWT auth, no in-memory session state). Add nodes behind a load balancer with sticky session disabled.

```
         ┌─────────────────────────────────┐
         │       Load Balancer (IIS ARR)   │
         └───────────────┬─────────────────┘
            ┌────────────┼────────────┐
            ▼            ▼            ▼
         API Node 1   API Node 2   API Node 3
```

**Data Protection keys** (for anti-forgery tokens if used) must be shared across nodes:
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"\\fileserver01\ITS-Keys"))
    .SetApplicationName("ITS");
```

### Distributed Caching

Add Redis for:
- User permission cache (avoids DB round-trip on every request)
- Project metadata cache
- Notification unread count

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = configuration.GetConnectionString("Redis"));
```

### Database Read Replicas

Route reporting and audit queries to the Always On secondary (readable replica):

```csharp
// In queries that don't need the latest data:
optionsBuilder.UseSqlServer(readReplicaConnectionString);
```

Or use a dedicated read-only `DbContext` registered separately.

---

## 7. Backup Strategy

| Target | Frequency | Retention |
|--------|-----------|-----------|
| Full DB backup | Daily | 30 days |
| Differential backup | Every 4 hours | 7 days |
| Transaction log backup | Every 15 minutes | 7 days |
| Attachment files | Daily robocopy to secondary share | 30 days |

**Recovery Point Objective (RPO):** ≤ 15 minutes  
**Recovery Time Objective (RTO):** ≤ 2 minutes (Always On failover)

---

## 8. Security Checklist

- [ ] JWT secret key ≥ 32 characters, stored in vault
- [ ] AD bind password stored in vault, never in config files
- [ ] HTTPS enforced; HTTP redirected
- [ ] TLS 1.2+ only; disable TLS 1.0/1.1
- [ ] File upload path validated to prevent path traversal
- [ ] CORS origins restricted to known frontend hostnames
- [ ] SQL Server: least-privilege app login (`db_datareader` + `db_datawriter` post-migration)
- [ ] Attachment MIME type allowlist enforced
- [ ] Request size limit set (`RequestSizeLimitAttribute` on upload endpoints)
- [ ] Serilog configured to not log request bodies (avoid credential exposure)
- [ ] `appsettings.Production.json` in `.gitignore`

---

## 9. Health Monitoring

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Load balancer health probe |
| SQL Server Agent Jobs | Alert on failed AD sync, email queue growth > 500 |
| Serilog file sink | `/logs/its-*.log` — rotate daily, retain 30 days |
| Windows Event Log | Application errors from unhandled exceptions |

Set up SMTP alerts or integrate with your monitoring platform (Zabbix, Grafana, PRTG) on:
- API response time p95 > 1 second
- Error rate > 1%
- Email queue size > 1,000 items
- Disk usage on attachment share > 80%
