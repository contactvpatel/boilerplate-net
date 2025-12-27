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
    protected override string TableName => "order";

    public OrderRepository(IDapperConnectionFactory connectionFactory, IDapperTransactionManager? transactionManager = null, ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory) { }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive FROM ""webshop"".""order"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Order>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive FROM ""webshop"".""order"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Order>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive, COUNT(*) OVER() AS TotalCount FROM ""webshop"".""order"" WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(sql, new { Offset = offset, PageSize = pageSize })).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Order>(), 0);
        }

        int total = (int)((IDictionary<string, object>)results[0])["TotalCount"];
        return (results.Select(r => new Order
        {
            Id = (int)((IDictionary<string, object>)r)["Id"],
            CustomerId = ((IDictionary<string, object>)r)["CustomerId"] as int?,
            OrderTimestamp = ((IDictionary<string, object>)r)["OrderTimestamp"] as DateTime?,
            ShippingAddressId = ((IDictionary<string, object>)r)["ShippingAddressId"] as int?,
            Total = ((IDictionary<string, object>)r)["Total"] as decimal?,
            ShippingCost = ((IDictionary<string, object>)r)["ShippingCost"] as decimal?,
            CreatedAt = (DateTime)((IDictionary<string, object>)r)["CreatedAt"],
            CreatedBy = ((IDictionary<string, object>)r)["CreatedBy"] != null ? (int)((IDictionary<string, object>)r)["CreatedBy"] : 0,
            UpdatedAt = ((IDictionary<string, object>)r)["UpdatedAt"] as DateTime?,
            UpdatedBy = ((IDictionary<string, object>)r)["UpdatedBy"] != null ? (int)((IDictionary<string, object>)r)["UpdatedBy"] : 0,
            IsActive = (bool)((IDictionary<string, object>)r)["IsActive"]
        }).ToList(), total);
    }

    public async IAsyncEnumerable<Order> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        IReadOnlyList<Order> items = await GetAllAsync(ct);
        foreach (Order item in items)
        {
            yield return item;
        }
    }

    public Task<IReadOnlyList<Order>> FindAsync(System.Linq.Expressions.Expression<Func<Order, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotSupportedException("Use explicit SQL queries.");
    }

    public Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(System.Linq.Expressions.Expression<Func<Order, bool>> predicate, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        throw new NotSupportedException("Use GetPagedAsync(int, int).");
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(0);
    }

    public async Task<List<Order>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive FROM ""webshop"".""order"" WHERE ""customerid"" = @CustomerId AND ""isactive"" = true ORDER BY ""ordertimestamp"" DESC";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Order>(new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: ct))).ToList();
    }

    public async Task<List<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""ordertimestamp"" AS OrderTimestamp, ""shippingaddressid"" AS ShippingAddressId,
            ""total"" AS Total, ""shippingcost"" AS ShippingCost, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy,
            ""isactive"" AS IsActive FROM ""webshop"".""order"" WHERE ""ordertimestamp"" BETWEEN @StartDate AND @EndDate AND ""isactive"" = true ORDER BY ""ordertimestamp""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Order>(new CommandDefinition(sql, new { StartDate = startDate, EndDate = endDate }, cancellationToken: ct))).ToList();
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
