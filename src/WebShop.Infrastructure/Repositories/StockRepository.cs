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

    public async Task<Stock?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, 
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""stock"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Stock>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Stock>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""stock"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Stock>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<(IReadOnlyList<Stock> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive, COUNT(*) OVER() AS TotalCount FROM ""webshop"".""stock"" 
            WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(sql, new { Offset = offset, PageSize = pageSize })).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Stock>(), 0);
        }

        int total = (int)((IDictionary<string, object>)results[0])["TotalCount"];
        return (results.Select(r => new Stock
        {
            Id = (int)((IDictionary<string, object>)r)["Id"],
            ArticleId = ((IDictionary<string, object>)r)["ArticleId"] as int?,
            Count = ((IDictionary<string, object>)r)["Count"] as int?,
            CreatedAt = (DateTime)((IDictionary<string, object>)r)["CreatedAt"],
            CreatedBy = ((IDictionary<string, object>)r)["CreatedBy"] != null ? (int)((IDictionary<string, object>)r)["CreatedBy"] : 0,
            UpdatedAt = ((IDictionary<string, object>)r)["UpdatedAt"] as DateTime?,
            UpdatedBy = ((IDictionary<string, object>)r)["UpdatedBy"] != null ? (int)((IDictionary<string, object>)r)["UpdatedBy"] : 0,
            IsActive = (bool)((IDictionary<string, object>)r)["IsActive"]
        }).ToList(), total);
    }

    public async IAsyncEnumerable<Stock> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        IReadOnlyList<Stock> items = await GetAllAsync(ct);
        foreach (Stock item in items)
        {
            yield return item;
        }
    }

    public Task<IReadOnlyList<Stock>> FindAsync(System.Linq.Expressions.Expression<Func<Stock, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotSupportedException("Use explicit SQL queries.");
    }

    public Task<(IReadOnlyList<Stock> Items, int TotalCount)> GetPagedAsync(System.Linq.Expressions.Expression<Func<Stock, bool>> predicate, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        throw new NotSupportedException("Use GetPagedAsync(int, int).");
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(0);
    }

    public async Task<Stock?> GetByArticleIdAsync(int articleId, CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""stock"" 
            WHERE ""articleid"" = @ArticleId AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Stock>(new CommandDefinition(sql, new { ArticleId = articleId }, cancellationToken: ct));
    }

    public async Task<List<Stock>> GetLowStockAsync(int threshold, CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""articleid"" AS ArticleId, ""count"" AS Count, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""stock"" 
            WHERE ""count"" <= @Threshold AND ""isactive"" = true ORDER BY ""count""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Stock>(new CommandDefinition(sql, new { Threshold = threshold }, cancellationToken: ct))).ToList();
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
