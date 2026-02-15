using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories.Base;

namespace WebShop.Infrastructure.Repositories;

public class SizeRepository : DapperRepositoryBase<Size>, ISizeRepository
{
    protected override string TableName => "sizes";

    public SizeRepository(IDapperConnectionFactory connectionFactory, IDapperTransactionManager? transactionManager = null, ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory) { }

    public async Task<Size?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""size"" AS SizeLabel, 
            ""size_us""::text AS SizeUs, ""size_uk""::text AS SizeUk, ""size_eu""::text AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, 
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive 
            FROM ""webshop"".""sizes"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Size>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Size>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""size"" AS SizeLabel,
            ""size_us""::text AS SizeUs, ""size_uk""::text AS SizeUk, ""size_eu""::text AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""sizes"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Size>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<(IReadOnlyList<Size> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""size"" AS SizeLabel,
            ""size_us""::text AS SizeUs, ""size_uk""::text AS SizeUk, ""size_eu""::text AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive, COUNT(*) OVER() AS ""TotalCount""
            FROM ""webshop"".""sizes"" WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(new CommandDefinition(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken))).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Size>(), 0);
        }

        int total = Convert.ToInt32(GetDictValue((IDictionary<string, object>)results[0], "TotalCount"));
        return (results.Select(r =>
        {
            var d = (IDictionary<string, object>)r;
            return new Size
            {
                Id = Convert.ToInt32(GetDictValue(d, "Id")),
                Gender = GetDictValue(d, "Gender") as string,
                Category = GetDictValue(d, "Category") as string,
                SizeLabel = GetDictValue(d, "SizeLabel") as string,
                SizeUs = GetDictValue(d, "SizeUs") as string,
                SizeUk = GetDictValue(d, "SizeUk") as string,
                SizeEu = GetDictValue(d, "SizeEu") as string,
                CreatedAt = (DateTime)GetDictValue(d, "CreatedAt")!,
                CreatedBy = GetDictValue(d, "CreatedBy") != null ? Convert.ToInt32(GetDictValue(d, "CreatedBy")) : 0,
                UpdatedAt = GetDictValue(d, "UpdatedAt") as DateTime?,
                UpdatedBy = GetDictValue(d, "UpdatedBy") != null ? Convert.ToInt32(GetDictValue(d, "UpdatedBy")) : 0,
                IsActive = (bool)GetDictValue(d, "IsActive")!
            };
        }).ToList(), total);
    }

    public async IAsyncEnumerable<Size> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Size> items = await GetAllAsync(cancellationToken);
        foreach (Size item in items)
        {
            yield return item;
        }
    }

    public async Task<IReadOnlyList<Size>> FindByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Size>();
        }

        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""size"" AS SizeLabel,
            ""size_us""::text AS SizeUs, ""size_uk""::text AS SizeUk, ""size_eu""::text AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""sizes"" WHERE ""id"" = ANY(@Ids) AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Size>(new CommandDefinition(sql, new { Ids = ids.ToArray() }, cancellationToken: cancellationToken))).ToList();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public async Task<List<Size>> GetByGenderAndCategoryAsync(string gender, string category, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gender);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""size"" AS SizeLabel,
            ""size_us""::text AS SizeUs, ""size_uk""::text AS SizeUk, ""size_eu""::text AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""sizes"" WHERE ""gender"" = CAST(@Gender AS public.gender) AND ""category"" = CAST(@Category AS public.category) AND ""isactive"" = true ORDER BY ""size""";

        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Size>(new CommandDefinition(sql, new { Gender = gender, Category = category }, cancellationToken: cancellationToken))).ToList();
    }

    protected override string BuildInsertSql()
    {
        return @"INSERT INTO ""webshop"".""sizes"" (""gender"", ""category"", ""size"", ""size_us"", ""size_uk"", ""size_eu"", ""isactive"", ""created"", ""createdby"", ""updatedby"") 
        VALUES (CAST(@Gender AS public.gender), CAST(@Category AS public.category), @SizeLabel, CAST(@SizeUs AS int4range), CAST(@SizeUk AS int4range), CAST(@SizeEu AS int4range), @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy) RETURNING ""id""";
    }

    private static object? GetDictValue(IDictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out object? v) ? v : d.TryGetValue(key.ToLowerInvariant(), out v) ? v : null;
    }

    protected override string BuildUpdateSql()
    {
        return @"UPDATE ""webshop"".""sizes"" SET ""gender"" = CAST(@Gender AS public.gender), ""category"" = CAST(@Category AS public.category), ""size"" = @SizeLabel, 
        ""size_us"" = CAST(@SizeUs AS int4range), ""size_uk"" = CAST(@SizeUk AS int4range), ""size_eu"" = CAST(@SizeEu AS int4range), ""updated"" = @UpdatedAt, ""updatedby"" = @UpdatedBy WHERE ""id"" = @Id AND ""isactive"" = true";
    }
}
