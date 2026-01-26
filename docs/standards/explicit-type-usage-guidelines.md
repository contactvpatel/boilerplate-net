# Explicit Type Usage Guidelines

[← Back to README](../../README.md)

## Overview

This document outlines the coding standards for using explicit types vs. `var` in the WebShop .NET codebase. Following these guidelines improves code readability and maintainability.

## Guidelines

### Use Explicit Types When

1. **The type is not immediately obvious from the right-hand side**

   ```csharp
   // ✅ Good - Type is explicit and clear
   string propertyName = _metadata.ColumnMappings.FirstOrDefault(kvp => kvp.Value == column).Key;
   PropertyInfo? property = typeof(T).GetProperty(propertyName);
   object? value = property.GetValue(entity);
   ```

2. **Loop variables where type clarity is important**

   ```csharp
   // ✅ Good - Explicit type in foreach
   foreach (string column in insertColumns)
   {
       // ...
   }
   
   foreach (KeyValuePair<string, string> mapping in _metadata.ColumnMappings)
   {
       // ...
   }
   ```

3. **Collection types that aren't immediately instantiated**

   ```csharp
   // ✅ Good - Type is explicit
   List<string> insertColumns = GetInsertColumns().ToList();
   List<dynamic> resultList = results.ToList();
   IEnumerable<ProductWithCount> results = await connection.QueryAsync<ProductWithCount>(...);
   ```

4. **Database-related types**

   ```csharp
   // ✅ Good - Database types are explicit
   DbConnection connection = _readContext.Database.GetDbConnection();
   DbCommand command = connection.CreateCommand();
   DbDataReader reader = await command.ExecuteReaderAsync(...);
   IDictionary<string, object> rowDict = (IDictionary<string, object>)row;
   ```

### Use `var` When

1. **The type is obvious from the right-hand side (instantiation)**

   ```csharp
   // ✅ Good - Type is obvious from 'new'
   var parameters = new Dictionary<string, object>();
   var sql = new StringBuilder($"SELECT {columnList} FROM {tableName}");
   ```

2. **Anonymous types (required)**

   ```csharp
   // ✅ Good - Anonymous types MUST use var
   var response = new
   {
       succeeded = false,
       error = "Rate limit exceeded"
   };
   ```

3. **LINQ queries where the type is clear from context**

   ```csharp
   // ✅ Good - LINQ result type is clear
   var propertyMappings = entityType.GetProperties()
       .Where(p => !p.IsShadowProperty())
       .Select(p => new { PropertyName = p.Name, ColumnName = p.GetColumnName() })
       .ToList();
   ```

4. **Simple boolean checks**

   ```csharp
   // ✅ Good - Boolean result is obvious
   var wasOpen = connection.State == System.Data.ConnectionState.Open;
   var isRelational = _readContext.Database.IsRelational();
   ```

## Examples from Codebase

### DapperQueryBuilder.cs

```csharp
public static string BuildSelectQuery(
    string tableName,
    IEnumerable<string> columns,
    string? whereClause = null,
    string? orderBy = null)
{
    // ✅ Explicit type - not obvious from Join
    string columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
    
    // ✅ var is OK - obvious instantiation
    var sql = new StringBuilder($"SELECT {columnList} FROM {tableName}");
    
    return sql.ToString();
}
```

### CustomerRepository.cs (Dapper)

```csharp
public virtual async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    // ✅ Explicit types for database objects
    DbConnection connection = _readContext.Database.GetDbConnection();
    var wasOpen = connection.State == System.Data.ConnectionState.Open;
    
    try
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        
        using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        
        // ✅ Explicit type in foreach
        foreach (var mapping in propertyMappings)
        {
            System.Reflection.PropertyInfo? property = typeof(T).GetProperty(mapping.PropertyName);
            // ...
        }
    }
    finally
    {
        // ...
    }
}
```

## Benefits

### 1. Improved Readability

- Developers can immediately see what type they're working with
- No need to hover over variables in IDE to see types
- Easier code reviews

### 2. Better Documentation

- Code is self-documenting
- Type information is visible in diffs/PRs
- Clearer for junior developers

### 3. Reduced Cognitive Load

- Less mental effort to understand code
- Type information is explicit, not inferred
- Easier to spot type-related bugs

### 4. Consistency

- Consistent style across the codebase
- Clear guidelines for when to use each approach
- Easier to maintain

## Anti-Patterns to Avoid

### ❌ Bad: Using `var` for non-obvious types

```csharp
// ❌ Bad - Type is not obvious
var propertyName = _metadata.ColumnMappings.FirstOrDefault(kvp => kvp.Value == column).Key;
var property = typeof(T).GetProperty(propertyName);
var value = property.GetValue(entity);
```

### ❌ Bad: Explicit types for obvious instantiations

```csharp
// ❌ Bad - Type is redundant
Dictionary<string, object> parameters = new Dictionary<string, object>();
StringBuilder sql = new StringBuilder($"SELECT id AS Id, firstname AS FirstName FROM {tableName}");
```

### ❌ Bad: Using `var` in foreach when type isn't clear

```csharp
// ❌ Bad - What type is 'column'?
foreach (var column in insertColumns)
{
    // ...
}
```

## Migration Summary

The following files were updated to follow these guidelines:

### Infrastructure Layer

1. **`DapperQueryBuilder.cs`**
   - Updated `columnList`, `quotedColumns`, `parameters`, `setClause`, `cleaned` to explicit `string`
   - Updated collection `.ToList()` calls to explicit `List<string>`

2. **Repository Implementations** (e.g., `CustomerRepository.cs`, `ProductRepository.cs`)
   - Updated `connection` to explicit `IDbConnection`
   - Updated `results` to explicit `IEnumerable<TEntity>`
   - Updated loop variables to explicit types

### API Layer

- **`RateLimitingExtensions.cs`** - Uses `var` for anonymous types (correct)
- **`HealthCheckResponseWriter.cs`** - Uses `var` for anonymous types (correct)

## Build Verification

All changes were verified with:

```bash
dotnet build --no-restore
```

**Result:** ✅ Build succeeded with 0 warnings and 0 errors

## References

- [Microsoft C# Coding Conventions - Implicitly Typed Local Variables](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions#implicitly-typed-local-variables)
- [C# Programming Guide - var keyword](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/var)

## Summary

✅ **Use explicit types** for:

- Non-obvious types from method calls
- Loop variables
- Database-related types
- Collection types from LINQ/method calls

✅ **Use `var`** for:

- Obvious instantiations (`new`)
- Anonymous types (required)
- Simple boolean checks
- LINQ queries with clear context

This balance provides the best readability while avoiding unnecessary verbosity.
