using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;
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
public abstract class DapperRepositoryBase<T> where T : BaseEntity
{
    protected readonly IDapperConnectionFactory _connectionFactory;
    protected readonly IDapperTransactionManager? _transactionManager;
    protected readonly ILogger<DapperRepositoryBase<T>>? _logger;

    /// <summary>
    /// Table name (lowercase) for database operations.
    /// </summary>
    protected abstract string TableName { get; }

    /// <summary>
    /// Schema name for database operations (typically "webshop").
    /// </summary>
    protected virtual string Schema => "webshop";

    /// <summary>
    /// Initializes the base repository with connection factory and optional transaction manager.
    /// </summary>
    protected DapperRepositoryBase(
        IDapperConnectionFactory connectionFactory,
        IDapperTransactionManager? transactionManager = null,
        ILoggerFactory? loggerFactory = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _transactionManager = transactionManager;
        _logger = loggerFactory?.CreateLogger<DapperRepositoryBase<T>>();
    }

    /// <summary>
    /// Creates a read connection for SELECT queries.
    /// </summary>
    protected IDbConnection GetReadConnection()
    {
        _logger?.LogDebug("Creating read connection for {EntityType}", typeof(T).Name);
        return _connectionFactory.CreateReadConnection();
    }

    /// <summary>
    /// Gets a write connection, reusing transaction connection if available.
    /// </summary>
    protected IDbConnection GetWriteConnection()
    {
        IDbTransaction? transaction = _transactionManager?.GetCurrentTransaction();
        if (transaction?.Connection != null)
        {
            _logger?.LogDebug("Reusing transaction connection for {EntityType}", typeof(T).Name);
            return transaction.Connection;
        }

        _logger?.LogDebug("Creating write connection for {EntityType}", typeof(T).Name);
        return _connectionFactory.CreateWriteConnection();
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
    /// </summary>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        SetAuditFieldsForCreate(entity);

        string sql = BuildInsertSql();
        IDbConnection connection = GetWriteConnection();
        IDbTransaction? transaction = _transactionManager?.GetCurrentTransaction();

        try
        {
            entity.Id = await connection.QuerySingleAsync<int>(
                new CommandDefinition(sql, entity, transaction, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            _logger?.LogDebug("Added {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding {EntityType}", typeof(T).Name);
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
    /// </summary>
    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        SetAuditFieldsForUpdate(entity);

        string sql = BuildUpdateSql();
        IDbConnection connection = GetWriteConnection();
        IDbTransaction? transaction = _transactionManager?.GetCurrentTransaction();

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

            _logger?.LogDebug("Updated {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
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
    /// </summary>
    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        SetAuditFieldsForUpdate(entity);

        string sql = $@"
            UPDATE ""{Schema}"".""{TableName}""
            SET ""isactive"" = false, ""updated"" = @UpdatedAt, ""updatedby"" = @UpdatedBy
            WHERE ""id"" = @Id AND ""isactive"" = true";

        IDbConnection connection = GetWriteConnection();
        IDbTransaction? transaction = _transactionManager?.GetCurrentTransaction();

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
            _logger?.LogDebug("Soft deleted {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting {EntityType} with Id {Id}", typeof(T).Name, entity.Id);
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
        string whereClause = includeSoftDeleted
            ? @"""id"" = @Id"
            : @"""id"" = @Id AND ""isactive"" = true";

        string sql = $@"SELECT EXISTS(SELECT 1 FROM ""{Schema}"".""{TableName}"" WHERE {whereClause})";

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
