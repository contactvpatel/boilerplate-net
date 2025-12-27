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
/// Product repository using hybrid Dapper approach for optimal performance.
/// Direct Dapper mapping for reads, shared base class for writes.
/// </summary>
public class ProductRepository : DapperRepositoryBase<Product>, IProductRepository
{
    protected override string TableName => "products";

    public ProductRepository(
        IDapperConnectionFactory connectionFactory,
        IDapperTransactionManager? transactionManager = null,
        ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory)
    {
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""labelid"" AS LabelId,
                ""category"" AS Category,
                ""gender"" AS Gender,
                ""currentlyactive"" AS CurrentlyActive,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""products""
            WHERE ""id"" = @Id AND ""isactive"" = true";

        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Product>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""labelid"" AS LabelId,
                ""category"" AS Category,
                ""gender"" AS Gender,
                ""currentlyactive"" AS CurrentlyActive,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""products""
            WHERE ""isactive"" = true
            ORDER BY ""id""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Product> results = await connection.QueryAsync<Product>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return results.ToList();
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
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
                ""name"" AS Name,
                ""labelid"" AS LabelId,
                ""category"" AS Category,
                ""gender"" AS Gender,
                ""currentlyactive"" AS CurrentlyActive,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive,
                COUNT(*) OVER() AS TotalCount
            FROM ""webshop"".""products""
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
            return (Array.Empty<Product>(), 0);
        }

        int totalCount = (int)((IDictionary<string, object>)resultList[0])["TotalCount"];
        List<Product> products = resultList.Select(MapToProduct).ToList();

        return (products, totalCount);
    }

    public async IAsyncEnumerable<Product> GetAllStreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""labelid"" AS LabelId,
                ""category"" AS Category,
                ""gender"" AS Gender,
                ""currentlyactive"" AS CurrentlyActive,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""products""
            WHERE ""isactive"" = true
            ORDER BY ""id""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Product> results = await connection.QueryAsync<Product>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        foreach (Product product in results)
        {
            yield return product;
        }
    }

    public async Task<List<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""labelid"" AS LabelId,
                ""category"" AS Category,
                ""gender"" AS Gender,
                ""currentlyactive"" AS CurrentlyActive,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""products""
            WHERE ""category"" = @Category AND ""isactive"" = true
            ORDER BY ""name""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Product> results = await connection.QueryAsync<Product>(
            new CommandDefinition(sql, new { Category = category }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return results.ToList();
    }

    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""labelid"" AS LabelId,
                ""category"" AS Category,
                ""gender"" AS Gender,
                ""currentlyactive"" AS CurrentlyActive,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""products""
            WHERE ""currentlyactive"" = true AND ""isactive"" = true
            ORDER BY ""name""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Product> results = await connection.QueryAsync<Product>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return results.ToList();
    }

    public async Task<List<Product>> GetByLabelIdAsync(int labelId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""labelid"" AS LabelId,
                ""category"" AS Category,
                ""gender"" AS Gender,
                ""currentlyactive"" AS CurrentlyActive,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""products""
            WHERE ""labelid"" = @LabelId AND ""isactive"" = true
            ORDER BY ""name""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Product> results = await connection.QueryAsync<Product>(
            new CommandDefinition(sql, new { LabelId = labelId }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return results.ToList();
    }

    public Task<IReadOnlyList<Product>> FindAsync(
        System.Linq.Expressions.Expression<Func<Product, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Dapper does not support LINQ expressions. " +
            "Use specific repository methods with explicit SQL queries (e.g., GetByCategoryAsync).");
    }

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        System.Linq.Expressions.Expression<Func<Product, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Dapper does not support LINQ expressions for pagination filtering. " +
            "Use GetPagedAsync(int pageNumber, int pageSize) for simple pagination.");
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    protected override string BuildInsertSql()
    {
        return @"
            INSERT INTO ""webshop"".""products"" (
                ""name"", ""labelid"", ""category"", ""gender"", ""currentlyactive"",
                ""isactive"", ""created"", ""createdby"", ""updatedby""
            )
            VALUES (
                @Name, @LabelId, @Category, @Gender, @CurrentlyActive,
                @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy
            )
            RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"
            UPDATE ""webshop"".""products""
            SET 
                ""name"" = @Name,
                ""labelid"" = @LabelId,
                ""category"" = @Category,
                ""gender"" = @Gender,
                ""currentlyactive"" = @CurrentlyActive,
                ""updated"" = @UpdatedAt,
                ""updatedby"" = @UpdatedBy
            WHERE ""id"" = @Id AND ""isactive"" = true";
    }

    private static Product MapToProduct(dynamic row)
    {
        IDictionary<string, object> dict = (IDictionary<string, object>)row;
        return new Product
        {
            Id = (int)dict["Id"],
            Name = dict["Name"] != null ? (string)dict["Name"] : null,
            LabelId = dict["LabelId"] != null ? (int?)dict["LabelId"] : null,
            Category = dict["Category"] != null ? (string)dict["Category"] : null,
            Gender = dict["Gender"] != null ? (string)dict["Gender"] : null,
            CurrentlyActive = dict["CurrentlyActive"] != null ? (bool?)dict["CurrentlyActive"] : null,
            CreatedAt = (DateTime)dict["CreatedAt"],
            CreatedBy = dict["CreatedBy"] != null ? (int)dict["CreatedBy"] : 0,
            UpdatedAt = dict["UpdatedAt"] != null ? (DateTime?)dict["UpdatedAt"] : null,
            UpdatedBy = dict["UpdatedBy"] != null ? (int)dict["UpdatedBy"] : 0,
            IsActive = (bool)dict["IsActive"]
        };
    }
}
