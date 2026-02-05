# Health Checks Implementation Guide

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Why Health Checks?](#why-health-checks)
- [What Problem It Solves](#what-problem-it-solves)
- [How It Works](#how-it-works)
- [Architecture & Design](#architecture--design)
- [Health Check Types](#health-check-types)
- [Implementation Details](#implementation-details)
- [Health Check Endpoints](#health-check-endpoints)
- [Response Format](#response-format)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Kubernetes Integration](#kubernetes-integration)
- [Troubleshooting](#troubleshooting)

---

## Overview

The WebShop API uses **ASP.NET Core Health Checks** to monitor the health and availability of the application and its dependencies. Health checks provide real-time status information about the API, database connections, and other critical components, enabling effective monitoring, alerting, and orchestration in containerized and distributed environments.

## Why Health Checks?

### The Problem Without Health Checks

Without health checks, you cannot:

1. **Monitor Application Health**: No way to know if the API is functioning correctly
2. **Detect Database Issues**: Database connectivity problems go unnoticed until users report errors
3. **Orchestrate Containers**: Container orchestrators (Kubernetes, Docker Swarm) can't determine if containers are ready
4. **Implement Auto-Scaling**: Load balancers can't route traffic away from unhealthy instances
5. **Set Up Alerts**: No automated way to detect and alert on failures
6. **Graceful Degradation**: Can't distinguish between "down" and "degraded" states

### The Solution: Health Checks

Health checks provide:

- **Real-Time Status**: Immediate visibility into application and dependency health
- **Kubernetes Integration**: Readiness and liveness probes for container orchestration
- **Load Balancer Integration**: Automatic traffic routing based on health status
- **Monitoring & Alerting**: Integration with monitoring systems (Prometheus, Application Insights)
- **Graceful Degradation**: Distinguish between healthy, degraded, and unhealthy states

---

## What Problem It Solves

### 1. **Container Orchestration**

**Problem:** Kubernetes needs to know when a container is ready to receive traffic and when it's alive.

**Solution:** Health checks provide:
- **Liveness Probe** (`/health/live`): Determines if the container should be restarted
- **Readiness Probe** (`/health/ready`): Determines if the container can receive traffic

### 2. **Load Balancer Integration**

**Problem:** Load balancers need to route traffic only to healthy instances.

**Solution:** Health checks enable load balancers to:
- Remove unhealthy instances from the pool
- Route traffic only to healthy instances
- Implement automatic failover

### 3. **Database Connectivity Monitoring**

**Problem:** Database connection issues may not surface until a user makes a request.

**Solution:** Health checks proactively test database connectivity:
- Separate checks for read and write databases
- Early detection of connection pool exhaustion
- Detection of network issues

### 4. **Monitoring & Alerting**

**Problem:** No automated way to detect and alert on application failures.

**Solution:** Health checks integrate with monitoring systems:
- Prometheus metrics export
- Application Insights integration
- Custom alerting rules based on health status

### 5. **Graceful Degradation**

**Problem:** Applications are either "up" or "down" - no middle ground.

**Solution:** Health checks support three states:
- **Healthy**: All checks passed, fully operational
- **Degraded**: Some non-critical checks failed, but API is still functional
- **Unhealthy**: Critical checks failed, API should not receive traffic

---

## How It Works

### Health Check Flow

```
1. Request arrives at /health endpoint
   ↓
2. Health Check Service executes all registered checks
   ↓
3. Each check runs independently (parallel execution)
   ↓
4. Results are aggregated into HealthReport
   ↓
5. Response writer formats the report as JSON
   ↓
6. JSON response returned to client
```

### Health Check Execution

1. **Registration**: Health checks are registered during service configuration
2. **Execution**: When an endpoint is called, all matching checks execute
3. **Aggregation**: Results are combined into a single `HealthReport`
4. **Response**: Custom response writer formats the report as JSON

### Health Status Determination

The overall health status is determined by the **worst** status among all checks:

- If any check is **Unhealthy** → Overall status is **Unhealthy**
- If any check is **Degraded** (and none are Unhealthy) → Overall status is **Degraded**
- If all checks are **Healthy** → Overall status is **Healthy**

---

## Architecture & Design

### Component Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Health Check Endpoints                │
│  /health  /health/detailed  /health/ready  /health/live│
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│              Health Check Middleware                    │
│         (MapHealthChecks with Options)                  │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│            Health Check Service                          │
│      (Executes registered health checks)                 │
└────────────────────┬────────────────────────────────────┘
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
┌──────────┐  ┌──────────┐  ┌──────────┐
│   Self   │  │   DB     │  │   DB     │
│  Check   │  │  Read    │  │  Write   │
└──────────┘  └──────────┘  └──────────┘
        │            │            │
        └────────────┼────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│            Response Writer                              │
│    (Formats HealthReport as JSON)                       │
└─────────────────────────────────────────────────────────┘
```

### Health Check Registration

Health checks are registered in `ServiceExtensions.ConfigureHealthChecks()`:

```csharp
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is healthy"))
    .Add(new HealthCheckRegistration(
        "db-read",
        sp => new DapperHealthCheck(
            sp.GetRequiredService<IDapperConnectionFactory>(),
            isWriteConnection: false),
        HealthStatus.Unhealthy,
        tags: new[] { "db", "read" }))
    .Add(new HealthCheckRegistration(
        "db-write",
        sp => new DapperHealthCheck(
            sp.GetRequiredService<IDapperConnectionFactory>(),
            isWriteConnection: true),
        HealthStatus.Unhealthy,
        tags: new[] { "db", "write" }));
```

### Tag-Based Filtering

Health checks use **tags** to group related checks:

- `"db"` - All database-related checks
- `"read"` - Read database checks
- `"write"` - Write database checks
- `"ready"` - Readiness checks

Tags enable filtering at the endpoint level:

```csharp
// Only checks with "db" tag
app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});
```

---

## Health Check Types

### 1. **Self Check**

**Purpose**: Verifies the API itself is running.

**Implementation**: Simple synchronous check that always returns healthy.

```csharp
.AddCheck("self", () => HealthCheckResult.Healthy("API is healthy"))
```

**When to Use**: Basic liveness check - confirms the API process is running.

### 2. **Database Connection Check**

**Purpose**: Verifies database connectivity by executing a simple query.

**Implementation**: Uses custom Dapper-based health check.

```csharp
.Add(new HealthCheckRegistration(
    "db-read",
    sp => new DapperHealthCheck(
        sp.GetRequiredService<IDapperConnectionFactory>(),
        isWriteConnection: false),
    HealthStatus.Unhealthy,
    tags: new[] { "db", "read" }))
.Add(new HealthCheckRegistration(
    "db-write",
    sp => new DapperHealthCheck(
        sp.GetRequiredService<IDapperConnectionFactory>(),
        isWriteConnection: true),
    HealthStatus.Unhealthy,
    tags: new[] { "db", "write" }))
```

**What It Does**:
- Executes `SELECT 1` query against the database
- Verifies connection pool is working
- Detects network connectivity issues
- Checks database authentication

**When to Use**: Always include for applications with database dependencies.

---

## Implementation Details

### Health Check Registration

**Location**: `src/WebShop.Api/Extensions/Core/ServiceExtensions.cs`

```csharp
public static void ConfigureHealthChecks(this IServiceCollection services)
{
    services.AddHealthChecks()
        .AddCheck(HealthCheckSelfName, () => HealthCheckResult.Healthy("API is healthy"))
        .Add(new HealthCheckRegistration(
            HealthCheckDbReadName,
            sp => new DapperHealthCheck(
                sp.GetRequiredService<IDapperConnectionFactory>(),
                isWriteConnection: false),
            HealthStatus.Unhealthy,
            tags: new[] { HealthCheckTagDb, HealthCheckTagRead }))
        .Add(new HealthCheckRegistration(
            HealthCheckDbWriteName,
            sp => new DapperHealthCheck(
                sp.GetRequiredService<IDapperConnectionFactory>(),
                isWriteConnection: true),
            HealthStatus.Unhealthy,
            tags: new[] { HealthCheckTagDb, HealthCheckTagWrite }));
}
```

### Health Check Endpoints

**Location**: `src/WebShop.Api/Extensions/Core/ServiceExtensions.cs`

```csharp
public static void ConfigureHealthCheckEndpoints(this WebApplication app)
{
    // Standard health check
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = WriteEnhancedHealthCheckResponse
    });

    // Detailed health check
    app.MapHealthChecks("/health/detailed", new HealthCheckOptions
    {
        ResponseWriter = WriteDetailedHealthCheckResponse,
        Predicate = _ => true
    });

    // Readiness probe
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains(HealthCheckTagReady),
        ResponseWriter = WriteEnhancedHealthCheckResponse
    });

    // Liveness probe
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = WriteEnhancedHealthCheckResponse
    });

    // Database health check
    app.MapHealthChecks("/health/db", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains(HealthCheckTagDb),
        ResponseWriter = WriteEnhancedHealthCheckResponse
    });
}
```

### Enhanced JSON Response Writers

The implementation includes two custom response writers:

#### 1. **Enhanced Response** (`WriteEnhancedHealthCheckResponse`)

Provides detailed information about each check:

```json
{
  "status": "Healthy",
  "timestamp": "2025-12-27T10:30:00Z",
  "totalDuration": "15.23ms",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "API is healthy",
      "duration": "0.12ms",
      "tags": [],
      "exception": null,
      "data": {}
    },
    {
      "name": "database-read",
      "status": "Healthy",
      "description": "...",
      "duration": "8.45ms",
      "tags": ["db", "read"],
      "exception": null,
      "data": {}
    }
  ]
}
```

#### 2. **Detailed Response** (`WriteDetailedHealthCheckResponse`)

Includes additional information for debugging:

```json
{
  "status": "Healthy",
  "timestamp": "2025-12-27T10:30:00Z",
  "totalDuration": "15.23ms",
  "totalDurationSeconds": 0.01523,
  "checks": [
    {
      "name": "database-read",
      "status": "Healthy",
      "description": "...",
      "duration": "8.45ms",
      "durationSeconds": 0.00845,
      "tags": ["db", "read"],
      "exception": null,
      "data": {},
      "isHealthy": true,
      "isDegraded": false,
      "isUnhealthy": false
    }
  ],
  "summary": {
    "total": 3,
    "healthy": 3,
    "degraded": 0,
    "unhealthy": 0
  }
}
```

**Key Differences**:
- Detailed response includes exception stack traces (if any)
- Boolean flags for each status type
- Summary statistics
- Duration in both milliseconds and seconds

---

## Health Check Endpoints

### Standard Endpoints

| Endpoint | Purpose | Checks Included | Use Case |
|----------|---------|----------------|----------|
| `/health` | Overall health | All checks | General monitoring, load balancers |
| `/health/detailed` | Detailed health | All checks with full details | Debugging, detailed monitoring |
| `/health/ready` | Readiness probe | Checks tagged with "ready" | Kubernetes readiness probe |
| `/health/live` | Liveness probe | No checks (always healthy) | Kubernetes liveness probe |
| `/health/db` | Database health | All database checks | Database-specific monitoring |

### Endpoint Details

#### `/health`

**Purpose**: General health check for monitoring and load balancers.

**Response**: Enhanced JSON with all checks.

**HTTP Status Codes**:
- `200 OK` - Healthy or Degraded
- `503 Service Unavailable` - Unhealthy

#### `/health/detailed`

**Purpose**: Detailed health information for debugging and comprehensive monitoring.

**Response**: Detailed JSON with exception information, summary statistics, and boolean flags.

**Use Cases**:
- Debugging health check failures
- Integration with monitoring systems
- Detailed status dashboards

#### `/health/ready`

**Purpose**: Kubernetes readiness probe - determines if the API can receive traffic.

**Checks**: Only checks tagged with `"ready"`.

**Current Implementation**: No checks are tagged with `"ready"`, so this endpoint always returns healthy. This can be customized based on requirements.

**Kubernetes Integration**:
```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5
```

#### `/health/live`

**Purpose**: Kubernetes liveness probe - determines if the container should be restarted.

**Checks**: No checks (predicate returns false).

**Current Implementation**: Always returns healthy, indicating the process is alive.

**Kubernetes Integration**:
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10
```

