# ── Stage 1: Build ────────────────────────────────────────────────────────────
# Use the full .NET 8 SDK image to restore, build, and publish the application.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first and restore NuGet packages.
# This layer is cached separately so a code-only change does not re-run restore.
COPY src/ProductsApi/ProductsApi.csproj ProductsApi/
RUN dotnet restore ProductsApi/ProductsApi.csproj

# Copy all remaining source files and publish a Release build.
COPY src/ProductsApi/ ProductsApi/
RUN dotnet publish ProductsApi/ProductsApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
# Use the lightweight ASP.NET 8 runtime image (no SDK) for the final image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a dedicated non-root system user and group (OWASP best practice).
# Running as a non-root user limits the blast radius of any container escape.
RUN groupadd --system appgroup \
 && useradd  --no-log-init --system --gid appgroup appuser

# Copy published output from the build stage.
COPY --from=build /app/publish .

# Transfer ownership so the non-root user can read the application files.
RUN chown -R appuser:appgroup /app

# Install curl for the HEALTHCHECK instruction below.
# The aspnet:8.0 Debian image does not include curl by default.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

# Switch to the non-root user before starting the process.
USER appuser

# Expose port 8080 (ASP.NET Core default for non-root containers).
EXPOSE 8080

# Docker-native health check: polls the Swagger JSON endpoint every 30 s.
# start_period gives the app time to start before the first check counts.
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl --fail http://localhost:8080/swagger/v1/swagger.json || exit 1

# Tell Kestrel to listen on all interfaces on port 8080.
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ProductsApi.dll"]
