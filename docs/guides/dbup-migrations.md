# DbUp Database Migrations Implementation Guide

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Why DbUp?](#why-dbup)
- [What Problem It Solves](#what-problem-it-solves)
- [How It Works](#how-it-works)
- [Architecture & Design](#architecture--design)
- [Benefits](#benefits)
- [Implementation Details](#implementation-details)
- [Configuration](#configuration)
- [Creating Migrations](#creating-migrations)
- [Creating Seed Scripts](#creating-seed-scripts)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## Overview

The WebShop API uses **DbUp** for SQL-based database migrations and seed data management. DbUp provides a simple, reliable way to version and deploy database changes using plain SQL scripts, with automatic execution on application startup and built-in safety mechanisms for concurrent deployments.

## Why DbUp?

### The Problem with Dapper Migrations

Dapper migrations have limitations:

1. **Limited SQL Control**: Dapper generates SQL, which may not be optimal for complex scenarios
2. **Database-Specific Features**: Difficult to use PostgreSQL-specific features (e.g., ENUMs, custom functions, triggers)
3. **Migration Complexity**: Complex migrations can be hard to express through Dapper model changes
4. **Version Control**: Migration history is stored in code, making it harder to review SQL changes
5. **Rollback Complexity**: Rolling back Dapper migrations can be challenging
6. **Team Collaboration**: Multiple developers can generate conflicting migration numbers

### The Solution: DbUp

DbUp provides:

- **Full SQL Control**: Write raw SQL for maximum flexibility
- **Database-Specific Features**: Use any PostgreSQL feature directly
- **Simple Versioning**: Timestamp-based script naming
- **Automatic Execution**: Runs on application startup
- **Concurrent Safety**: PostgreSQL advisory locks prevent race conditions
- **Idempotency Support**: Scripts can be written to be safely re-runnable
- **Seed Data Management**: Separate seed scripts for different environments

## What Problem It Solves

### 1. **Database Schema Versioning**

**Problem:** Tracking database schema changes across deployments and environments.

**Solution:** DbUp tracks executed scripts in a `schemaversions` table, ensuring each migration runs only once.

### 2. **Concurrent Deployment Safety**

**Problem:** Multiple application instances deploying simultaneously could run migrations concurrently, causing conflicts.

**Solution:** PostgreSQL advisory locks ensure only one process executes migrations at a time.

### 3. **Automatic Migration Execution**

**Problem:** Manual migration execution is error-prone and can be forgotten.

**Solution:** Migrations run automatically on application startup via `IStartupFilter`.

### 4. **Environment-Specific Seed Data**

**Problem:** Different environments need different seed data (development vs. production).

**Solution:** Separate seed script folders for each environment (Dev, QA, UAT, Production).

### 5. **SQL Review and Control**

**Problem:** Dapper migrations generate SQL that's hard to review and optimize.

**Solution:** DbUp uses plain SQL files that can be reviewed, tested, and optimized before deployment.

## How It Works

### Execution Flow

```
1. Application starts
   ↓
2. DatabaseConnectionValidationFilter.Configure() is called (IStartupFilter)
   - Validates both read and write database connections
   - Fails fast if connections are invalid
   - Uses extension method: ValidateDatabaseConnections()
   ↓
3. DatabaseMigrationInitFilter.Configure() is called (IStartupFilter)
   ↓
4. Check EnableDatabaseMigration setting
   ↓
3a. If disabled:
    - Skip migration
    - Continue to next startup filter
   ↓
3b. If enabled:
    - Load database connection string
    ↓
4. Ensure database exists (creates if missing)
   ↓
5. Acquire PostgreSQL advisory lock
   ↓
5a. If lock acquired:
    - Check for pending migrations
    ↓
    5a1. If no pending migrations:
         - Run seed scripts (if needed)
         - Release lock
         - Continue startup
    ↓
    5a2. If pending migrations:
         - Execute migration scripts in order
         - If successful:
             - Run seed scripts
             - Release lock
             - Continue startup
         - If failed:
             - Log error
             - Release lock
             - Terminate application (Environment.Exit(1))
   ↓
5b. If lock not acquired:
    - Wait up to 60 seconds
    - Retry every 5 seconds
    - If timeout:
        - Log warning
        - Continue startup (assumes another instance is migrating)
```

### Migration Script Execution

1. **Script Discovery**: DbUp scans `DbUpMigration/Migrations/` folder for `.sql` files
2. **Script Ordering**: Scripts are executed in alphabetical order (timestamp prefix ensures correct order)
3. **Version Tracking**: Each executed script is recorded in `schemaversions` table
4. **Transaction Safety**: All scripts run in a transaction (rollback on failure)
5. **Idempotency**: Scripts use `IF NOT EXISTS` patterns to be safely re-runnable

### Seed Script Execution

1. **Environment Detection**: Reads `AppSettings:Environment` (Dev, QA, UAT, Production)
2. **Script Discovery**: Scans `DbUpMigration/Seeds/{Environment}/` folder for `.sql` files
3. **Execution**: Runs seed scripts after successful migrations
4. **Idempotency**: Seed scripts use table-level and row-level idempotency patterns

### Advisory Lock Mechanism

PostgreSQL advisory locks prevent concurrent migration execution:

```sql
-- Acquire lock
SELECT pg_try_advisory_lock(3565012658280623778);

-- Execute migrations...

-- Release lock
SELECT pg_advisory_unlock(3565012658280623778);
```

**Benefits:**

- Only one process can migrate at a time
- Other processes wait (up to 60 seconds) or continue if lock is held
- Prevents database corruption from concurrent migrations

## Architecture & Design

### Startup Filter Pattern

DbUp migrations run via `IStartupFilter`, which executes before the application starts serving requests:

```csharp
public class DatabaseMigrationInitFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        // Run migrations
        // Then call next() to continue startup
        return next;
    }
}
```

**Why Startup Filter?**

- Executes before application starts
- Can terminate application if migration fails
- Ensures database is ready before serving requests

**Startup Filter Execution Order:**

1. `DatabaseConnectionValidationFilter` - Validates read and write connections (fail-fast)
2. `DatabaseMigrationInitFilter` - Runs migrations (only if connections are valid)

**Note:** Connection validation runs before migrations to ensure database connectivity before attempting migrations. This prevents migration failures due to connection issues.

### Registration

Registered in `Core/ServiceExtensions.cs`:

```csharp
// Startup filters execute in reverse order of registration
// Register migrations first, then validation, so validation executes first
services.AddTransient<IStartupFilter, DatabaseMigrationInitFilter>();
services.AddTransient<IStartupFilter, DatabaseConnectionValidationFilter>();
```

### Folder Structure

```
src/WebShop.Api/DbUpMigration/
├── Migrations/
│   └── 20250101-000000-Initial-Schema.sql
└── Seeds/
    ├── Dev/
    │   └── 20250101-000001-Sample-Data.sql
    ├── QA/
    ├── UAT/
    └── Production/
```

**Naming Convention:**

- **Migrations**: `YYYYMMDD-HHMMSS-Description.sql`
- **Seeds**: `YYYYMMDD-HHMMSS-Description.sql`

### Project Configuration

SQL files are embedded as resources and copied to output:

```xml
<ItemGroup>
  <EmbeddedResource Include="DbUpMigration\**\*.sql" />
  <Content Include="DbUpMigration\**\*.sql">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

## Benefits

### 1. **Full SQL Control**

- Write optimized SQL for your specific use case
- Use PostgreSQL-specific features (ENUMs, functions, triggers, etc.)
- Complex migrations are easier to express

### 2. **Version Control Friendly**

- SQL files are plain text, easy to review in pull requests
- Changes are visible in git diffs
- Team can review SQL before deployment

### 3. **Automatic Execution**

- Migrations run automatically on startup
- No manual intervention required
- Consistent across all environments

### 4. **Concurrent Safety**

- Advisory locks prevent race conditions
- Multiple instances can deploy safely
- Kubernetes-friendly (handles pod restarts)

### 5. **Environment-Specific Seeds**

- Different seed data per environment
- Development gets sample data
- Production gets minimal/essential data only

### 6. **Idempotency Support**

- Scripts can be safely re-run
- Useful for fixing failed migrations
- Handles partial execution scenarios

### 7. **Failure Handling**

- Failed migrations terminate the application
- Prevents serving requests with invalid schema
- Kubernetes will restart and retry

## Implementation Details

### DatabaseMigrationInitFilter

The main filter that orchestrates migrations:

```csharp
public class DatabaseMigrationInitFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        if (_appSettingModel.CurrentValue.EnableDatabaseMigration)
        {
            // Ensure database exists
            EnsureDatabase.For.PostgresqlDatabase(dbConnectionString, dbUpLogger);
            
            // Acquire advisory lock
            // Execute migrations
            // Run seed scripts
        }
        return next;
    }
}
```

### Migration Engine Configuration

```csharp
UpgradeEngine migrationUpgrader = DeployChanges.To
    .PostgresqlDatabase(dbConnectionString)
    .WithScriptsFromFileSystem(migrationPath)
    .WithTransaction()                    // All scripts in one transaction
    .WithExecutionTimeout(TimeSpan.FromMinutes(10))
    .LogTo(dbUpLogger)
    .Build();
```

**Key Features:**

- **WithTransaction()**: All scripts run in a transaction (rollback on failure)
- **WithExecutionTimeout()**: Prevents hanging migrations
- **WithScriptsFromFileSystem()**: Loads scripts from folder

### Seed Engine Configuration

```csharp
UpgradeEngine seedUpgrader = DeployChanges.To
    .PostgresqlDatabase(dbConnectionString)
    .WithScriptsFromFileSystem(seedPath)
    .WithTransaction()
    .WithExecutionTimeout(TimeSpan.FromMinutes(10))
    .LogTo(dbUpLogger)
    .WithVariablesDisabled()  // Important: Disables variable substitution
    .Build();
```

**Key Difference:**

- **WithVariablesDisabled()**: Prevents DbUp from interpreting PostgreSQL dollar-quoting (`$tag$`) as variables

### Advisory Lock Implementation

```csharp
// Try to acquire lock
using NpgsqlCommand lockCommand = new(
    $"SELECT pg_try_advisory_lock({advisoryLockKey});", 
    dbLockConnection);
bool result = lockCommand.ExecuteScalar() as bool? ?? false;

if (result)
{
    // Lock acquired - execute migrations
}
else
{
    // Lock not acquired - wait and retry
    Thread.Sleep(5000);
}
```

**Lock Key:** Configured in `appsettings.json` as `PostgresqlAdvisoryLockKey`

### DbUpLoggerExtension

Integrates DbUp logging with ASP.NET Core ILogger:

```csharp
public class DbUpLoggerExtension : IUpgradeLog
{
    public void LogInformation(string format, params object[] args)
    {
        _logger.LogInformation(string.Format(format, args));
    }
    // ... other log methods
}
```

**Benefits:**

- Unified logging with application logs
- Structured logging support
- Consistent log format

## Configuration

### appsettings.json

```json
{
  "AppSettings": {
    "EnableDatabaseMigration": true,
    "Environment": "Dev",
    "PostgresqlAdvisoryLockKey": 3565012658280623778
  },
  "DbConnectionSettings": {
    "Write": {
      "Host": "localhost",
      "Port": "5432",
      "DatabaseName": "webshopdb",
      "UserId": "postgres",
      "Password": "yourpassword"
    }
  }
}
```

### Configuration Options

- **`EnableDatabaseMigration`**: Enable/disable automatic migrations (default: `true`)
- **`Environment`**: Environment name for seed script selection (Dev, QA, UAT, Production)
- **`PostgresqlAdvisoryLockKey`**: Unique key for advisory lock (prevents conflicts with other applications)

### Disabling Migrations

Set `EnableDatabaseMigration` to `false`:

```json
{
  "AppSettings": {
    "EnableDatabaseMigration": false
  }
}
```

**Use Cases:**

- Manual migration control
- Troubleshooting
- Development scenarios

## Creating Migrations

### Migration File Naming

Use timestamp prefix for ordering:

```
YYYYMMDD-HHMMSS-Description.sql
```

**Examples:**

- `20250101-000000-Initial-Schema.sql`
- `20250115-120000-AddProductTable.sql`
- `20250201-090000-AddIndexes.sql`

### Migration Script Structure

```sql
/*
 * Migration: Add Product Table
 * Description: Creates the product table with all required columns
 * 
 * This migration uses IF NOT EXISTS patterns for idempotency.
 */

-- Set search path
SET search_path TO webshop;

-- Create table if it doesn't exist
CREATE TABLE IF NOT EXISTS webshop.product (
    id SERIAL PRIMARY KEY,
    name VARCHAR(500) NOT NULL,
    category VARCHAR(100),
    created TIMESTAMP NOT NULL DEFAULT NOW(),
    updated TIMESTAMP
);

-- Create index if it doesn't exist
CREATE INDEX IF NOT EXISTS ix_product_category 
ON webshop.product(category);

-- Add comment
COMMENT ON TABLE webshop.product IS 'Product catalog table';
```

### Idempotency Patterns

**1. IF NOT EXISTS (Recommended)**

```sql
CREATE TABLE IF NOT EXISTS webshop.product (...);
CREATE INDEX IF NOT EXISTS ix_product_name ON webshop.product(name);
```

**2. DO Block with Existence Check**

```sql
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'gender') THEN
        CREATE TYPE public.gender AS ENUM ('male', 'female', 'unisex');
    END IF;
END $$;
```

**3. Conditional ALTER TABLE**

```sql
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'webshop' 
        AND table_name = 'product' 
        AND column_name = 'description'
    ) THEN
        ALTER TABLE webshop.product ADD COLUMN description TEXT;
    END IF;
END $$;
```

### Best Practices for Migrations

1. **Always Use IF NOT EXISTS**: Makes scripts idempotent
2. **Test Locally First**: Run migration on local database before committing
3. **Review SQL**: Have team review SQL in pull requests
4. **Keep Migrations Small**: One logical change per migration
5. **Document Complex Logic**: Add comments for non-obvious SQL
6. **Use Transactions**: DbUp handles this automatically with `WithTransaction()`

## Creating Seed Scripts

### Seed File Naming

Same as migrations: `YYYYMMDD-HHMMSS-Description.sql`

**Examples:**

- `20250101-000001-Sample-Data.sql`
- `20250101-000002-Reference-Data.sql`

### Seed Script Structure

Seed scripts should use **dual-level idempotency**:

```sql
/*
 * Development Sample Data Seed Script
 * Description: Loads sample data for development environment
 */

SET search_path TO webshop;

-- Load Products Data
DO $$
BEGIN
    -- TABLE-LEVEL IDEMPOTENCY: Check if data already exists
    IF NOT EXISTS (SELECT 1 FROM webshop.product LIMIT 1) THEN
        RAISE NOTICE 'Loading products data...';
        
        -- ROW-LEVEL IDEMPOTENCY: Use ON CONFLICT DO NOTHING
        INSERT INTO webshop.product (id, name, category, isactive, createdby, created)
        VALUES
            (1, 'T-Shirt', 'Apparel', true, 1, NOW()),
            (2, 'Jeans', 'Apparel', true, 1, NOW())
        ON CONFLICT (id) DO NOTHING;
        
        RAISE NOTICE 'Products data loaded successfully.';
    ELSE
        RAISE NOTICE 'Products data already exists; skipping.';
    END IF;
END $$;
```

### Idempotency Patterns for Seeds

**1. Table-Level Check (Performance Optimization)**

```sql
IF NOT EXISTS (SELECT 1 FROM webshop.product LIMIT 1) THEN
    -- Load all data
END IF;
```

**2. Row-Level Check (Resilience)**

```sql
INSERT INTO webshop.product (id, name, ...)
VALUES (...)
ON CONFLICT (id) DO NOTHING;
```

**Benefits:**

- **Table-level**: Skips entire table if data exists (fast)
- **Row-level**: Handles partial data scenarios (resilient)

### Environment-Specific Seeds

Place seed scripts in environment-specific folders:

- **Dev**: `DbUpMigration/Seeds/Dev/` - Sample/test data
- **QA**: `DbUpMigration/Seeds/QA/` - QA test data
- **UAT**: `DbUpMigration/Seeds/UAT/` - UAT test data
- **Production**: `DbUpMigration/Seeds/Production/` - Essential reference data only

**Important:** Production seed scripts should contain minimal, essential data only (e.g., reference data, lookup tables).

### Dollar-Quoting for Multi-Line Strings

Use PostgreSQL dollar-quoting for complex INSERT statements:

```sql
EXECUTE $qINSERT$INSERT INTO webshop.product (id, name, category) VALUES
(1, 'Product 1', 'Category 1'),
(2, 'Product 2', 'Category 2')
ON CONFLICT (id) DO NOTHING;$qINSERT$;
```

**Why Dollar-Quoting?**

- Handles single quotes in data without escaping
- Supports multi-line strings
- Avoids conflicts with DbUp variable substitution (when `WithVariablesDisabled()` is used)

## Best Practices

### 1. **Migration Naming**

- Use descriptive names: `20250115-120000-AddProductTable.sql`
- Include date and time for ordering
- Keep names concise but clear

### 2. **Idempotency**

- Always use `IF NOT EXISTS` patterns
- Test scripts by running them twice
- Handle partial execution scenarios

### 3. **Transaction Safety**

- DbUp runs all scripts in a transaction
- If any script fails, all changes are rolled back
- Keep migrations atomic (one logical change)

### 4. **Performance**

- Test migrations on large datasets
- Use appropriate indexes
- Consider downtime for large migrations

### 5. **Review Process**

- Review SQL in pull requests
- Test migrations locally first
- Verify on staging before production

### 6. **Seed Data**

- Use table-level checks for performance
- Use row-level checks for resilience
- Keep production seeds minimal
- Document seed data purpose

### 7. **Error Handling**

- Migrations fail fast (application terminates)
- Review logs for migration errors
- Fix and redeploy (migrations are idempotent)

### 8. **Concurrent Deployments**

- Advisory locks handle concurrent deployments
- Multiple instances can deploy safely
- One instance migrates, others wait

## Troubleshooting

### Issue: Migration Not Running

**Symptoms:** Changes not applied to database

**Solutions:**

1. Check `EnableDatabaseMigration` is `true`
2. Verify migration file is in `DbUpMigration/Migrations/` folder
3. Check file naming (must end with `.sql`)
4. Review logs for migration execution
5. Check `schemaversions` table for executed scripts

### Issue: Migration Fails

**Symptoms:** Application terminates on startup, migration error in logs

**Solutions:**

1. Review error message in logs
2. Check SQL syntax
3. Verify database permissions
4. Test migration manually on database
5. Fix migration script and redeploy (idempotent scripts can be re-run)

### Issue: Concurrent Migration Conflicts

**Symptoms:** Multiple instances trying to migrate simultaneously

**Solutions:**

1. Advisory locks should handle this automatically
2. Check lock timeout (60 seconds default)
3. Verify `PostgresqlAdvisoryLockKey` is unique
4. Review logs for lock acquisition messages

### Issue: Seed Scripts Not Running

**Symptoms:** Seed data not loaded

**Solutions:**

1. Check `Environment` setting matches folder name
2. Verify seed scripts are in `DbUpMigration/Seeds/{Environment}/`
3. Check seed script file naming
4. Review logs for seed execution
5. Verify migrations completed successfully (seeds run after migrations)

### Issue: Script Execution Order

**Symptoms:** Scripts running in wrong order

**Solutions:**

1. Use timestamp prefix: `YYYYMMDD-HHMMSS-Description.sql`
2. Scripts are executed alphabetically
3. Ensure timestamps are sequential
4. Use leading zeros for consistent ordering

### Issue: Dollar-Quoting Errors

**Symptoms:** Syntax errors with dollar-quoted strings

**Solutions:**

1. Use `WithVariablesDisabled()` for seed scripts
2. Choose tag that doesn't conflict: `$qINSERT$` (starts with letter)
3. Avoid tags that look like parameters: `$1INSERT$` (PostgreSQL thinks `$1` is parameter)
4. Use consistent tag format: `$tag$...$tag$`

### Issue: Advisory Lock Not Released

**Symptoms:** Lock held after migration completes

**Solutions:**

1. Check `CleanupResources()` is called in `finally` block
2. Verify connection is properly closed
3. Manually release lock if needed:

   ```sql
   SELECT pg_advisory_unlock(3565012658280623778);
   ```

## Related Documentation

- [DbUp Documentation](https://dbup.readthedocs.io/)
- [PostgreSQL Advisory Locks](https://www.postgresql.org/docs/current/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)
- [DatabaseMigrationInitFilter](../src/WebShop.Api/Filters/DatabaseMigrationInitFilter.cs)

## Summary

DbUp provides:

- ✅ SQL-based migrations with full control
- ✅ Automatic execution on application startup
- ✅ Concurrent deployment safety via advisory locks
- ✅ Environment-specific seed data management
- ✅ Idempotent scripts for safe re-execution
- ✅ Transaction safety with automatic rollback
- ✅ Version tracking in `schemaversions` table
- ✅ Integration with ASP.NET Core logging

By using DbUp, developers can manage database schema changes and seed data with confidence, knowing that migrations are safe, automatic, and version-controlled.