#### `/health/db`

**Purpose**: Database-specific health monitoring.

**Checks**: All checks tagged with `"db"` (read and write databases).

**Use Cases**:
- Database connectivity monitoring
- Separate monitoring for database health
- Integration with database monitoring tools

---

## Response Format

### Status Values

Health checks return one of three status values:

1. **Healthy** (`HealthStatus.Healthy`)
   - All checks passed
   - API is fully operational
   - HTTP Status: `200 OK`

2. **Degraded** (`HealthStatus.Degraded`)
   - Some non-critical checks failed
   - API is still functional but may have reduced capabilities
   - HTTP Status: `200 OK` (still accepts traffic)

3. **Unhealthy** (`HealthStatus.Unhealthy`)
   - Critical checks failed
   - API should not receive traffic
   - HTTP Status: `503 Service Unavailable`

### Response Structure

#### Enhanced Response Format

```json
{
  "status": "Healthy|Degraded|Unhealthy",
  "timestamp": "ISO 8601 UTC timestamp",
  "totalDuration": "duration in milliseconds (e.g., '15.23ms')",
  "checks": [
    {
      "name": "check name",
      "status": "Healthy|Degraded|Unhealthy",
      "description": "human-readable description",
      "duration": "duration in milliseconds",
      "tags": ["tag1", "tag2"],
      "exception": "exception message (if any)",
      "data": {
        "key": "value"
      }
    }
  ]
}
```

