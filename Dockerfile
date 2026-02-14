# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["WebShop.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["NuGet.config", "./"]
COPY ["src/WebShop.Api/WebShop.Api.csproj", "src/WebShop.Api/"]
COPY ["src/WebShop.Business/WebShop.Business.csproj", "src/WebShop.Business/"]
COPY ["src/WebShop.Core/WebShop.Core.csproj", "src/WebShop.Core/"]
COPY ["src/WebShop.Infrastructure/WebShop.Infrastructure.csproj", "src/WebShop.Infrastructure/"]
COPY ["src/WebShop.Util/WebShop.Util.csproj", "src/WebShop.Util/"]

# Restore dependencies
RUN dotnet restore "src/WebShop.Api/WebShop.Api.csproj"

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/WebShop.Api
RUN dotnet publish "WebShop.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health checks (minimal footprint)
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app
USER appuser

# Copy published output
COPY --from=build /app/publish .

# Expose port (Kestrel default)
EXPOSE 8080

# Set URLs for Kestrel
ENV ASPNETCORE_URLS=http://+:8080

# Health check (liveness probe)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "WebShop.Api.dll"]
