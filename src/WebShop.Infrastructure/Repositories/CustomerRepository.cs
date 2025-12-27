using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories.Base;
using CommandDefinition = Dapper.CommandDefinition;

namespace WebShop.Infrastructure.Repositories;

/// <summary>
/// Customer repository using hybrid Dapper approach for optimal performance.
/// Direct Dapper mapping for reads, shared base class for writes.
/// </summary>
public class CustomerRepository : DapperRepositoryBase<Customer>, ICustomerRepository
{
    protected override string TableName => "customer";

    public CustomerRepository(
        IDapperConnectionFactory connectionFactory,
        IDapperTransactionManager? transactionManager = null,
        ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory)
    {
    }

    /// <summary>
    /// Retrieves customer by unique identifier.
    /// </summary>
    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""firstname"" AS FirstName,
                ""lastname"" AS LastName,
                ""gender"" AS Gender,
                ""email"" AS Email,
                ""dateofbirth"" AS DateOfBirth,
                ""currentaddressid"" AS CurrentAddressId,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""customer""
            WHERE ""id"" = @Id AND ""isactive"" = true";

        using IDbConnection connection = GetReadConnection();

        return await connection.QueryFirstOrDefaultAsync<Customer>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all active customers.
    /// </summary>
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""firstname"" AS FirstName,
                ""lastname"" AS LastName,
                ""gender"" AS Gender,
                ""email"" AS Email,
                ""dateofbirth"" AS DateOfBirth,
                ""currentaddressid"" AS CurrentAddressId,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""customer""
            WHERE ""isactive"" = true
            ORDER BY ""id""";

        using IDbConnection connection = GetReadConnection();

        IEnumerable<Customer> results = await connection.QueryAsync<Customer>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return results.ToList();
    }

    /// <summary>
    /// Retrieves customers with pagination support.
    /// </summary>
    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;

        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""firstname"" AS FirstName,
                ""lastname"" AS LastName,
                ""gender"" AS Gender,
                ""email"" AS Email,
                ""dateofbirth"" AS DateOfBirth,
                ""currentaddressid"" AS CurrentAddressId,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive,
                COUNT(*) OVER() AS TotalCount
            FROM ""webshop"".""customer""
            WHERE ""isactive"" = true
            ORDER BY ""id""
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        using IDbConnection connection = GetReadConnection();

        IEnumerable<dynamic> results = await connection.QueryAsync<dynamic>(
            new CommandDefinition(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        List<dynamic> resultList = results.ToList();
        if (resultList.Count == 0)
        {
            return (Array.Empty<Customer>(), 0);
        }

        // Extract total count from first row (window function)
        int totalCount = (int)((IDictionary<string, object>)resultList[0])["TotalCount"];

        // Map to Customer entities
        List<Customer> customers = resultList.Select(MapToCustomer).ToList();

        return (customers, totalCount);
    }

    /// <summary>
    /// Streams all active customers asynchronously for large result sets.
    /// </summary>
    public async IAsyncEnumerable<Customer> GetAllStreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""firstname"" AS FirstName,
                ""lastname"" AS LastName,
                ""gender"" AS Gender,
                ""email"" AS Email,
                ""dateofbirth"" AS DateOfBirth,
                ""currentaddressid"" AS CurrentAddressId,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""customer""
            WHERE ""isactive"" = true
            ORDER BY ""id""";

        using IDbConnection connection = GetReadConnection();