#### Detailed Response Format

Includes all fields from enhanced response, plus:

```json
{
  "status": "Healthy|Degraded|Unhealthy",
  "timestamp": "ISO 8601 UTC timestamp",
  "totalDuration": "duration in milliseconds",
  "totalDurationSeconds": 0.01523,
  "checks": [
    {
      "name": "check name",
      "status": "Healthy|Degraded|Unhealthy",
      "description": "human-readable description",
      "duration": "duration in milliseconds",
      "durationSeconds": 0.00845,
      "tags": ["tag1", "tag2"],
      "exception": {
        "message": "exception message",
        "type": "ExceptionTypeName",
        "stackTrace": "full stack trace"
      },
      "data": {},
      "isHealthy": true,
      "isDegraded": false,
      "isUnhealthy": false
    }
  ],
  "summary": {
    "total": 3,
    "healthy": 3,
    "degraded": 0,
    "unhealthy": 0
  }
}
```

---

## Configuration

### Health Check Constants

**Location**: `src/WebShop.Api/Extensions/Core/ServiceExtensions.cs`

```csharp
private const string HealthCheckSelfName = "self";
private const string HealthCheckDbReadName = "database-read";
private const string HealthCheckDbWriteName = "database-write";
private const string HealthCheckTagDb = "db";
private const string HealthCheckTagRead = "read";
private const string HealthCheckTagWrite = "write";
private const string HealthCheckTagReady = "ready";
```

