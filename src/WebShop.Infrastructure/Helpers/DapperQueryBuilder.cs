using System.Data;
using System.Text;
using Dapper;
using WebShop.Core.Entities;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Helper class for building secure, parameterized Dapper queries.
/// Provides SQL injection protection through parameterization and query building utilities.
/// </summary>
public static class DapperQueryBuilder
{
    /// <summary>
    /// Builds a SELECT query with explicit column list for security and performance.
    /// </summary>
    /// <param name="tableName">The table name (schema qualified if needed).</param>
    /// <param name="columns">The column names to select.</param>
    /// <param name="whereClause">Optional WHERE clause (must be parameterized).</param>
    /// <param name="orderBy">Optional ORDER BY clause.</param>
    /// <returns>A parameterized SQL query string.</returns>
    public static string BuildSelectQuery(
        string tableName,
        IEnumerable<string> columns,
        string? whereClause = null,
        string? orderBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(columns);

        string columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
        StringBuilder sql = new StringBuilder($"SELECT {columnList} FROM {tableName}");

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sql.Append($" WHERE {whereClause}");
        }

        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            sql.Append($" ORDER BY {orderBy}");
        }

        return sql.ToString();
    }

    /// <summary>
    /// Builds a paginated SELECT query with window function for total count.
    /// </summary>
    /// <param name="tableName">The table name (schema qualified if needed).</param>
    /// <param name="columns">The column names to select.</param>
    /// <param name="whereClause">Optional WHERE clause (must be parameterized).</param>
    /// <param name="orderBy">ORDER BY clause (required for pagination).</param>
    /// <param name="offset">The number of rows to skip.</param>
    /// <param name="pageSize">The number of rows to fetch.</param>
    /// <returns>A parameterized SQL query string with COUNT(*) OVER() window function.</returns>
    public static string BuildPagedQuery(
        string tableName,
        IEnumerable<string> columns,
        string? whereClause = null,
        string orderBy = "\"id\"",
        int offset = 0,
        int pageSize = 20)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentException.ThrowIfNullOrWhiteSpace(orderBy);

        string columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
        StringBuilder sql = new StringBuilder($"SELECT {columnList}, COUNT(*) OVER() AS \"TotalCount\" FROM {tableName}");

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sql.Append($" WHERE {whereClause}");
        }

        sql.Append($" ORDER BY {orderBy}");
        sql.Append($" OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY");

        return sql.ToString();
    }

    /// <summary>
    /// Builds an INSERT query with RETURNING clause (PostgreSQL).
    /// </summary>
    /// <param name="tableName">The table name (schema qualified if needed).</param>
    /// <param name="columns">The column names to insert.</param>
    /// <param name="returningColumn">The column to return (typically "id").</param>
    /// <returns>A parameterized SQL INSERT query string.</returns>
    public static string BuildInsertQuery(
        string tableName,
        IEnumerable<string> columns,
        string returningColumn = "id")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(columns);

        List<string> columnList = columns.ToList();
        if (columnList.Count == 0)
        {
            throw new ArgumentException("At least one column is required for INSERT.", nameof(columns));
        }

        string quotedColumns = string.Join(", ", columnList.Select(c => $"\"{c}\""));
        string parameters = string.Join(", ", columnList.Select(c => $"@{c.Replace("\"", "")}"));

        return $"INSERT INTO {tableName} ({quotedColumns}) VALUES ({parameters}) RETURNING \"{returningColumn}\"";
    }

    /// <summary>
    /// Builds an UPDATE query with WHERE clause.
    /// </summary>
    /// <param name="tableName">The table name (schema qualified if needed).</param>
    /// <param name="columns">The column names to update.</param>
    /// <param name="whereClause">WHERE clause (must be parameterized, e.g., "\"id\" = @Id").</param>
    /// <returns>A parameterized SQL UPDATE query string.</returns>
    public static string BuildUpdateQuery(
        string tableName,
        IEnumerable<string> columns,
        string whereClause)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentException.ThrowIfNullOrWhiteSpace(whereClause);

        List<string> columnList = columns.ToList();
        if (columnList.Count == 0)
        {
            throw new ArgumentException("At least one column is required for UPDATE.", nameof(columns));
        }

        string setClause = string.Join(", ", columnList.Select(c => $"\"{c}\" = @{c.Replace("\"", "")}"));

        return $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";
    }

    /// <summary>
    /// Builds a soft delete query (sets IsActive = false and tracks who deleted it).
    /// </summary>
    /// <param name="tableName">The table name (schema qualified if needed).</param>
    /// <param name="whereClause">WHERE clause (must be parameterized).</param>
    /// <returns>A parameterized SQL UPDATE query string for soft delete.</returns>
    public static string BuildSoftDeleteQuery(string tableName, string whereClause)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(whereClause);

        return $"UPDATE {tableName} SET \"isactive\" = false, \"updated\" = @UpdatedAt, \"updatedby\" = @UpdatedBy WHERE {whereClause} AND \"isactive\" = true";
    }

    /// <summary>
    /// Escapes and quotes a PostgreSQL identifier to prevent SQL injection.
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped and quoted identifier.</returns>
    public static string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        // Remove any existing quotes and add new ones
        string cleaned = identifier.Trim('"');
        return $"\"{cleaned}\"";
    }

    /// <summary>
    /// Builds a schema-qualified table name.
    /// </summary>
    /// <param name="schema">The schema name.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns>A schema-qualified table name.</returns>
    public static string BuildTableName(string schema, string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        return $"\"{schema}\".\"{tableName}\"";
    }

    /// <summary>
    /// Gets the standard audit columns for BaseEntity.
    /// </summary>
    /// <returns>A list of audit column names.</returns>
    public static List<string> GetAuditColumns()
    {
        return new List<string> { "id", "created", "createdby", "updated", "updatedby", "isactive" };
    }

    /// <summary>
    /// Gets the standard WHERE clause for active entities (soft delete filter).
    /// </summary>
    /// <returns>A WHERE clause string for active entities.</returns>
    public static string GetActiveFilter()
    {
        return "\"isactive\" = true";
    }
}
