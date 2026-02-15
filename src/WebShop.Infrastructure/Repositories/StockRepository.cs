using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories.Base;

namespace WebShop.Infrastructure.Repositories;

public class StockRepository : DapperRepositoryBase<Stock>, IStockRepository
{
    protected override string TableName => "stock";

    public StockRepository(IDapperConnectionFactory connectionFactory, IDapperTransactionManager? transactionManager = null, ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory) { }

    public async Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, 
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""stock"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Stock>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Stock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""stock"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Stock>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<(IReadOnlyList<Stock> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive, COUNT(*) OVER() AS ""TotalCount"" FROM ""webshop"".""stock"" 
            WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(new CommandDefinition(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken))).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Stock>(), 0);
        }

        int total = Convert.ToInt32(GetDictValue((IDictionary<string, object>)results[0], "TotalCount"));
        return (results.Select(r =>
        {
            IDictionary<string, object> d = (IDictionary<string, object>)r;
            return new Stock
            {
                Id = Convert.ToInt32(GetDictValue(d, "Id")),
                ArticleId = GetDictValue(d, "ArticleId") != null ? Convert.ToInt32(GetDictValue(d, "ArticleId")) : null,
                Count = GetDictValue(d, "Count") != null ? Convert.ToInt32(GetDictValue(d, "Count")) : null,
                CreatedAt = (DateTime)GetDictValue(d, "CreatedAt")!,
                CreatedBy = GetDictValue(d, "CreatedBy") != null ? Convert.ToInt32(GetDictValue(d, "CreatedBy")) : 0,
                UpdatedAt = GetDictValue(d, "UpdatedAt") as DateTime?,
                UpdatedBy = GetDictValue(d, "UpdatedBy") != null ? Convert.ToInt32(GetDictValue(d, "UpdatedBy")) : 0,
                IsActive = (bool)GetDictValue(d, "IsActive")!
            };
        }).ToList(), total);
    }

    public async IAsyncEnumerable<Stock> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Stock> items = await GetAllAsync(cancellationToken);
        foreach (Stock item in items)
        {
            yield return item;
        }
    }

    public async Task<IReadOnlyList<Stock>> FindByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Stock>();
        }

        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""stock"" WHERE ""id"" = ANY(@Ids) AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Stock>(new CommandDefinition(sql, new { Ids = ids.ToArray() }, cancellationToken: cancellationToken))).ToList();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public async Task<Stock?> GetByArticleIdAsync(int articleId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""stock"" 
            WHERE ""articleid"" = @ArticleId AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Stock>(new CommandDefinition(sql, new { ArticleId = articleId }, cancellationToken: cancellationToken));
    }

    public async Task<List<Stock>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""stock"" 
            WHERE ""count"" <= @Threshold AND ""isactive"" = true ORDER BY ""count""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Stock>(new CommandDefinition(sql, new { Threshold = threshold }, cancellationToken: cancellationToken))).ToList();
    }

    private static object? GetDictValue(IDictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out object? v) ? v : d.TryGetValue(key.ToLowerInvariant(), out v) ? v : null;
    }

    protected override string BuildInsertSql()
    {
        return @"INSERT INTO ""webshop"".""stock"" (""articleid"", ""count"", ""isactive"", ""created"", ""createdby"", ""updatedby"") 
        VALUES (@ArticleId, @Count, @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy) RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"UPDATE ""webshop"".""stock"" SET ""articleid"" = @ArticleId, ""count"" = @Count, ""updated"" = @UpdatedAt, 
        ""updatedby"" = @UpdatedBy WHERE ""id"" = @Id AND ""isactive"" = true";
    }
}