### Adding New Health Checks

To add a new health check:

1. **Register the check** in `ConfigureHealthChecks()`:

```csharp
services.AddHealthChecks()
    .AddCheck("custom-check", () =>
    {
        // Your health check logic
        if (IsServiceAvailable())
        {
            return HealthCheckResult.Healthy("Service is available");
        }
        return HealthCheckResult.Unhealthy("Service is unavailable");
    }, tags: ["custom", "ready"]);
```

2. **Create a custom health check class** (for complex checks):

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform health check
            bool isHealthy = await CheckServiceHealthAsync(cancellationToken);
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Custom service is healthy");
            }
            
            return HealthCheckResult.Unhealthy("Custom service is unhealthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Custom service check failed", ex);
        }
    }
}
```

3. **Register the custom check**:

```csharp
services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("custom-check", tags: ["custom"]);
```

---

## Usage Examples

### Basic Health Check Request

```bash
# Standard health check
curl https://localhost:7109/health

# Response
{
  "status": "Healthy",
  "timestamp": "2025-12-27T10:30:00Z",
  "totalDuration": "15.23ms",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "API is healthy",
      "duration": "0.12ms",
      "tags": [],
      "exception": null,
      "data": {}
    },
    {
      "name": "database-read",
      "status": "Healthy",
      "description": "...",
      "duration": "8.45ms",
      "tags": ["db", "read"],
      "exception": null,
      "data": {}
    },
    {
      "name": "database-write",
      "status": "Healthy",
      "description": "...",
      "duration": "9.12ms",
      "tags": ["db", "write"],
      "exception": null,
      "data": {}
    }
  ]
}
```

### Detailed Health Check Request

```bash
# Detailed health check
curl https://localhost:7109/health/detailed

