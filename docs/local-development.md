# ITS — Local Development Setup

## Prerequisites

| Requirement | Minimum version |
|-------------|-----------------|
| .NET SDK | 9.0 |
| SQL Server | 2019 (or SQL Server Express / LocalDB) |
| Active Directory / LDAP | Optional — see Mock AD section |

---

## 1. Configuration

Copy `src/ITS.Api/appsettings.json` and create `appsettings.Development.json` (already tracked) alongside a **user secrets** file for credentials:

```bash
cd src/ITS.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-minimum-32-character-secret-key-here"
dotnet user-secrets set "ActiveDirectory:BindPassword" "your-service-account-password"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ITS_Dev;Trusted_Connection=True;TrustServerCertificate=True"
```

### Required configuration values

| Key | Description | Example |
|-----|-------------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string | `Server=.;Database=ITS;Trusted_Connection=True;TrustServerCertificate=True` |
| `Jwt:Key` | HS256 signing key — **minimum 32 characters** | `SuperSecretKeyThatIsAtLeast32Chars!` |
| `Jwt:Issuer` | JWT issuer claim | `https://its.yourcompany.com` |
| `Jwt:Audience` | JWT audience claim | `https://its.yourcompany.com` |
| `Jwt:ExpiryHours` | Token lifetime in hours | `8` |
| `ActiveDirectory:Host` | Domain controller hostname or IP | `dc01.corp.example.com` |
| `ActiveDirectory:Port` | LDAP port (389 plain, 636 SSL) | `389` |
| `ActiveDirectory:BindDn` | Service account distinguished name | `CN=its-svc,OU=ServiceAccounts,DC=corp,DC=example,DC=com` |
| `ActiveDirectory:BindPassword` | Service account password | *(store in user-secrets)* |
| `ActiveDirectory:SearchBase` | LDAP search root | `DC=corp,DC=example,DC=com` |
| `ActiveDirectory:Domain` | UPN suffix / NetBIOS domain | `corp.example.com` |
| `FileStorage:BasePath` | Absolute path for attachment storage | `C:\ITS\Attachments` |
| `FileStorage:BaseUrl` | Public base URL for file downloads | `https://its.yourcompany.com/files` |
| `Email:SmtpHost` | SMTP server hostname | `mail.yourcompany.com` |
| `Cors:AllowedOrigins` | Array of allowed CORS origins | `["http://localhost:3000"]` |

### Environment variables (alternative to appsettings)

All configuration keys can be overridden via environment variables using `__` as the section separator:

```bash
ConnectionStrings__DefaultConnection="Server=sql;Database=ITS;..."
Jwt__Key="your-secret-key"
ActiveDirectory__Host="dc01.corp.example.com"
```

---

## 2. SQL Server Setup

### Option A — LocalDB (simplest for local dev)

```
Server=(localdb)\MSSQLLocalDB;Database=ITS_Dev;Integrated Security=True;TrustServerCertificate=True
```

### Option B — SQL Server via Docker

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name its-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Connection string:
```
Server=localhost,1433;Database=ITS_Dev;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True
```

---

## 3. Database Migrations

### Generate the initial migration (first time only)

```bash
cd src/ITS.Api
dotnet ef migrations add InitialCreate --project ../ITS.Infrastructure --startup-project .
```

### Apply migrations

```bash
# Development — auto-applied on startup when ASPNETCORE_ENVIRONMENT=Development
dotnet run

# Staging / Production — apply explicitly
dotnet ef database update --project src/ITS.Infrastructure --startup-project src/ITS.Api
```

### Generate SQL script for production deployment

```bash
dotnet ef migrations script --idempotent \
  --project src/ITS.Infrastructure \
  --startup-project src/ITS.Api \
  --output scripts/migrate.sql
```

---

## 4. Active Directory Configuration

The service account (`BindDn`) requires:
- Read access to the Users container in the configured `SearchBase`
- `memberOf` attribute read access (for group membership resolution)

### AD Group → Role mapping

Assign roles by inserting records into `identity.AdGroupRoleMappings`:

```sql
INSERT INTO identity.AdGroupRoleMappings (AdGroupDistinguishedName, AdGroupName, RoleId, ProjectId)
VALUES (
  'CN=ITS-Admins,OU=Groups,DC=corp,DC=example,DC=com',
  'ITS-Admins',
  1,   -- Role 1 = System Administrator (seeded)
  NULL -- NULL = global role
);
```

---

## 5. Running the API

```bash
# Development (auto-migrations, Swagger enabled)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/ITS.Api

# Swagger UI
open http://localhost:5000/swagger
```

---

## 6. Seeded Reference Data

The following is inserted automatically on first startup:

**Roles**

| ID | Name |
|----|------|
| 1 | System Administrator |
| 2 | Department Administrator |
| 3 | Project Manager |
| 4 | Team Lead |
| 5 | Developer |
| 6 | Reporter |
| 7 | Viewer |

**Priorities**

| ID | Name | SLA Target |
|----|------|------------|
| 1 | Low | 72 hours |
| 2 | Medium | 24 hours |
| 3 | High | 8 hours |
| 4 | Critical | 2 hours |

**Resolutions**: Fixed, Won't Fix, Duplicate, Cannot Reproduce, By Design

**Ticket Link Types**: Blocks, Relates To, Duplicates, Clones

**Default Workflow**: "Default Software Workflow" template with statuses New → Open → In Progress → Review → Done → Closed.

---

## 7. Health Check

```bash
curl http://localhost:5000/health
```

Returns JSON with status of the SQL Server connection.

---

## 8. Security Notes

- JWT secret key must be **at least 32 characters**. Store it in user-secrets locally and in a secrets manager (Azure Key Vault, HashiCorp Vault) in production.
- AD service account password must never appear in `appsettings.json` committed to source control.
- Passwords are never stored locally — Active Directory is the sole authentication source of truth.
- HTTPS redirection is enforced; disable only in local HTTP-only dev scenarios.