        IEnumerable<Customer> results = await connection.QueryAsync<Customer>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        foreach (Customer customer in results)
        {
            yield return customer;
        }
    }

    /// <summary>
    /// Retrieves customer by email address.
    /// </summary>
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""firstname"" AS FirstName,
                ""lastname"" AS LastName,
                ""gender"" AS Gender,
                ""email"" AS Email,
                ""dateofbirth"" AS DateOfBirth,
                ""currentaddressid"" AS CurrentAddressId,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""customer""
            WHERE ""email"" = @Email AND ""isactive"" = true";

        using IDbConnection connection = GetReadConnection();

        return await connection.QueryFirstOrDefaultAsync<Customer>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all customers with their address information.
    /// Note: Currently returns customers only. For actual join, implement explicit SQL join.
    /// </summary>
    public async Task<List<Customer>> GetAllWithAddressAsync(CancellationToken cancellationToken = default)
    {
        // For now, returns all customers (address relationship can be loaded separately if needed)
        // For true join with Address entity, implement explicit SQL join query:
        // SELECT c.*, a.* FROM customer c LEFT JOIN address a ON c.currentaddressid = a.id
        IReadOnlyList<Customer> customers = await GetAllAsync(cancellationToken);
        return customers.ToList();
    }

    /// <summary>
    /// Builds INSERT SQL for customer entity.
    /// </summary>
    protected override string BuildInsertSql()
    {
        return @"
            INSERT INTO ""webshop"".""customer"" (
                ""firstname"", ""lastname"", ""gender"", ""email"", ""dateofbirth"",
                ""currentaddressid"", ""isactive"", ""created"", ""createdby"", ""updatedby""
            )
            VALUES (
                @FirstName, @LastName, @Gender, @Email, @DateOfBirth,
                @CurrentAddressId, @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy
            )
            RETURNING ""id""";
    }

    /// <summary>
    /// Builds UPDATE SQL for customer entity.
    /// </summary>
    protected override string BuildUpdateSql()
    {
        return @"
            UPDATE ""webshop"".""customer""
            SET 
                ""firstname"" = @FirstName,
                ""lastname"" = @LastName,
                ""gender"" = @Gender,
                ""email"" = @Email,
                ""dateofbirth"" = @DateOfBirth,
                ""currentaddressid"" = @CurrentAddressId,
                ""updated"" = @UpdatedAt,
                ""updatedby"" = @UpdatedBy
            WHERE ""id"" = @Id AND ""isactive"" = true";
    }

    /// <summary>
    /// Not supported with Dapper hybrid approach. Use specific repository methods with explicit SQL.
    /// </summary>
    public Task<IReadOnlyList<Customer>> FindAsync(
        System.Linq.Expressions.Expression<Func<Customer, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Dapper does not support LINQ expressions. " +
            "Use specific repository methods with explicit SQL queries for filtering (e.g., GetByEmailAsync).");
    }

    /// <summary>
    /// Not supported with Dapper hybrid approach. Use specific paginated methods with explicit SQL.
    /// </summary>
    public Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        System.Linq.Expressions.Expression<Func<Customer, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Dapper does not support LINQ expressions for pagination filtering. " +
            "Use GetPagedAsync(int pageNumber, int pageSize) for simple pagination.");
    }

    /// <summary>
    /// Not needed with Dapper - operations execute immediately.
    /// Included for interface compatibility only.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dapper executes SQL immediately, so there's nothing to save
        // This method exists for interface compatibility
        // If using transactions, commit should be handled by IDapperTransactionManager
        return Task.FromResult(0);
    }

    /// <summary>
    /// Maps dynamic result to Customer entity (used for pagination with TotalCount).
    /// </summary>
    private static Customer MapToCustomer(dynamic row)
    {
        IDictionary<string, object> dict = (IDictionary<string, object>)row;
        return new Customer
        {
            Id = (int)dict["Id"],
            FirstName = (string)dict["FirstName"],
            LastName = (string)dict["LastName"],
            Gender = dict["Gender"] != null ? (string)dict["Gender"] : string.Empty,
            Email = (string)dict["Email"],
            DateOfBirth = dict["DateOfBirth"] != null ? (DateTime?)dict["DateOfBirth"] : null,
            CurrentAddressId = dict["CurrentAddressId"] != null ? (int?)dict["CurrentAddressId"] : null,
            CreatedAt = (DateTime)dict["CreatedAt"],
            CreatedBy = dict["CreatedBy"] != null ? (int)dict["CreatedBy"] : 0,
            UpdatedAt = dict["UpdatedAt"] != null ? (DateTime?)dict["UpdatedAt"] : null,
            UpdatedBy = dict["UpdatedBy"] != null ? (int)dict["UpdatedBy"] : 0,
            IsActive = (bool)dict["IsActive"]
        };
    }
}