# Response includes exception details, summary, and boolean flags
```

### Database Health Check

```bash
# Database-specific health check
curl https://localhost:7109/health/db

# Response includes only database checks
```

### Readiness Probe

```bash
# Readiness probe (for Kubernetes)
curl https://localhost:7109/health/ready

# Response includes only readiness checks
```

### Liveness Probe

```bash
# Liveness probe (for Kubernetes)
curl https://localhost:7109/health/live

# Response: Always healthy (no checks executed)
```

---

## Best Practices

### 1. **Keep Health Checks Fast**

Health checks should execute quickly (< 1 second):

- ✅ **Good**: Simple database query (`SELECT 1`)
- ❌ **Bad**: Complex queries or external API calls

**Why**: Health checks are called frequently (every 5-10 seconds in Kubernetes). Slow checks can impact performance.

### 2. **Use Tags for Organization**

Group related health checks with tags:

```csharp
.AddCheck("database-read", ..., tags: ["db", "read"])
.AddCheck("database-write", ..., tags: ["db", "write"])
.AddCheck("cache", ..., tags: ["cache", "ready"])
```

**Benefits**:
- Filter checks by category
- Create endpoint-specific health checks
- Organize checks logically

### 3. **Separate Readiness and Liveness**

- **Liveness**: Should the container be restarted? (usually no checks or very basic)
- **Readiness**: Can the container receive traffic? (check dependencies)

**Example**:
```csharp
// Liveness: Always healthy (process is alive)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: Check dependencies
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### 4. **Don't Check External Services**

**Problem**: External services (SSO, MIS, ASM) are outside your control.

**Solution**: Only check services you control:
- ✅ API itself
- ✅ Database connections
- ❌ External third-party services

**Why**: External service failures shouldn't mark your API as unhealthy.

### 5. **Use Enhanced JSON Responses**

Custom response writers provide better observability:

- Individual check statuses
- Execution durations
- Exception information
- Summary statistics

### 6. **Monitor Health Check Endpoints**

Set up monitoring for health check endpoints:

- Alert on unhealthy status
- Track health check duration
- Monitor check failure rates
- Create dashboards

### 7. **Handle Timeouts Gracefully**

Health checks should have reasonable timeouts:

```csharp
services.AddHealthChecks()
    .AddCheck("database", async () =>
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        // Health check logic with timeout
    });
```

### 8. **Don't Expose Sensitive Information**

Health check responses should not include:
- Connection strings
- Passwords
- Internal IP addresses
- Detailed error messages in production

---

## Kubernetes Integration

### Readiness Probe Configuration

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: webshop-api
spec:
  template:
    spec:
      containers:
      - name: api
        image: webshop-api:latest
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
            scheme: HTTPS
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          successThreshold: 1
          failureThreshold: 3
```

**Configuration**:
- `initialDelaySeconds: 10` - Wait 10 seconds before first check
- `periodSeconds: 5` - Check every 5 seconds
- `timeoutSeconds: 3` - Timeout after 3 seconds
- `successThreshold: 1` - One success marks as ready
- `failureThreshold: 3` - Three failures mark as not ready

### Liveness Probe Configuration

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
    scheme: HTTPS
  initialDelaySeconds: 30
  periodSeconds: 10
  timeoutSeconds: 3
  failureThreshold: 3
```

