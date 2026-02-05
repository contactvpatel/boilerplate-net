using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories.Base;

namespace WebShop.Infrastructure.Repositories;

public class OrderRepository : DapperRepositoryBase<Order>, IOrderRepository
{
    /// <summary>
    /// Row type for paged order queries. Matches the SELECT columns including COUNT(*) OVER() AS TotalCount
    /// so Dapper can map directly without dynamic/IDictionary.
    /// </summary>
    private sealed class OrderPagedRow
    {
        public int Id { get; init; }
        public int? CustomerId { get; init; }
        public DateTime? OrderTimestamp { get; init; }
        public int? ShippingAddressId { get; init; }
        public decimal? Total { get; init; }
        public decimal? ShippingCost { get; init; }
        public DateTime CreatedAt { get; init; }
        public int CreatedBy { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public int UpdatedBy { get; init; }
        public bool IsActive { get; init; }
        public int TotalCount { get; init; }
    }

    protected override string TableName => "order";

    public OrderRepository(IDapperConnectionFactory connectionFactory, IDapperTransactionManager? transactionManager = null, ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory) { }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive FROM ""webshop"".""order"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Order>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive FROM ""webshop"".""order"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Order>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive, COUNT(*) OVER() AS TotalCount FROM ""webshop"".""order"" WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        IReadOnlyList<OrderPagedRow> rows = (await connection.QueryAsync<OrderPagedRow>(new CommandDefinition(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken))).ToList();
        if (rows.Count == 0)
        {
            return (Array.Empty<Order>(), 0);
        }

        int totalCount = rows[0].TotalCount;
        List<Order> items = rows.Select(r => new Order
        {
            Id = r.Id,
            CustomerId = r.CustomerId,
            OrderTimestamp = r.OrderTimestamp,
            ShippingAddressId = r.ShippingAddressId,
            Total = r.Total,
            ShippingCost = r.ShippingCost,
            CreatedAt = r.CreatedAt,
            CreatedBy = r.CreatedBy,
            UpdatedAt = r.UpdatedAt,
            UpdatedBy = r.UpdatedBy,
            IsActive = r.IsActive
        }).ToList();
        return (items, totalCount);
    }

    public async IAsyncEnumerable<Order> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Order> items = await GetAllAsync(cancellationToken);
        foreach (Order item in items)
        {
            yield return item;
        }
    }

    public Task<IReadOnlyList<Order>> FindAsync(System.Linq.Expressions.Expression<Func<Order, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use explicit SQL queries.");
    }

    public Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(System.Linq.Expressions.Expression<Func<Order, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetPagedAsync(int, int).");
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public async Task<List<Order>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive FROM ""webshop"".""order"" WHERE ""customerid"" = @CustomerId AND ""isactive"" = true ORDER BY ""ordertimestamp"" DESC";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Order>(new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<List<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive FROM ""webshop"".""order"" WHERE ""ordertimestamp"" BETWEEN @StartDate AND @EndDate AND ""isactive"" = true ORDER BY ""ordertimestamp""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Order>(new CommandDefinition(sql, new { StartDate = startDate, EndDate = endDate }, cancellationToken: cancellationToken))).ToList();
    }

    protected override string BuildInsertSql()
    {
        return @"INSERT INTO ""webshop"".""order"" (""customerid"", ""ordertimestamp"", ""shippingaddressid"", ""total"", ""shippingcost"", 
        ""isactive"", ""created"", ""createdby"", ""updatedby"") VALUES (@CustomerId, @OrderTimestamp, @ShippingAddressId, @Total, @ShippingCost, 
        @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy) RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"UPDATE ""webshop"".""order"" SET ""customerid"" = @CustomerId, ""ordertimestamp"" = @OrderTimestamp, 
        ""shippingaddressid"" = @ShippingAddressId, ""total"" = @Total, ""shippingcost"" = @ShippingCost, ""updated"" = @UpdatedAt, 
        ""updatedby"" = @UpdatedBy WHERE ""id"" = @Id AND ""isactive"" = true";
    }
}
