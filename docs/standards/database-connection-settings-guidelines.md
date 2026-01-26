# Database Connection Settings Guidelines

**Database:** PostgreSQL

This document provides comprehensive guidelines for configuring and optimizing database connection settings in the WebShop API application.

[← Back to README](../../README.md)

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Configuration Settings](#configuration-settings)
- [Environment-Specific Recommendations](#environment-specific-recommendations)
- [Performance Tuning](#performance-tuning)
- [Security Best Practices](#security-best-practices)
- [Troubleshooting](#troubleshooting)
- [Monitoring](#monitoring)
- [Best Practices Summary](#best-practices-summary)

---

## Overview

The WebShop API uses a **dual-connection architecture** with separate read and write database connections. This design pattern provides:

- **Read/Write Separation**: Optimize read operations with read replicas
- **Performance**: Distribute load across multiple database connections
- **Scalability**: Scale read and write operations independently
- **Resilience**: Isolate read and write failures

### Connection Pooling

The application uses **Npgsql connection pooling** combined with **Dapper Dapper connection pooling**:

- **Npgsql Pool**: Managed by Npgsql driver (configured via connection string)
- **Dapper connection Pool**: Managed by Dapper (configured in `DependencyInjection.cs` with `poolSize: 1024`)

**Important**: The total number of database connections = `MaxPoolSize` × `Dapper connection Pool Size`. For example:

- `MaxPoolSize: 100` × `Dapper connection Pool: 1024` = Up to 100 actual database connections (pooled across 1024 Dapper connection instances)

---

## Architecture

### Connection Model Structure

```json
{
  "DbConnectionSettings": {
    "Read": { /* Read-only connection settings */ },
    "Write": { /* Write connection settings */ }
  }
}
```

### How Connections Are Used

1. **Read Connections** (`IDapperConnectionFactory`):
   - Used for all SELECT queries
   - Optimized for read operations with connection pooling
   - Can point to read replicas for load distribution

2. **Write Connections** (`IDapperConnectionFactory`):
   - Used for INSERT, UPDATE, DELETE operations
   - Supports transactions via `IDapperTransactionManager`
   - Always points to the primary database

### Connection String Generation

Connection strings are built from configuration using `DbConnectionModel.CreateConnectionString()`, which:

- Applies secure defaults if settings are missing
- Validates SSL mode
- Combines all settings into a PostgreSQL connection string

### Connection Validation on Startup

The application validates both read and write database connections on startup using `DatabaseConnectionValidationFilter`:

- **Extension Method**: `ValidateDatabaseConnections()` validates connections before application starts
- **Fail-Fast Pattern**: Application won't start if connections are invalid
- **Independent of Migrations**: Validates connections even when migrations are disabled
- **Clear Error Messages**: Provides specific error messages for read vs. write connection failures

**Execution Order:**

1. Connection validation (validates both read and write)
2. Database migrations (if enabled)

This ensures database connectivity is verified before attempting any database operations.

---

## Configuration Settings

### Code Defaults vs Configuration Overrides

**Important**: All optional database connection settings have **secure, production-ready defaults** defined in `src/WebShop.Util/Models/DbConnectionModel.cs`.

**You do NOT need to include optional settings in your `appsettings.json` unless you want to override the defaults.**

**Configuration Philosophy**:

- ✅ **Minimal Configuration**: Include only required fields (Host, Port, DatabaseName, UserId, Password)
- ✅ **Override When Needed**: Add optional settings only when you have specific requirements
- ✅ **Environment-Specific**: Use environment-specific config files to override defaults per environment
- ✅ **Validate with Testing**: Always validate overrides with load testing before deploying

### Quick Reference: Code Defaults

All these values are automatically applied if not specified in configuration:

| Setting                   | Code Default                                                 | When to Override                                                       |
|---------------------------|--------------------------------------------------------------|------------------------------------------------------------------------|
| `SslMode`                 | `Require`                                                    | Development (use `Prefer`), Production (use `VerifyFull`)             |
| `MaxPoolSize`             | `100`                                                        | High-traffic scenarios (increase), Development (decrease to 20)       |
| `MinPoolSize`             | `5`                                                          | Development (decrease to 2), High-traffic (increase to 10)            |
| `ConnectionIdleLifetime`  | `300` seconds (5 min)                                        | Rarely needed                                                          |
| `CommandTimeout`          | `30` seconds                                                 | Complex queries (increase to 60-120s)                                  |
| `Timeout`                 | `15` seconds                                                 | Cross-region connections (increase to 30s)                             |
| `ConnectionLifetime`      | `0` (unlimited)                                              | Load balancer scenarios (set to 3600)                                  |
| `ApplicationName`         | From `AppSettings.ApplicationName` or `"WebShop.Api"`        | Per-connection monitoring (override per connection)                    |
| `MaxAutoPrepare`          | `10`                                                         | High-performance apps (increase to 20-50)                              |
| `AutoPrepareMinUsages`    | `2`                                                          | One-off queries (increase to 3-5)                                      |

### Required Settings

These settings **must** be configured in all environments:

```json
{
  "DbConnectionSettings": {
    "Read": {
      "Host": "database.example.com",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "webshop_user",
      "Password": "secure-password"
    },
    "Write": {
      "Host": "database.example.com",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "webshop_user",
      "Password": "secure-password"
    }
  }
}
```

### Optional Settings (Code Defaults)

All optional settings have **secure, production-ready defaults defined in the code** (`DbConnectionModel.cs`).

**You do NOT need to include these in your `appsettings.json` unless you want to override the defaults.**

The following sections document what each setting does and when you might want to override the default values.

#### 1. **SslMode** (Security)

**Code Default**: `Require` *(automatically applied if not specified)*

**Purpose**: Controls SSL/TLS encryption for database connections.

**Options**:

- `Disable` - No SSL (❌ **NEVER use in production**)
- `Allow` - Try SSL, fallback to non-SSL (⚠️ **Not recommended**)
- `Prefer` - Prefer SSL, fallback to non-SSL (✅ **Development only**)
- `Require` - Require SSL (✅ **Production default**)
- `VerifyCA` - Require SSL + verify certificate authority
- `VerifyFull` - Require SSL + verify CA + verify hostname (✅ **Most secure**)

**Recommendations** (Override only if needed):

- **Development**: `Prefer` (allows local connections without SSL)
- **Production**: `VerifyFull` (maximum security)

**Example**:

```json
{
  "SslMode": "VerifyFull"
}
```

**Why It Matters**:

- Without SSL, database credentials and data are transmitted in **plain text**
- Vulnerable to man-in-the-middle attacks
- Required for compliance (PCI-DSS, GDPR, HIPAA)

---

#### 2. **MaxPoolSize** (Performance)

**Code Default**: `100` *(automatically applied if not specified)*

**Purpose**: Maximum number of database connections in the connection pool.

**Recommendations** (Override only if needed):

- **Development**: `20` (lower resource usage)
- **Small Production**: `100` (suitable for most applications)
- **High-Traffic Production**: `200-500` (based on load testing)

**Calculation Formula**:

```text
MaxPoolSize = (Expected Concurrent Requests × Avg Query Duration) / Target Response Time
```

**Example**:

- 1000 concurrent requests
- Average query: 50ms
- Target response: 100ms
- **MaxPoolSize** = (1000 × 0.05) / 0.1 = **500 connections**

**Why It Matters**:

- Too low: Connection pool exhaustion → requests queued → slow responses
- Too high: Database overload → resource exhaustion → degraded performance
- Each connection consumes ~2-10 MB of database memory

**Monitoring**:

```sql
-- Check active connections
SELECT count(*) FROM pg_stat_activity WHERE application_name = 'webshop-api';

-- Check connection pool usage
SELECT 
    application_name,
    count(*) as connections,
    state,
    wait_event_type
FROM pg_stat_activity 
WHERE application_name LIKE 'webshop-api%'
GROUP BY application_name, state, wait_event_type;
```

---

#### 3. **MinPoolSize** (Performance)

**Code Default**: `5` *(automatically applied if not specified)*

**Purpose**: Minimum number of connections to maintain in the pool (warm connections).

**Recommendations** (Override only if needed):

- **Development**: `2` (lower resource usage)
- **Production**: `5-10` (faster response for first requests after idle period)

**Why It Matters**:

- Maintains "warm" connections ready for immediate use
- Reduces connection establishment overhead (~100-300ms) for first requests
- Prevents cold start delays after idle periods

**Trade-off**:

- Higher `MinPoolSize` = Faster response but more idle connections
- Lower `MinPoolSize` = Less resource usage but slower first requests

---

#### 4. **ConnectionIdleLifetime** (Resource Management)

**Code Default**: `300` seconds (5 minutes) *(automatically applied if not specified)*

**Purpose**: Time (in seconds) to keep idle connections alive before closing them.

**Recommendations** (Override only if needed):

- **Development**: `300` (5 minutes)
- **Production**: `300-600` (5-10 minutes)

**Why It Matters**:

- Prevents connection leaks from accumulating
- Frees resources when connections are truly idle
- Balances between connection reuse and resource cleanup

**Example**:

```json
{
  "ConnectionIdleLifetime": 600  // 10 minutes
}
```

---

#### 5. **CommandTimeout** (Reliability)

**Code Default**: `30` seconds *(automatically applied if not specified)*

**Purpose**: Maximum time (in seconds) a database command can run before timing out.

**Recommendations** (Override only if needed):

- **Fast Queries**: `30` seconds (default)
- **Complex Reports**: `60-120` seconds
- **Batch Operations**: `300-600` seconds (5-10 minutes)

**Why It Matters**:

- Prevents long-running queries from blocking connections indefinitely
- Without timeout: A slow query can hold a connection for hours
- Causes: Pool exhaustion, application hangs, degraded performance

**Example**:

```json
{
  "CommandTimeout": 60  // 60 seconds for complex queries
}
```

**Best Practice**: Set timeout based on your longest expected query duration + 20% buffer.

---

#### 6. **Timeout** (Reliability)

**Code Default**: `15` seconds *(automatically applied if not specified)*

**Purpose**: Maximum time (in seconds) to wait when establishing a database connection.

**Recommendations** (Override only if needed):

- **Local/Development**: `15` seconds (default)
- **Production (Same Region)**: `15` seconds
- **Production (Cross-Region)**: `30` seconds

**Why It Matters**:

- Prevents application from hanging if database is unreachable
- Fails fast instead of waiting indefinitely
- Enables proper error handling and retry logic

**Example**:

```json
{
  "Timeout": 30  // 30 seconds for cross-region connections
}
```

---

#### 7. **ConnectionLifetime** (Performance)

**Code Default**: `0` (unlimited - connections recycled by pool) *(automatically applied if not specified)*

**Purpose**: Maximum lifetime (in seconds) of a connection before it's recycled.

**Recommendations** (Override only if needed):

- **Most Cases**: `0` (unlimited - let pool manage)
- **Load Balancer Scenarios**: `3600` (1 hour) to distribute connections

**Why It Matters**:

- `0` = Pool manages connection lifecycle efficiently
- Non-zero = Helps with load balancer connection distribution
- Prevents connection staleness issues

**Example**:

```json
{
  "ConnectionLifetime": 0  // Unlimited (recommended)
}
```

---

#### 8. **ApplicationName** (Monitoring)

**Purpose**: Identifies the application in PostgreSQL monitoring views.

**Configuration**: Now centrally configured in `AppSettings.ApplicationName` (single source of truth)

**Default**: `"WebShop.Api"` (fallback if not configured)

**Priority System**:

1. **Connection-specific ApplicationName** (if explicitly set in `DbConnectionSettings.Read/Write`)
2. **Global AppSettings.ApplicationName** (recommended - single configuration)
3. **Default "WebShop.Api"** (fallback)

**Recommendations**:

- **Standard**: Set `AppSettings.ApplicationName` to identify your application (e.g., `"webshop-api"`)
- **Advanced**: Optionally override per-connection for read/write distinction (e.g., `"webshop-api-read"`, `"webshop-api-write"`)

**Why It Matters**:

- Appears in `pg_stat_activity` view
- Critical for monitoring and debugging
- Identifies which application is consuming database resources
- Helps distinguish between different application instances and environments

**Example** (Recommended - Global Configuration):

```json
{
  "AppSettings": {
    "ApplicationName": "webshop-api"  // ✅ Single source of truth
  },
  "DbConnectionSettings": {
    "Read": {
      // Will use "webshop-api" from AppSettings
    },
    "Write": {
      // Will use "webshop-api" from AppSettings
    }
  }
}
```

**Example** (Advanced - Per-Connection Override):

```json
{
  "AppSettings": {
    "ApplicationName": "webshop-api"  // Default for all connections
  },
  "DbConnectionSettings": {
    "Read": {
      "ApplicationName": "webshop-api-read"  // ✅ Override for read connections
    },
    "Write": {
      "ApplicationName": "webshop-api-write"  // ✅ Override for write connections
    }
  }
}
```

**Monitoring Query**:

```sql
-- View connections by application
SELECT 
    application_name,
    count(*) as connection_count,
    state,
    sum(EXTRACT(EPOCH FROM (now() - state_change))) as total_seconds_idle
FROM pg_stat_activity
WHERE application_name LIKE 'webshop-api%'
GROUP BY application_name, state
ORDER BY application_name, state;
```

---

#### 9. **MaxAutoPrepare** (Performance)

**Code Default**: `10` *(automatically applied if not specified)*

**Purpose**: Maximum number of prepared statements to cache per connection.

**Recommendations** (Override only if needed):

- **Most Applications**: `10` (default)
- **High-Performance**: `20-50` (if you have many frequently-used queries)

**Why It Matters**:

- Prepared statements cache query plans for faster execution
- Reduces query parsing overhead
- Protects against SQL injection attacks
- Higher values = Better performance but more memory usage

**Example**:

```json
{
  "MaxAutoPrepare": 20  // Cache more query plans
}
```

---

#### 10. **AutoPrepareMinUsages** (Performance)

**Code Default**: `2` *(automatically applied if not specified)*

**Purpose**: Minimum number of times a statement must be used before auto-preparing.

**Recommendations** (Override only if needed):

- **Most Applications**: `2` (default)
- **One-Off Queries**: `3-5` (avoid preparing rarely-used queries)

**Why It Matters**:

- Ensures only frequently-used queries are prepared
- Avoids overhead for one-off queries
- Balances between performance and memory usage

**Example**:

```json
{
  "AutoPrepareMinUsages": 3  // Only prepare queries used 3+ times
}
```

---

## Environment-Specific Recommendations

### Development Environment

**File**: `appsettings.Development.json`

```json
{
  "AppSettings": {
    "ApplicationName": "webshop-api-dev"
  },
  "DbConnectionSettings": {
    "Read": {
      "Host": "localhost",
      "Port": "5432",
      "DatabaseName": "webshopdb_dev",
      "UserId": "postgres",
      "Password": "dev-password"
    },
    "Write": {
      "Host": "localhost",
      "Port": "5432",
      "DatabaseName": "webshopdb_dev",
      "UserId": "postgres",
      "Password": "dev-password"
    }
  }
}
```

**Common Overrides for Development:**
- `SslMode: "Prefer"` - Allow local connections without SSL
- `MaxPoolSize: 20` - Lower resource usage
- `MinPoolSize: 2` - Fewer warm connections

---

### Production Environment

**File**: `appsettings.Production.json`

```json
{
  "AppSettings": {
    "ApplicationName": "webshop-api-production"
  },
  "DbConnectionSettings": {
    "Read": {
      "Host": "read-replica.example.com",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "webshop_read_user",
      "Password": "${DB_READ_PASSWORD}"
    },
    "Write": {
      "Host": "primary-db.example.com",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "webshop_write_user",
      "Password": "${DB_WRITE_PASSWORD}"
    }
  }
}
```

**Common Overrides for High-Traffic Production:**
- Read: `SslMode: "VerifyFull"`, `MaxPoolSize: 200`, `MinPoolSize: 10`
- Write: `SslMode: "VerifyFull"`, `MaxPoolSize: 100`, `MinPoolSize: 10`
- Both: `ConnectionIdleLifetime: 600`, `CommandTimeout: 60`, `Timeout: 30`
- Advanced: Per-connection `ApplicationName` for separate read/write monitoring

---

### Staging/QA Environment

**File**: `appsettings.Staging.json`

```json
{
  "AppSettings": {
    "ApplicationName": "webshop-api-staging"
  },
  "DbConnectionSettings": {
    "Read": {
      "Host": "staging-db.example.com",
      "Port": "5432",
      "DatabaseName": "webshopdb_staging",
      "UserId": "webshop_staging_user",
      "Password": "${DB_STAGING_PASSWORD}"
    },
    "Write": {
      "Host": "staging-db.example.com",
      "Port": "5432",
      "DatabaseName": "webshopdb_staging",
      "UserId": "webshop_staging_user",
      "Password": "${DB_STAGING_PASSWORD}"
    }
  }
}
```

**Common Overrides for Load Testing:**
- `MaxPoolSize: 50`, `MinPoolSize: 5` - Lower than production for cost savings

---

## Performance Tuning

### Step 1: Baseline Measurement

Before tuning, establish baseline metrics:

```sql
-- Connection pool usage
SELECT 
    application_name,
    count(*) as total_connections,
    count(*) FILTER (WHERE state = 'active') as active_connections,
    count(*) FILTER (WHERE state = 'idle') as idle_connections,
    count(*) FILTER (WHERE state = 'idle in transaction') as idle_in_transaction
FROM pg_stat_activity
WHERE application_name LIKE 'webshop-api%'
GROUP BY application_name;

-- Query performance
SELECT 
    query,
    calls,
    total_exec_time,
    mean_exec_time,
    max_exec_time
FROM pg_stat_statements
WHERE query LIKE '%webshop%'
ORDER BY total_exec_time DESC
LIMIT 20;
```

### Step 2: Identify Bottlenecks

**Symptoms of Connection Pool Exhaustion**:

- High number of `idle in transaction` connections
- Slow response times
- Timeout errors
- Connection wait events in PostgreSQL

**Symptoms of Too Many Connections**:

- High database CPU/memory usage
- Database connection limit warnings
- Degraded database performance

### Step 3: Tune Settings

**If experiencing connection pool exhaustion**:

1. Increase `MaxPoolSize` (but monitor database resources)
2. Reduce `ConnectionIdleLifetime` to free connections faster
3. Reduce `CommandTimeout` to fail queries faster
4. Optimize slow queries

**If experiencing too many connections**:

1. Decrease `MaxPoolSize`
2. Increase `ConnectionIdleLifetime` to keep connections longer
3. Review if read replicas are needed

**If experiencing slow first requests**:

1. Increase `MinPoolSize` to maintain more warm connections

### Step 4: Load Testing

After tuning, perform load testing:

```bash
# Example: Use Apache Bench or similar
ab -n 10000 -c 100 https://api.example.com/api/v1/products
```

Monitor:

- Response times
- Connection pool usage
- Database CPU/memory
- Error rates

---

## Security Best Practices

### 1. **Never Store Passwords in Configuration Files**

❌ **Bad**:

```json
{
  "Password": "MySecretPassword123"
}
```

✅ **Good**:

```json
{
  "Password": "${DB_PASSWORD}"  // Environment variable
}
```

Or use Azure Key Vault, AWS Secrets Manager, or similar.

### 2. **Use Strong SSL Modes in Production**

❌ **Bad**:

```json
{
  "SslMode": "Disable"  // NEVER in production
}
```

✅ **Good**:

```json
{
  "SslMode": "VerifyFull"  // Maximum security
}
```

### 3. **Use Separate Database Users**

- **Read User**: `SELECT` permissions only
- **Write User**: `INSERT`, `UPDATE`, `DELETE` permissions

### 4. **Rotate Passwords Regularly**

- Change database passwords every 90 days
- Use environment variables or secret management
- Update all environments simultaneously

### 5. **Monitor Connection Activity**

Regularly review connection logs for suspicious activity:

```sql
-- Recent connection activity
SELECT 
    application_name,
    usename,
    client_addr,
    state,
    query_start,
    state_change
FROM pg_stat_activity
WHERE application_name LIKE 'webshop-api%'
ORDER BY state_change DESC;
```

---

## Troubleshooting

### Issue: "Connection pool exhausted"

**Symptoms**:

- `NpgsqlException: The connection pool has been exhausted`
- Slow response times
- Timeout errors

**Solutions**:

1. Increase `MaxPoolSize`
2. Check for connection leaks (queries holding connections too long)
3. Reduce `CommandTimeout` to fail slow queries faster
4. Optimize slow queries
5. Add read replicas for read operations

**Diagnosis**:

```sql
-- Check for long-running queries
SELECT 
    pid,
    application_name,
    state,
    query_start,
    now() - query_start as duration,
    query
FROM pg_stat_activity
WHERE application_name LIKE 'webshop-api%'
  AND state != 'idle'
ORDER BY query_start;
```

---

### Issue: "Too many database connections"

**Symptoms**:

- Database connection limit warnings
- Degraded database performance
- High database memory usage

**Solutions**:

1. Decrease `MaxPoolSize`
2. Increase `ConnectionIdleLifetime` to keep connections longer
3. Review if all connections are necessary
4. Consider connection pooling at database level (PgBouncer)

**Diagnosis**:

```sql
-- Total connections by application
SELECT 
    application_name,
    count(*) as connections
FROM pg_stat_activity
GROUP BY application_name
ORDER BY count(*) DESC;
```

---

### Issue: "Slow first request after idle period"

**Symptoms**:

- First request after idle period is slow (~100-300ms)
- Subsequent requests are fast

**Solutions**:

1. Increase `MinPoolSize` to maintain warm connections
2. Use connection keep-alive

---

### Issue: "SSL connection errors"

**Symptoms**:

- `NpgsqlException: SSL connection required`
- Connection failures in production

**Solutions**:

1. Verify database SSL is enabled
2. Use `SslMode: Prefer` for development (if SSL not available)
3. Use `SslMode: Require` or `VerifyFull` for production
4. Check certificate validity

---

### Issue: "Query timeout errors"

**Symptoms**:

- `NpgsqlException: Command timeout expired`
- Queries failing after 30 seconds

**Solutions**:

1. Increase `CommandTimeout` if queries legitimately need more time
2. Optimize slow queries
3. Add database indexes
4. Consider query pagination for large result sets

---

## Monitoring

### Key Metrics to Monitor

1. **Connection Pool Usage**
   - Active connections
   - Idle connections
   - Pool exhaustion events

2. **Query Performance**
   - Average query duration
   - Slow query count
   - Timeout frequency

3. **Database Health**
   - Connection count
   - Database CPU/memory
   - Lock contention

### Monitoring Queries

**Connection Pool Health**:

```sql
SELECT 
    application_name,
    count(*) as total,
    count(*) FILTER (WHERE state = 'active') as active,
    count(*) FILTER (WHERE state = 'idle') as idle,
    count(*) FILTER (WHERE state = 'idle in transaction') as idle_in_transaction,
    max(EXTRACT(EPOCH FROM (now() - state_change))) as max_idle_seconds
FROM pg_stat_activity
WHERE application_name LIKE 'webshop-api%'
GROUP BY application_name;
```

**Slow Queries**:

```sql
SELECT 
    application_name,
    query,
    calls,
    mean_exec_time,
    max_exec_time,
    total_exec_time
FROM pg_stat_statements
WHERE application_name LIKE 'webshop-api%'
  AND mean_exec_time > 1000  -- Queries taking > 1 second on average
ORDER BY mean_exec_time DESC
LIMIT 20;
```

**Connection Wait Events**:

```sql
SELECT 
    application_name,
    wait_event_type,
    wait_event,
    count(*) as connection_count
FROM pg_stat_activity
WHERE application_name LIKE 'webshop-api%'
  AND wait_event IS NOT NULL
GROUP BY application_name, wait_event_type, wait_event
ORDER BY count(*) DESC;
```

---

## Best Practices Summary

### ✅ Do's

1. **Always use SSL in production** (`SslMode: VerifyFull`)
2. **Store passwords in environment variables or secret management**
3. **Use separate read/write connections** for better performance
4. **Set appropriate `ApplicationName`** for monitoring
5. **Tune `MaxPoolSize` based on load testing**
6. **Monitor connection pool usage regularly**
7. **Set `CommandTimeout` based on longest expected query**
8. **Use read replicas for read operations** in high-traffic scenarios
9. **Configure different settings per environment**
10. **Review and optimize slow queries regularly**
11. **Validate connections on startup** - The application automatically validates both read and write connections before starting (via `DatabaseConnectionValidationFilter`)

### ❌ Don'ts

1. **Never disable SSL in production**
2. **Never hardcode passwords in configuration files**
3. **Don't set `MaxPoolSize` too high** (causes database overload)
4. **Don't set `CommandTimeout` too high** (masks performance issues)
5. **Don't ignore connection pool exhaustion warnings**
6. **Don't use same connection settings for all environments**
7. **Don't skip monitoring connection metrics**
8. **Don't set `MinPoolSize` higher than `MaxPoolSize`**

---

## Additional Resources

- [Npgsql Connection String Parameters](https://www.npgsql.org/doc/connection-string-parameters.html)
- [PostgreSQL Connection Pooling Best Practices](https://www.postgresql.org/docs/current/runtime-config-connection.html)
- [Dapper Performance](https://learn.microsoft.com/en-us/ef/core/performance/)
- [PostgreSQL Monitoring Queries](https://www.postgresql.org/docs/current/monitoring-stats.html)

---

**Note**: Always test configuration changes in a staging environment before applying to production. Monitor metrics after changes to ensure optimal performance.