**Configuration**:
- `initialDelaySeconds: 30` - Wait 30 seconds before first check
- `periodSeconds: 10` - Check every 10 seconds
- `failureThreshold: 3` - Three failures trigger container restart

### Startup Probe (Optional)

For applications with slow startup:

```yaml
startupProbe:
  httpGet:
    path: /health/live
    port: 8080
    scheme: HTTPS
  initialDelaySeconds: 0
  periodSeconds: 5
  timeoutSeconds: 3
  failureThreshold: 40  # Allow up to 200 seconds for startup
```

---

## Troubleshooting

### Issue: Health Check Returns Unhealthy

**Symptoms**: `/health` endpoint returns `503 Service Unavailable`.

**Diagnosis**:
1. Check the detailed response: `GET /health/detailed`
2. Look for exception messages in the response
3. Check individual check statuses

**Common Causes**:
- Database connection failure
- Database authentication issues
- Network connectivity problems
- Connection pool exhaustion

**Solutions**:
- Verify database is running and accessible
- Check connection strings in configuration
- Verify network connectivity
- Check connection pool settings

### Issue: Health Check is Slow

**Symptoms**: Health check takes > 1 second to respond.

**Diagnosis**:
- Check `totalDuration` in response
- Check individual check durations
- Look for slow database queries

**Solutions**:
- Optimize database queries
- Use connection pooling
- Add timeouts to health checks
- Consider caching health check results (with caution)

### Issue: Readiness Probe Fails

**Symptoms**: Kubernetes readiness probe fails, pods not receiving traffic.

**Diagnosis**:
- Check `/health/ready` endpoint directly
- Verify checks are tagged with `"ready"`
- Check if dependencies are available

**Solutions**:
- Ensure dependencies are healthy
- Tag appropriate checks with `"ready"`
- Adjust readiness probe configuration

### Issue: Liveness Probe Fails

**Symptoms**: Kubernetes restarts containers repeatedly.

**Diagnosis**:
- Check `/health/live` endpoint
- Verify endpoint is configured correctly
- Check if application is actually running

**Solutions**:
- Ensure `/health/live` always returns healthy
- Verify endpoint is accessible
- Check application logs for errors

### Issue: Database Health Check Fails

**Symptoms**: Database health check returns unhealthy.

**Diagnosis**:
- Check `/health/db` endpoint
- Verify database connectivity
- Check connection strings

**Common Causes**:
- Database server is down
- Network connectivity issues
- Authentication failures
- Connection pool exhausted

**Solutions**:
- Verify database is running
- Test database connectivity manually
- Check connection strings
- Review connection pool settings
- Check firewall rules

---

## Summary

### Key Takeaways

1. **Health checks are essential** for container orchestration and monitoring
2. **Use tags** to organize and filter health checks
3. **Separate liveness and readiness** probes for Kubernetes
4. **Keep checks fast** (< 1 second)
5. **Only check services you control** (not external services)
6. **Use enhanced JSON responses** for better observability
7. **Monitor health check endpoints** for proactive issue detection

### Current Implementation

The WebShop API includes:

- ✅ Self health check (API is running)
- ✅ Custom Dapper-based database read health check
- ✅ Custom Dapper-based database write health check
- ✅ Enhanced JSON response format
- ✅ Detailed JSON response format
- ✅ Multiple endpoints for different use cases
- ✅ Tag-based filtering
- ✅ Kubernetes-ready (liveness and readiness probes)

### Future Enhancements

Potential improvements:

- Add custom health checks for critical services
- Implement health check result caching (with TTL)
- Add health check metrics export (Prometheus)
- Create health check dashboard
- Add health check history/trending

---

## Related Documentation

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Kubernetes Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
- [Health Check Best Practices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/monitor-app-health)

---

## Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-12-27 | 1.0 | Initial health checks implementation guide | System |

