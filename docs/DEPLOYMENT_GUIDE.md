# ITS Deployment Guide

This guide covers deploying the Internal Ticketing System (ITS) to a production Linux server using Docker Compose.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [First-Time Deployment](#first-time-deployment)
3. [Database Migrations](#database-migrations)
4. [Environment Variables Reference](#environment-variables-reference)
5. [Reverse Proxy Configuration (nginx)](#reverse-proxy-configuration-nginx)
6. [Health Check Endpoint](#health-check-endpoint)
7. [Log File Locations](#log-file-locations)
8. [Backup Procedures](#backup-procedures)
9. [Upgrade Procedure](#upgrade-procedure)
10. [Rollback Procedure](#rollback-procedure)
11. [Troubleshooting](#troubleshooting)

---

## Prerequisites

| Component | Minimum Version | Notes |
|-----------|----------------|-------|
| Docker Engine | 24.0 | `docker --version` |
| Docker Compose (plugin) | 2.20 | `docker compose version` |
| .NET 9 SDK | 9.0 | Required only for running migrations outside Docker |
| Node.js | 20 LTS | Required only for local frontend builds |
| SQL Server | 2019 or 2022 | Provided via Docker image or external instance |
| nginx | 1.24+ | Recommended reverse proxy |

The production server requires outbound internet access to:
- `mcr.microsoft.com` — pull the SQL Server image
- Your AI provider endpoint (OpenAI / Azure OpenAI) if AI features are enabled
- Your SMTP relay for email notifications

---

## First-Time Deployment

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/Ticketing-systemInternal.git /opt/its
cd /opt/its
```

### 2. Configure Environment Variables

```bash
cp .env.example .env
nano .env   # or use your preferred editor
```

Fill in every `REPLACE_ME` value. Pay particular attention to:

- `MSSQL_SA_PASSWORD` — must satisfy SQL Server complexity requirements (upper, lower, digit, symbol, min 8 chars)
- `Jwt__Key` — minimum 32 random characters; generate with `openssl rand -base64 48`
- `ActiveDirectory__BindPassword` — service account password for LDAP bind
- `Cors__AllowedOrigins__0` — exact public URL of the frontend (e.g. `https://its.yourcompany.com`)

### 3. Build and Start the Stack

```bash
docker compose up -d --build
```

This will:
1. Pull the SQL Server 2022 image and start it with a health check
2. Build the API image and start it (waits for SQL Server to be healthy)
3. Build the frontend image (nginx static) and start it (waits for API to be healthy)

Monitor startup:
```bash
docker compose logs -f
```

### 4. Run Database Migrations

The API does **not** auto-migrate in production. Run migrations explicitly after the first start (and after each upgrade):

```bash
# Option A — via dotnet CLI (requires .NET 9 SDK on the host)
cd /opt/its/src/ITS.Api
dotnet ef database update \
  --connection "Server=localhost,1433;Database=ITS;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=True"

# Option B — via a one-off Docker container
docker compose run --rm api \
  dotnet ef database update \
  --project /app/ITS.Api.dll
```

### 5. Verify Deployment

```bash
# Health endpoint
curl http://localhost:5000/health

# Expected:
# {"status":"Healthy","totalDurationMs":...,"checks":[...]}
```

---

## Database Migrations

ITS uses EF Core 9 Code-First migrations. The migrations project is in `src/ITS.Infrastructure`.

### Generate a New Migration (Development)

```bash
cd /opt/its
dotnet ef migrations add <MigrationName> \
  --project src/ITS.Infrastructure \
  --startup-project src/ITS.Api
```

### Apply Pending Migrations (Production)

```bash
dotnet ef database update \
  --project src/ITS.Infrastructure \
  --startup-project src/ITS.Api \
  --connection "<production-connection-string>"
```

### Generate a SQL Script (Recommended for Production)

```bash
dotnet ef migrations script \
  --project src/ITS.Infrastructure \
  --startup-project src/ITS.Api \
  --output /tmp/migration.sql \
  --idempotent
```

Review the generated SQL before applying it to the production database.

---

## Environment Variables Reference

All environment variables can be set in the `.env` file or passed directly via your deployment platform.

### Database

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `MSSQL_SA_PASSWORD` | Yes | — | SQL Server SA password (used by the `sqlserver` container) |
| `ConnectionStrings__DefaultConnection` | Yes | — | Full ADO.NET connection string for the API |

### JWT Authentication

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `Jwt__Key` | Yes | — | HMAC-SHA256 signing key, minimum 32 characters |
| `Jwt__Issuer` | Yes | — | Token issuer claim (public API base URL) |
| `Jwt__Audience` | Yes | — | Token audience claim (typically same as issuer) |
| `Jwt__ExpiryHours` | No | `8` | Token lifetime in hours (no refresh endpoint) |

### Active Directory

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ActiveDirectory__Host` | Yes | — | Domain controller hostname or IP |
| `ActiveDirectory__Port` | No | `389` | LDAP port (use 636 for LDAPS) |
| `ActiveDirectory__BindDn` | Yes | — | Distinguished Name of the service account |
| `ActiveDirectory__BindPassword` | Yes | — | Service account password |
| `ActiveDirectory__SearchBase` | Yes | — | LDAP search base (e.g. `DC=yourcompany,DC=com`) |
| `ActiveDirectory__Domain` | Yes | — | Short domain name (e.g. `yourcompany.com`) |

### Email

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `Email__FromAddress` | Yes | — | Sender email address |
| `Email__FromName` | No | `ITS - Issue Tracking System` | Display name |
| `Email__SmtpHost` | Yes | — | SMTP relay hostname |
| `Email__SmtpPort` | No | `25` | SMTP port |
| `Email__EnableSsl` | No | `false` | Enable STARTTLS / SSL |
| `Email__SmtpUsername` | No | — | SMTP username (leave blank for anonymous relay) |
| `Email__SmtpPassword` | No | — | SMTP password |

### File Storage

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `FileStorage__BaseUrl` | Yes | — | Public URL prefix used to build attachment download links |

### AI Features

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `Ai__Provider` | No | `Mock` | AI backend: `Mock`, `OpenAI`, or `AzureOpenAI` |
| `Ai__OpenAi__ApiKey` | Conditional | — | OpenAI API key (required when `Ai__Provider=OpenAI`) |
| `Ai__OpenAi__Model` | No | `gpt-4o-mini` | OpenAI model name |
| `Ai__AzureOpenAi__Endpoint` | Conditional | — | Azure OpenAI resource endpoint |
| `Ai__AzureOpenAi__ApiKey` | Conditional | — | Azure OpenAI API key |
| `Ai__AzureOpenAi__DeploymentName` | Conditional | — | Azure deployment name |

### CORS

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `Cors__AllowedOrigins__0` | Yes | — | Primary allowed origin (frontend URL) |
| `Cors__AllowedOrigins__1` | No | — | Additional origin (e.g. staging URL) |

### Logging

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `Serilog__MinimumLevel__Default` | No | `Warning` | Root log level: `Debug`, `Information`, `Warning`, `Error` |

---

## Reverse Proxy Configuration (nginx)

Place this configuration in `/etc/nginx/sites-available/its` and symlink to `sites-enabled`:

```nginx
# HTTP → HTTPS redirect
server {
    listen 80;
    server_name its.yourcompany.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name its.yourcompany.com;

    # SSL certificates (e.g. from Let's Encrypt or your internal CA)
    ssl_certificate     /etc/ssl/certs/its.yourcompany.com.crt;
    ssl_certificate_key /etc/ssl/private/its.yourcompany.com.key;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    ssl_session_cache   shared:SSL:10m;

    # Security headers
    add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
    add_header X-Frame-Options DENY always;
    add_header X-Content-Type-Options nosniff always;
    add_header Referrer-Policy strict-origin-when-cross-origin always;

    # Frontend (static React SPA)
    location / {
        proxy_pass         http://127.0.0.1:3000;
        proxy_http_version 1.1;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }

    # API
    location /api/ {
        proxy_pass         http://127.0.0.1:5000/api/;
        proxy_http_version 1.1;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 120s;

        # File uploads up to 25 MB
        client_max_body_size 26m;
    }

    # Health check (internal only — restrict if needed)
    location /health {
        proxy_pass http://127.0.0.1:5000/health;
    }
}
```

After saving:
```bash
nginx -t && systemctl reload nginx
```

---

## Health Check Endpoint

The API exposes a health check at `GET /health` (no authentication required).

**Sample response:**
```json
{
  "status": "Healthy",
  "totalDurationMs": 12.4,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "durationMs": 11.1,
      "description": "SQL Server connection OK"
    }
  ]
}
```

Possible status values: `Healthy`, `Degraded`, `Unhealthy`.

---

## Log File Locations

| Location | Description |
|----------|-------------|
| `api_logs` Docker volume → `/app/logs/its-YYYYMMDD.log` | Rolling daily API log files (retained 30 days) |
| `docker compose logs its-api` | Live container stdout/stderr |
| `docker compose logs its-sqlserver` | SQL Server container output |

To follow logs in real time:
```bash
docker compose logs -f api
```

To access log files directly:
```bash
docker run --rm -v its_api_logs:/logs alpine ls /logs
docker run --rm -v its_api_logs:/logs alpine cat /logs/its-20260621.log
```

---

## Backup Procedures

### SQL Server Database Backup

```bash
# Full backup via sqlcmd inside the container
docker exec its-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
  -Q "BACKUP DATABASE [ITS] TO DISK = '/var/opt/mssql/backup/ITS_$(date +%Y%m%d_%H%M%S).bak' WITH COMPRESSION, STATS = 10"
```

Copy the backup file off the container:
```bash
docker cp its-sqlserver:/var/opt/mssql/backup/ITS_<timestamp>.bak /opt/backups/
```

### Automate with Cron

```cron
# Daily backup at 02:00
0 2 * * * docker exec its-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<password>" -C -Q "BACKUP DATABASE [ITS] TO DISK = '/var/opt/mssql/backup/ITS_$(date +\%Y\%m\%d).bak' WITH COMPRESSION" && docker cp its-sqlserver:/var/opt/mssql/backup/ITS_$(date +%Y%m%d).bak /opt/backups/
```

### Attachment Volume Backup

```bash
# Tar the Docker volume content
docker run --rm \
  -v its_api_attachments:/data \
  -v /opt/backups:/backup \
  alpine tar czf /backup/attachments-$(date +%Y%m%d).tar.gz -C /data .
```

---

## Upgrade Procedure

1. **Pull latest code:**
   ```bash
   cd /opt/its
   git pull origin main
   ```

2. **Review `.env.example` for new variables** and update `.env` accordingly.

3. **Rebuild images:**
   ```bash
   docker compose build --no-cache
   ```

4. **Apply database migrations** (see [Database Migrations](#database-migrations)).

5. **Restart services:**
   ```bash
   docker compose up -d
   ```

6. **Verify health:**
   ```bash
   curl http://localhost:5000/health
   ```

---

## Rollback Procedure

1. **Identify the previous image tag** (or git commit):
   ```bash
   git log --oneline -5
   git checkout <previous-commit-or-tag>
   ```

2. **Rebuild and restart from the previous commit:**
   ```bash
   docker compose build --no-cache
   docker compose up -d
   ```

3. **Reverse the migration** (if schema changed):
   ```bash
   dotnet ef database update <PreviousMigrationName> \
     --project src/ITS.Infrastructure \
     --startup-project src/ITS.Api \
     --connection "<connection-string>"
   ```

4. **Restore the database backup** if data was corrupted:
   ```bash
   docker exec its-sqlserver /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
     -Q "RESTORE DATABASE [ITS] FROM DISK = '/var/opt/mssql/backup/ITS_<timestamp>.bak' WITH REPLACE"
   ```

---

## Troubleshooting

### API cannot connect to SQL Server

- Confirm `sqlserver` container is healthy: `docker compose ps`
- Verify `ConnectionStrings__DefaultConnection` uses `Server=sqlserver` (the Docker service name), not `localhost`
- Check SA password complexity: SQL Server requires upper, lower, digit, and symbol characters
- Inspect SQL Server logs: `docker compose logs sqlserver`

### Active Directory authentication failing

- Verify the domain controller is reachable from the container: `docker exec its-api ping <AD_HOST>`
- Test LDAP bind manually:
  ```bash
  docker exec its-api ldapsearch -H ldap://<AD_HOST>:389 \
    -D "<BindDn>" -w "<BindPassword>" -b "<SearchBase>" "(sAMAccountName=testuser)"
  ```
- Confirm `ActiveDirectory__SearchBase` format: `DC=yourcompany,DC=com`
- Ensure the service account has read access to the user OU

### JWT configuration errors

- `IDX10720` or `SecurityTokenSignatureKeyNotFoundException` — the `Jwt__Key` is too short or mismatched between API instances
- Ensure `Jwt__Issuer` and `Jwt__Audience` exactly match the values used when tokens were issued
- Check token expiry: tokens are valid for `Jwt__ExpiryHours` hours (default 8), with no refresh endpoint

### AI provider errors

- `Ai__Provider=Mock` is always safe and returns canned responses — use it to confirm the rest of the stack works
- For OpenAI: verify the API key is valid and the selected model is available in your account
- For Azure OpenAI: confirm the deployment name matches exactly (case-sensitive) and the resource endpoint URL ends with `/`
- Rate limit errors (HTTP 429) from the AI provider are expected under heavy load and will surface as 503 responses from `/api/ai/*` endpoints

### Email notifications not sending

- Check the background email queue via application logs: `docker compose logs api | grep -i email`
- Confirm the SMTP host is reachable: `docker exec its-api nc -zv <SmtpHost> <SmtpPort>`
- If using anonymous relay, ensure `Email__SmtpUsername` and `Email__SmtpPassword` are left empty

### Attachment uploads failing

- Verify the `api_attachments` volume is mounted correctly: `docker inspect its-api | grep Mounts -A 20`
- Check disk space on the host: `df -h`
- Ensure the nginx `client_max_body_size` is set to at least 26m (matches the 25 MB attachment limit)
