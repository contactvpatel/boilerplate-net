using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories.Base;

namespace WebShop.Infrastructure.Repositories;

public class OrderPositionRepository : DapperRepositoryBase<OrderPosition>, IOrderPositionRepository
{
    protected override string TableName => "order_positions";

    public OrderPositionRepository(IDapperConnectionFactory connectionFactory, IDapperTransactionManager? transactionManager = null, ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory) { }

    public async Task<OrderPosition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""orderid"" AS OrderId, ""articleid"" AS ArticleId, ""amount"" AS Amount, ""price"" AS Price,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive 
            FROM ""webshop"".""order_positions"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<OrderPosition>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<OrderPosition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""orderid"" AS OrderId, ""articleid"" AS ArticleId, ""amount"" AS Amount, ""price"" AS Price,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""order_positions"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<OrderPosition>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<(IReadOnlyList<OrderPosition> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""orderid"" AS OrderId, ""articleid"" AS ArticleId, ""amount"" AS Amount, ""price"" AS Price,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive,
            COUNT(*) OVER() AS TotalCount FROM ""webshop"".""order_positions"" WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(sql, new { Offset = offset, PageSize = pageSize })).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<OrderPosition>(), 0);
        }

        int total = (int)((IDictionary<string, object>)results[0])["TotalCount"];
        return (results.Select(r =>
        {
            IDictionary<string, object> dict = (IDictionary<string, object>)r;
            object amountValue = dict["Amount"];
            return new OrderPosition
            {
                Id = (int)dict["Id"],
                OrderId = dict["OrderId"] as int?,
                ArticleId = dict["ArticleId"] as int?,
                Amount = amountValue != null ? (short?)(int)amountValue : null,
                Price = dict["Price"] as decimal?,
                CreatedAt = (DateTime)dict["CreatedAt"],
                CreatedBy = dict["CreatedBy"] != null ? (int)dict["CreatedBy"] : 0,
                UpdatedAt = dict["UpdatedAt"] as DateTime?,
                UpdatedBy = dict["UpdatedBy"] != null ? (int)dict["UpdatedBy"] : 0,
                IsActive = (bool)dict["IsActive"]
            };
        }).ToList(), total);
    }

    public async IAsyncEnumerable<OrderPosition> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrderPosition> items = await GetAllAsync(cancellationToken);
        foreach (OrderPosition item in items)
        {
            yield return item;
        }
    }

    public Task<IReadOnlyList<OrderPosition>> FindAsync(System.Linq.Expressions.Expression<Func<OrderPosition, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use explicit SQL queries.");
    }

    public Task<(IReadOnlyList<OrderPosition> Items, int TotalCount)> GetPagedAsync(System.Linq.Expressions.Expression<Func<OrderPosition, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetPagedAsync(int, int).");
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public async Task<List<OrderPosition>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""orderid"" AS OrderId, ""articleid"" AS ArticleId, ""amount"" AS Amount, ""price"" AS Price,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""order_positions"" WHERE ""orderid"" = @OrderId AND ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<OrderPosition>(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken))).ToList();
    }

    protected override string BuildInsertSql()
    {
        return @"INSERT INTO ""webshop"".""order_positions"" (""orderid"", ""articleid"", ""amount"", ""price"", ""isactive"", ""created"", ""createdby"", ""updatedby"")
        VALUES (@OrderId, @ArticleId, @Amount, @Price, @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy) RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"UPDATE ""webshop"".""order_positions"" SET ""orderid"" = @OrderId, ""articleid"" = @ArticleId, ""amount"" = @Amount, 
        ""price"" = @Price, ""updated"" = @UpdatedAt, ""updatedby"" = @UpdatedBy WHERE ""id"" = @Id AND ""isactive"" = true";
    }
}
