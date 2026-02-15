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

    public async Task<Address?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""address"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Address>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Address>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive FROM ""webshop"".""address"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Address>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<(IReadOnlyList<Address> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive, COUNT(*) OVER() AS ""TotalCount"" FROM ""webshop"".""address"" 
            WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(sql, new { Offset = offset, PageSize = pageSize })).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Address>(), 0);
        }

        int total = Convert.ToInt32(GetDictValue((IDictionary<string, object>)results[0], "TotalCount"));
        return (results.Select(r =>
        {
            var d = (IDictionary<string, object>)r;
            return new Address
            {
                Id = Convert.ToInt32(GetDictValue(d, "Id")),
                CustomerId = GetDictValue(d, "CustomerId") != null ? Convert.ToInt32(GetDictValue(d, "CustomerId")) : null,
                FirstName = GetDictValue(d, "FirstName") as string,
                LastName = GetDictValue(d, "LastName") as string,
                Address1 = GetDictValue(d, "Address1") as string,
                Address2 = GetDictValue(d, "Address2") as string,
                City = GetDictValue(d, "City") as string,
                Zip = GetDictValue(d, "Zip") as string,
                CreatedAt = (DateTime)GetDictValue(d, "CreatedAt")!,
                CreatedBy = GetDictValue(d, "CreatedBy") != null ? Convert.ToInt32(GetDictValue(d, "CreatedBy")) : 0,
                UpdatedAt = GetDictValue(d, "UpdatedAt") as DateTime?,
                UpdatedBy = GetDictValue(d, "UpdatedBy") != null ? Convert.ToInt32(GetDictValue(d, "UpdatedBy")) : 0,
                IsActive = (bool)GetDictValue(d, "IsActive")!
            };
        }).ToList(), total);
    }

    public async IAsyncEnumerable<Address> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Address> items = await GetAllAsync(cancellationToken);
        foreach (Address item in items)
        {
            yield return item;
        }
    }

    public async Task<IReadOnlyList<Address>> FindByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Address>();
        }

        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""address"" WHERE ""id"" = ANY(@Ids) AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Address>(new CommandDefinition(sql, new { Ids = ids.ToArray() }, cancellationToken: cancellationToken))).ToList();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public async Task<List<Address>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""customerid"" AS CustomerId, ""firstname"" AS FirstName, ""lastname"" AS LastName, ""address1"" AS Address1, ""address2"" AS Address2,
            ""city"" AS City, ""zip"" AS Zip, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""address"" WHERE ""customerid"" = @CustomerId AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Address>(new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: cancellationToken))).ToList();
    }

    private static object? GetDictValue(IDictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out object? v) ? v : d.TryGetValue(key.ToLowerInvariant(), out v) ? v : null;
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
