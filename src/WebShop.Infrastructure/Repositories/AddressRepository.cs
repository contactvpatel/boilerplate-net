using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories.Base;

namespace WebShop.Infrastructure.Repositories;

public class AddressRepository : DapperRepositoryBase<Address>, IAddressRepository
{
    protected override string TableName => "address";

    public AddressRepository(IDapperConnectionFactory connectionFactory, IDapperTransactionManager? transactionManager = null, ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory) { }

    public async Task<Address?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""address"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Address>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Address>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""address"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Address>(new CommandDefinition(sql, cancellationToken: ct))).ToList();
    }

    public async Task<(IReadOnlyList<Address> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive, COUNT(*) OVER() AS TotalCount FROM ""webshop"".""address"" 
            WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(sql, new { Offset = offset, PageSize = pageSize })).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Address>(), 0);
        }

        int total = (int)((IDictionary<string, object>)results[0])["TotalCount"];
        return (results.Select(r => new Address
        {
            Id = (int)((IDictionary<string, object>)r)["Id"],
            CustomerId = ((IDictionary<string, object>)r)["CustomerId"] as int?,
            FirstName = ((IDictionary<string, object>)r)["FirstName"] as string,
            LastName = ((IDictionary<string, object>)r)["LastName"] as string,
            Address1 = ((IDictionary<string, object>)r)["Address1"] as string,
            Address2 = ((IDictionary<string, object>)r)["Address2"] as string,
            City = ((IDictionary<string, object>)r)["City"] as string,
            Zip = ((IDictionary<string, object>)r)["Zip"] as string,
            CreatedAt = (DateTime)((IDictionary<string, object>)r)["CreatedAt"],
            CreatedBy = ((IDictionary<string, object>)r)["CreatedBy"] != null ? (int)((IDictionary<string, object>)r)["CreatedBy"] : 0,
            UpdatedAt = ((IDictionary<string, object>)r)["UpdatedAt"] as DateTime?,
            UpdatedBy = ((IDictionary<string, object>)r)["UpdatedBy"] != null ? (int)((IDictionary<string, object>)r)["UpdatedBy"] : 0,
            IsActive = (bool)((IDictionary<string, object>)r)["IsActive"]
        }).ToList(), total);
    }

    public async IAsyncEnumerable<Address> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        IReadOnlyList<Address> items = await GetAllAsync(ct);
        foreach (Address item in items)
        {
            yield return item;
        }
    }

    public Task<IReadOnlyList<Address>> FindAsync(System.Linq.Expressions.Expression<Func<Address, bool>> predicate, CancellationToken ct = default)
    {
        throw new NotSupportedException("Use explicit SQL queries.");
    }

    public Task<(IReadOnlyList<Address> Items, int TotalCount)> GetPagedAsync(System.Linq.Expressions.Expression<Func<Address, bool>> predicate, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        throw new NotSupportedException("Use GetPagedAsync(int, int).");
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(0);
    }

    public async Task<List<Address>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""address"" WHERE ""customerid"" = @CustomerId AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Address>(new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: ct))).ToList();
    }

    protected override string BuildInsertSql()
    {
        return @"INSERT INTO ""webshop"".""address"" (""customerid"", ""firstname"", ""lastname"", ""address1"", ""address2"", ""city"", ""zip"", 
        ""isactive"", ""created"", ""createdby"", ""updatedby"") VALUES (@CustomerId, @FirstName, @LastName, @Address1, @Address2, @City, @Zip, 
        @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy) RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"UPDATE ""webshop"".""address"" SET ""customerid"" = @CustomerId, ""firstname"" = @FirstName, ""lastname"" = @LastName, ""address1"" = @Address1, 
        ""address2"" = @Address2, ""city"" = @City, ""zip"" = @Zip, ""updated"" = @UpdatedAt, 
        ""updatedby"" = @UpdatedBy WHERE ""id"" = @Id AND ""isactive"" = true";
    }
}
