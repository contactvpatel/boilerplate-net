# Database Migration: PostgreSQL to SQL Server

This guide provides comprehensive instructions for migrating the WebShop application from PostgreSQL to SQL Server. **Important**: The application supports only one database provider at a time - the migration completely replaces PostgreSQL with SQL Server.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Migration Process](#migration-process)
- [Code Changes](#code-changes)
- [Data Migration](#data-migration)
- [Testing & Deployment](#testing--deployment)
- [Rollback Plan](#rollback-plan)
- [Troubleshooting](#troubleshooting)

---

## Overview

**Key Considerations:**

- ✅ **Default PostgreSQL**: Application starts with PostgreSQL by default
- ✅ **Manual SQL Server Enable**: Uncomment code to switch to SQL Server
- ✅ **Complete Migration**: PostgreSQL completely removed after migration
- ✅ **Data Migration Required**: Schema and data transfer from PostgreSQL to SQL Server

### Key Differences

| Feature | PostgreSQL (Default) | SQL Server (After Migration) |
|---------|-------------------|-----------------------------|
| **Connection String** | Npgsql | SqlClient |
| **Migration Tool** | dbup-postgresql | dbup-sqlserver |
| **Identity** | SERIAL/SEQUENCE | IDENTITY |
| **Quoting** | `"column"` | `[column]` |
| **Returning Values** | `RETURNING` | `OUTPUT INSERTED` |
| **Case Sensitivity** | Case sensitive | Case insensitive (default) |

---

## Prerequisites

### Development Environment

- **SQL Server** (Developer Edition recommended)
  - Download from: <https://www.microsoft.com/en-us/sql-server/sql-server-downloads>
  - Or use SQL Server Express: <https://www.microsoft.com/en-us/download/details.aspx?id=101064>

- **SQL Server Management Studio (SSMS)**
  - Download from: <https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms>

### NuGet Packages

**Default (PostgreSQL):**

```xml
<PackageVersion Include="dbup-postgresql" Version="6.1.5" />
<PackageVersion Include="Npgsql" Version="10.0.1" />
```

**SQL Server (Commented Out):**

```xml
<!-- <PackageVersion Include="dbup-sqlserver" Version="6.0.16" /> -->
<!-- <PackageVersion Include="Microsoft.Data.SqlClient" Version="6.1.4" /> -->
```

### Database Permissions

Ensure your SQL Server user has these permissions:

- `CREATE DATABASE`
- `ALTER DATABASE`
- `CREATE TABLE`, `ALTER TABLE`, `DROP TABLE`
- `SELECT`, `INSERT`, `UPDATE`, `DELETE` on all tables
- `EXECUTE` on stored procedures

---

## Complete Migration Process

### Phase 1: Preparation (PostgreSQL Still Active)

#### Step 1: Install SQL Server

1. **Download and Install SQL Server**
   - SQL Server Developer Edition: <https://www.microsoft.com/en-us/sql-server/sql-server-downloads>
   - Or SQL Server Express for development

2. **Install SQL Server Management Studio (SSMS)**
   - Download: <https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms>

3. **Create Database and User**

```sql
-- Create database
CREATE DATABASE webshopdb;
GO

-- Create login
CREATE LOGIN webshop_user WITH PASSWORD = 'YourSecurePassword123!';
GO

-- Create user and grant permissions
USE webshopdb;
CREATE USER webshop_user FOR LOGIN webshop_user;
ALTER ROLE db_owner ADD MEMBER webshop_user;
GO
```

#### Step 2: Enable SQL Server Packages

**File:** `Directory.Packages.props`

Replace PostgreSQL packages with SQL Server packages:

```xml
<!-- Remove PostgreSQL packages -->
<!-- <PackageVersion Include="dbup-postgresql" Version="6.1.5" /> -->
<!-- <PackageVersion Include="Npgsql" Version="10.0.1" /> -->

<!-- Add SQL Server packages -->
<PackageVersion Include="dbup-sqlserver" Version="6.0.16" />
<PackageVersion Include="Microsoft.Data.SqlClient" Version="6.1.4" />
```

### Phase 2: Code Migration (Switch Database Providers)

#### Step 3: Update Project References

**File:** `src/WebShop.Infrastructure/WebShop.Infrastructure.csproj`

Replace all PostgreSQL references with SQL Server:

```xml
<!-- Remove PostgreSQL -->
<!-- <PackageReference Include="Npgsql" /> -->

<!-- Add SQL Server -->
<PackageReference Include="dbup-sqlserver" />
<PackageReference Include="Microsoft.Data.SqlClient" />
```

#### Step 4: Update Database Configuration

**File:** `src/WebShop.Api/appsettings.json`

Replace entire database configuration:

```json
{
  "DbConnectionSettings": {
    "Read": {
      "Host": "localhost",
      "Port": "1433",
      "DatabaseName": "webshopdb",
      "UserId": "webshop_user",
      "Password": "YourSecurePassword123!",
      "ApplicationName": "webshop-api"
    },
    "Write": {
      "Host": "localhost",
      "Port": "1433",
      "DatabaseName": "webshopdb",
      "UserId": "webshop_user",
      "Password": "YourSecurePassword123!",
      "ApplicationName": "webshop-api"
    }
  }
}
```

#### Step 5: Uncomment SQL Server Connection Factory

**File:** `src/WebShop.Infrastructure/Helpers/DapperConnectionFactory.cs`

**Current (PostgreSQL active):**

```csharp
// using Microsoft.Data.SqlClient; // Uncomment for SQL Server support
using Npgsql;

public IDbConnection CreateReadConnection()
{
    IDbConnection connection = new NpgsqlConnection(_readConnectionString);

    // If SQL Server support is enabled, uncomment the following line, add corresponding using directive and remove the above line for PostgreSQL

    // IDbConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_readConnectionString);

    return connection;
}
```

**For SQL Server:** Uncomment the SQL Server line and using directive, comment out PostgreSQL.

#### Step 6: Uncomment SQL Server Connection String Builder

**File:** `src/WebShop.Util/Models/DbConnectionModel.cs`

**Current (PostgreSQL active):**

```csharp
// using Microsoft.Data.SqlClient; // Uncomment for SQL Server support
using Npgsql;

public static string CreateConnectionString(ConnectionModel databaseConnectionModel, string applicationName)
{
    return CreatePostgreSQLConnectionString(databaseConnectionModel, applicationName);

    // If SQL Server support is enabled, uncomment the following line and CreateSQLServerConnectionString function, add corresponding using directive and remove the above line for PostgreSQL

    // return CreateSQLServerConnectionString(databaseConnectionModel, applicationName);
}

/* Uncomment for SQL Server support:
private static string CreateSQLServerConnectionString(ConnectionModel databaseConnectionModel, string applicationName)
{
    var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
    {
        DataSource = $"{databaseConnectionModel.Host}{(string.IsNullOrEmpty(databaseConnectionModel.Port) ? "" : $",{databaseConnectionModel.Port}")}",
        InitialCatalog = databaseConnectionModel.DatabaseName,
        UserID = databaseConnectionModel.UserId,
        Password = databaseConnectionModel.Password,
        ApplicationName = applicationName,
        Encrypt = databaseConnectionModel.SslMode?.ToUpperInvariant() switch
        {
            "REQUIRE" or "VERIFYCA" or "VERIFYFULL" => true,
            _ => false
        },
        TrustServerCertificate = databaseConnectionModel.SslMode?.ToUpperInvariant() switch
        {
            "ALLOW" or "DISABLE" => true,
            _ => false
        },
        MaxPoolSize = databaseConnectionModel.MaxPoolSize ?? 100,
        MinPoolSize = databaseConnectionModel.MinPoolSize ?? 5,
        CommandTimeout = databaseConnectionModel.CommandTimeout ?? 30,
        ConnectTimeout = databaseConnectionModel.Timeout ?? 15,
        MultipleActiveResultSets = true,
        PersistSecurityInfo = false
    };

    return builder.ConnectionString;
}
*/
```

**For SQL Server:** Uncomment the SQL Server method and using directive, comment out PostgreSQL call.

#### Step 7: Update Migration System

**File:** `src/WebShop.Api/Filters/DatabaseMigrationInitFilter.cs`

Replace PostgreSQL migration with SQL Server:

```csharp
// Replace PostgreSQL migration
// UpgradeEngine migrationUpgrader = DeployChanges.To
//     .PostgresqlDatabase(dbConnectionString)

// With SQL Server migration
UpgradeEngine migrationUpgrader = DeployChanges.To
    .SqlDatabase(dbConnectionString)
    .WithScriptsFromFileSystem(migrationPath)
    .WithTransaction()
    .WithExecutionTimeout(TimeSpan.FromMinutes(10))
    .LogTo(dbUpLogger)
    .Build();
```

#### Step 8: Update Query Builder

**File:** `src/WebShop.Infrastructure/Helpers/DapperQueryBuilder.cs`

Replace PostgreSQL syntax with SQL Server syntax:

```csharp
// Update quoting from PostgreSQL to SQL Server
public static string BuildSelectQuery(string tableName, IEnumerable<string> columns, string? whereClause = null, string? orderBy = null)
{
    string columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
    StringBuilder sql = new StringBuilder($"SELECT {columnList} FROM {tableName}");
    // ... rest remains similar
}

// Update INSERT to use OUTPUT instead of RETURNING
public static string BuildInsertQuery(string tableName, IEnumerable<string> columns, string returningColumn = "id")
{
    List<string> columnList = columns.ToList();
    string quotedColumns = string.Join(", ", columnList.Select(c => $"[{c}]"));
    string parameters = string.Join(", ", columnList.Select(c => $"@{c.Replace("[", "").Replace("]", "")}"));

    return $"INSERT INTO {tableName} ({quotedColumns}) OUTPUT INSERTED.[{returningColumn}] VALUES ({parameters})";
}

// Update table name building
public static string BuildTableName(string schema, string tableName)
{
    return $"[{schema}].[{tableName}]";
}
```

---

## Migration Process

### Phase 1: Preparation (PostgreSQL Active)

1. **Install SQL Server & Tools**
   - SQL Server Developer Edition or Express
   - SQL Server Management Studio (SSMS)
   - Create database and user with proper permissions

2. **Enable SQL Server Packages**
   - Uncomment SQL Server packages in `Directory.Packages.props`
   - Remove or comment out PostgreSQL packages

### Phase 2: Code Changes (Manual Uncommenting)

1. **Update Project References**
   - Uncomment SQL Server packages in all `.csproj` files
   - Comment out PostgreSQL packages

2. **Update Configuration**
   - Update connection strings for SQL Server format in `appsettings.json`

3. **Uncomment SQL Server Code**
   - **DapperConnectionFactory.cs**: Uncomment SQL Server connection creation
   - **DbConnectionModel.cs**: Uncomment SQL Server connection string method
   - **DatabaseMigrationInitFilter.cs**: Uncomment SQL Server migration setup
   - **DapperQueryBuilder.cs**: Update for SQL Server syntax

### Phase 3: Data Migration

1. **Migrate Schema & Data**
   - Adapt migration scripts for SQL Server syntax
   - Use import/export tools or custom scripts
   - Handle data type conversions (SERIAL→IDENTITY, etc.)

### Phase 4: Cleanup & Verification

1. **Remove PostgreSQL Code**
   - Comment out or remove PostgreSQL implementations
   - Update documentation to reflect SQL Server only

### Phase 5: Testing & Deployment

1. **Full Testing & Production Deployment**
   - Unit/integration/load testing with SQL Server
   - Production deployment with monitoring

---

## Code Changes

### Connection Factory Updates

**File:** `src/WebShop.Infrastructure/Helpers/DapperConnectionFactory.cs`

**Current (PostgreSQL):**

```csharp
public IDbConnection CreateReadConnection()
{
    IDbConnection connection = new NpgsqlConnection(_readConnectionString);
    // If SQL Server support is enabled, uncomment the following line...
    // IDbConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_readConnectionString);
    return connection;
}
```

**For SQL Server:** Uncomment the SQL Server line and add using directive.

### Connection String Builder Updates

**File:** `src/WebShop.Util/Models/DbConnectionModel.cs`

**Current (PostgreSQL):**

```csharp
public static string CreateConnectionString(ConnectionModel databaseConnectionModel, string applicationName)
{
    return CreatePostgreSQLConnectionString(databaseConnectionModel, applicationName);
    // If SQL Server support is enabled, uncomment the following line...
    // return CreateSQLServerConnectionString(databaseConnectionModel, applicationName);
}
```

**For SQL Server:** Uncomment the SQL Server line and `CreateSQLServerConnectionString` method.

### Migration System Updates

**File:** `src/WebShop.Api/Filters/DatabaseMigrationInitFilter.cs`

**Current (PostgreSQL):**

```csharp
// PostgreSQL migration (current)
UpgradeEngine migrationUpgrader = DeployChanges.To
    .PostgresqlDatabase(dbConnectionString)...
```

**For SQL Server:** Replace with:

```csharp
UpgradeEngine migrationUpgrader = DeployChanges.To
    .SqlDatabase(dbConnectionString)...
```

### Query Builder Updates

**File:** `src/WebShop.Infrastructure/Helpers/DapperQueryBuilder.cs`

Update syntax for SQL Server:

- Column quoting: `"column"` → `[column]`
- Table names: `"schema"."table"` → `[schema].[table]`
- INSERT returns: `RETURNING id` → `OUTPUT INSERTED.[id]`
- Booleans: `true`/`false` → `1`/`0`

### Data Type Mapping

| PostgreSQL | SQL Server | Notes |
|------------|------------|-------|
| `SERIAL` | `INT IDENTITY` | Auto-incrementing PK |
| `BIGSERIAL` | `BIGINT IDENTITY` | Large PKs |
| `VARCHAR(n)` | `NVARCHAR(n)` | Unicode support |
| `TEXT` | `NVARCHAR(MAX)` | Large text |
| `TIMESTAMP` | `DATETIME2` | Higher precision |
| `BOOLEAN` | `BIT` | True/false |
| `UUID` | `UNIQUEIDENTIFIER` | GUIDs |
| `JSONB` | `NVARCHAR(MAX)` | JSON storage |

---

## Data Migration

### Migration Approaches

**Option 1: Fresh Schema**

- Run DbUp migrations to create SQL Server schema from scratch
- Import data using SQL Server Import/Export Wizard

**Option 2: Schema Conversion**

- Adapt existing PostgreSQL migration scripts for SQL Server
- Handle data type conversions and identity columns

### Common Schema Changes

**Identity Columns:**

```sql
-- PostgreSQL sequences
CREATE SEQUENCE customer_id_seq;
CREATE TABLE customers (id INTEGER DEFAULT nextval('customer_id_seq') PRIMARY KEY, ...);

-- SQL Server identity
CREATE TABLE customers (id INT IDENTITY(1,1) PRIMARY KEY, ...);
```

**Data Types:**

```sql
-- PostgreSQL → SQL Server
SERIAL → INT IDENTITY
BIGSERIAL → BIGINT IDENTITY
VARCHAR(n) → NVARCHAR(n)  -- Unicode support
TEXT → NVARCHAR(MAX)
TIMESTAMP → DATETIME2
BOOLEAN → BIT
UUID → UNIQUEIDENTIFIER
JSONB → NVARCHAR(MAX)
```

## Troubleshooting

### Connection Issues

- **Login failed**: Check authentication mode, user permissions, password policies
- **SSL Provider error**: Verify encryption settings and certificates

### Migration Issues

- **Invalid object name**: Check schema casing (SQL Server case-insensitive)
- **Identity column errors**: Use `SET IDENTITY_INSERT ON/OFF` for explicit inserts

### Performance Issues

- **Slow queries**: Update statistics, check execution plans, defragment indexes
- **Connection pool exhaustion**: Adjust pool settings, monitor with DMVs

### Data Issues

- **String truncation**: SQL Server stricter on length limits
- **Arithmetic overflow**: Check numeric precision and scale

### Performance Comparison

| Operation | PostgreSQL | SQL Server | Notes |
|-----------|------------|------------|-------|
| Simple SELECT | Fast | Fast | Similar |
| Complex JOINs | Good | Excellent | SQL Server optimizer |
| Bulk INSERT | Good | Excellent | SQL Server bulk ops |
| JSON Operations | Excellent | Good | PostgreSQL JSONB |
| Full-text Search | Good | Excellent | SQL Server FTS |
| Geospatial | Excellent | Good | PostgreSQL PostGIS |

### Monitoring (SQL Server)

**Key DMVs:**

```sql
SELECT * FROM sys.dm_exec_connections;          -- Active connections
SELECT * FROM sys.dm_exec_query_stats ORDER BY total_worker_time DESC;  -- Query performance
```

**Backup Strategy:** Full (weekly) + Differential (daily) + Transaction logs (15-30 min)

**High Availability:** Always On Availability Groups, Failover Clustering

---

## Migration Checklist

- [ ] **Preparation**: SQL Server installed, database/user created, PostgreSQL backed up
- [ ] **Packages**: Uncomment SQL Server packages, comment out PostgreSQL packages
- [ ] **Configuration**: Update connection strings for SQL Server format
- [ ] **Code**: Uncomment SQL Server implementations in connection factory, builder, migrations
- [ ] **Queries**: Update syntax for SQL Server (brackets, OUTPUT INSERTED, boolean conversion)
- [ ] **Data**: Adapt migration scripts, transfer schema and data
- [ ] **Cleanup**: remove out PostgreSQL implementations, update documentation

---

## Resources

**Documentation:** [SQL Server Docs](https://docs.microsoft.com/en-us/sql/sql-server/)  
**Migration Tools:** SSMS, Azure Data Studio, SQL Server Migration Assistant  
**Community:** Stack Overflow, SQL Server Central Forums

---
