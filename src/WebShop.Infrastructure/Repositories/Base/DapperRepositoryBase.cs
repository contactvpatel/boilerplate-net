using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Infrastructure.Repositories.Base;

/// <summary>
/// Base repository for Dapper operations using hybrid approach.
/// Provides shared write operations and connection management.
/// Derived classes implement read operations with direct Dapper for maximum performance.
/// </summary>
/// <remarks>
/// This base class provides common write operations and connection management.
/// Derived classes must implement their specific repository interface (e.g., ICustomerRepository)
/// which extends IRepository&lt;T&gt;, providing read operations with direct Dapper mapping.
/// </remarks>
/// <typeparam name="T">The entity type, must inherit from BaseEntity.</typeparam>
public abstract class DapperRepositoryBase<T>(
    IDapperConnectionFactory connectionFactory,
    IDapperTransactionManager? transactionManager = null,
    ILoggerFactory? loggerFactory = null) where T : BaseEntity
{
    private readonly ILogger<DapperRepositoryBase<T>>? logger = loggerFactory?.CreateLogger<DapperRepositoryBase<T>>();

    /// <summary>
    /// Table name (lowercase) for database operations.
    /// </summary>
    protected abstract string TableName { get; }

    /// <summary>
    /// Schema name for database operations (typically "webshop").
    /// </summary>
    protected virtual string Schema => "webshop";

    /// <summary>
    /// Creates a read connection for SELECT queries.
    /// Caller must dispose the connection (e.g. with <c>using IDbConnection connection = GetReadConnection();</c>)
    /// or hand it to code that owns its lifetime.
    /// </summary>
    protected IDbConnection GetReadConnection()
    {
        logger?.LogDebug("Creating read connection for {EntityType}", typeof(T).Name);
        return connectionFactory.CreateReadConnection();
    }

    /// <summary>
    /// Gets a write connection, reusing transaction connection if available.
    /// When no transaction is active, the caller (e.g. AddAsync/UpdateAsync/DeleteAsync) must dispose the connection in a finally block.
    /// When a transaction is active, do not dispose—the transaction owns the connection lifetime.
    /// </summary>
    protected IDbConnection GetWriteConnection()
    {
        IDbTransaction? transaction = transactionManager?.GetCurrentTransaction();
        if (transaction?.Connection != null)
        {
            logger?.LogDebug("Reusing transaction connection for {EntityType}", typeof(T).Name);
            return transaction.Connection;
        }

        logger?.LogDebug("Creating write connection for {EntityType}", typeof(T).Name);
        return connectionFactory.CreateWriteConnection();
    }

    /// <summary>
    /// Sets audit fields for entity creation.
    /// </summary>
    protected void SetAuditFieldsForCreate(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.IsActive = true;
        // Note: CreatedBy should be set by service layer using IUserContext
    }

    /// <summary>
    /// Sets audit fields for entity updates.
    /// </summary>
    protected void SetAuditFieldsForUpdate(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        // Note: UpdatedBy should be set by service layer using IUserContext
    }

    /// <summary>
    /// Adds a new entity to the database.
    /// Schema and TableName are validated to prevent SQL injection before building the INSERT statement.
    /// </summary>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        SetAuditFieldsForCreate(entity);

        SqlIdentifierValidator.Validate(Schema, nameof(Schema));
        SqlIdentifierValidator.Validate(TableName, nameof(TableName));
        string sql = BuildInsertSql();
        IDbConnection connection = GetWriteConnection();
        IDbTransaction? transaction = transactionManager?.GetCurrentTransaction();

        try
        {
            entity.Id = await connection.QuerySingleAsync<int>(
                new CommandDefinition(sql, entity, transaction, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            logger?.LogDebug("Added {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
            return entity;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error adding {EntityType}", typeof(T).Name);
            throw;
        }
        finally
        {
            if (transaction == null)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Updates an existing entity in the database.
    /// Schema and TableName are validated to prevent SQL injection before building the UPDATE statement.
    /// </summary>
    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        SetAuditFieldsForUpdate(entity);

        SqlIdentifierValidator.Validate(Schema, nameof(Schema));
        SqlIdentifierValidator.Validate(TableName, nameof(TableName));
        string sql = BuildUpdateSql();
        IDbConnection connection = GetWriteConnection();
        IDbTransaction? transaction = transactionManager?.GetCurrentTransaction();

        try
        {
            int rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(sql, entity, transaction, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException(
                    $"Entity with Id {entity.Id} not found or already soft-deleted. Update operation failed.");
            }

            logger?.LogDebug("Updated {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error updating {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
            throw;
        }
        finally
        {
            if (transaction == null)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Soft deletes an entity from the database (sets IsActive = false).
    /// Schema and TableName are validated to prevent SQL injection (must be alphanumeric/underscore only).
    /// </summary>
    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        SetAuditFieldsForUpdate(entity);

        string schema = SqlIdentifierValidator.Validate(Schema, nameof(Schema));
        string tableName = SqlIdentifierValidator.Validate(TableName, nameof(TableName));
        string sql = $@"
            UPDATE ""{schema}"".""{tableName}""
            SET ""isactive"" = false, ""updated"" = @UpdatedAt, ""updatedby"" = @UpdatedBy
            WHERE ""id"" = @Id AND ""isactive"" = true";

        IDbConnection connection = GetWriteConnection();
        IDbTransaction? transaction = transactionManager?.GetCurrentTransaction();

        try
        {
            int rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(sql, entity, transaction, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException(
                    $"Entity with Id {entity.Id} not found or already deleted. Delete operation failed.");
            }

            entity.IsActive = false;
            logger?.LogDebug("Soft deleted {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error deleting {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
            throw;
        }
        finally
        {
            if (transaction == null)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Checks if an entity exists in the database.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(int id, bool includeSoftDeleted = false, CancellationToken cancellationToken = default)
    {
        string schema = SqlIdentifierValidator.Validate(Schema, nameof(Schema));
        string tableName = SqlIdentifierValidator.Validate(TableName, nameof(TableName));
        string whereClause = includeSoftDeleted
            ? @"""id"" = @Id"
            : @"""id"" = @Id AND ""isactive"" = true";

        string sql = $@"SELECT EXISTS(SELECT 1 FROM ""{schema}"".""{tableName}"" WHERE {whereClause})";

        using IDbConnection connection = GetReadConnection();
        bool exists = await connection.QueryFirstOrDefaultAsync<bool>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return exists;
    }

    /// <summary>
    /// Builds the INSERT SQL statement for this entity type.
    /// Override in derived classes for entity-specific columns.
    /// </summary>
    protected abstract string BuildInsertSql();

    /// <summary>
    /// Builds the UPDATE SQL statement for this entity type.
    /// Override in derived classes for entity-specific columns.
    /// </summary>
    protected abstract string BuildUpdateSql();

}
