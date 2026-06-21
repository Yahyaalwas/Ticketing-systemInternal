# ─── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY ITS.sln .
COPY src/ITS.Api/ITS.Api.csproj               src/ITS.Api/
COPY src/ITS.Application/ITS.Application.csproj   src/ITS.Application/
COPY src/ITS.Domain/ITS.Domain.csproj             src/ITS.Domain/
COPY src/ITS.Infrastructure/ITS.Infrastructure.csproj src/ITS.Infrastructure/
COPY src/ITS.Shared/ITS.Shared.csproj             src/ITS.Shared/

RUN dotnet restore src/ITS.Api/ITS.Api.csproj

# Copy source
COPY src/ src/

# Publish
RUN dotnet publish src/ITS.Api/ITS.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system --gid 1001 its && \
    adduser --system --uid 1001 --ingroup its its

# Writable directories for logs and file attachments
RUN mkdir -p /app/logs /app/attachments && \
    chown -R its:its /app

COPY --from=build --chown=its:its /app/publish .

USER its

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ITS.Api.dll"]
