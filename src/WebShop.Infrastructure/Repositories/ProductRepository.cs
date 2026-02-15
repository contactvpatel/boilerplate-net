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
                COUNT(*) OVER() AS ""TotalCount""
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

        int totalCount = Convert.ToInt32(GetDictValue((IDictionary<string, object>)resultList[0], "TotalCount"));
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
            WHERE ""category"" = CAST(@Category AS public.category) AND ""isactive"" = true
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

    public async Task<IReadOnlyList<Product>> FindByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Product>();
        }

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
            WHERE ""id"" = ANY(@Ids) AND ""isactive"" = true";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Product> results = await connection.QueryAsync<Product>(
            new CommandDefinition(sql, new { Ids = ids.ToArray() }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return results.ToList();
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
                @Name, @LabelId, CAST(@Category AS public.category), CAST(@Gender AS public.gender), @CurrentlyActive,
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
                ""category"" = CAST(@Category AS public.category),
                ""gender"" = CAST(@Gender AS public.gender),
                ""currentlyactive"" = @CurrentlyActive,
                ""updated"" = @UpdatedAt,
                ""updatedby"" = @UpdatedBy
            WHERE ""id"" = @Id AND ""isactive"" = true";
    }

    private static object? GetDictValue(IDictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out object? v) ? v : d.TryGetValue(key.ToLowerInvariant(), out v) ? v : null;
    }

    private static Product MapToProduct(dynamic row)
    {
        var dict = (IDictionary<string, object>)row;
        return new Product
        {
            Id = Convert.ToInt32(GetDictValue(dict, "Id")),
            Name = GetDictValue(dict, "Name") as string,
            LabelId = GetDictValue(dict, "LabelId") != null ? Convert.ToInt32(GetDictValue(dict, "LabelId")) : null,
            Category = GetDictValue(dict, "Category")?.ToString(),
            Gender = GetDictValue(dict, "Gender")?.ToString(),
            CurrentlyActive = GetDictValue(dict, "CurrentlyActive") as bool?,
            CreatedAt = (DateTime)GetDictValue(dict, "CreatedAt")!,
            CreatedBy = GetDictValue(dict, "CreatedBy") != null ? Convert.ToInt32(GetDictValue(dict, "CreatedBy")) : 0,
            UpdatedAt = GetDictValue(dict, "UpdatedAt") as DateTime?,
            UpdatedBy = GetDictValue(dict, "UpdatedBy") != null ? Convert.ToInt32(GetDictValue(dict, "UpdatedBy")) : 0,
            IsActive = (bool)GetDictValue(dict, "IsActive")!
        };
    }
}
