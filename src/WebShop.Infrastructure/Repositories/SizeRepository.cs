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
        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""sizelabel"" AS SizeLabel, 
            ""sizeus"" AS SizeUs, ""sizeuk"" AS SizeUk, ""sizeeu"" AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, 
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive 
            FROM ""webshop"".""sizes"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Size>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Size>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""sizelabel"" AS SizeLabel,
            ""sizeus"" AS SizeUs, ""sizeuk"" AS SizeUk, ""sizeeu"" AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
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
        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""sizelabel"" AS SizeLabel,
            ""sizeus"" AS SizeUs, ""sizeuk"" AS SizeUk, ""sizeeu"" AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive, COUNT(*) OVER() AS TotalCount
            FROM ""webshop"".""sizes"" WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(sql, new { Offset = offset, PageSize = pageSize })).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Size>(), 0);
        }

        int total = (int)((IDictionary<string, object>)results[0])["TotalCount"];
        return (results.Select(r => new Size
        {
            Id = (int)((IDictionary<string, object>)r)["Id"],
            Gender = ((IDictionary<string, object>)r)["Gender"] as string,
            Category = ((IDictionary<string, object>)r)["Category"] as string,
            SizeLabel = ((IDictionary<string, object>)r)["SizeLabel"] as string,
            SizeUs = ((IDictionary<string, object>)r)["SizeUs"] as string,
            SizeUk = ((IDictionary<string, object>)r)["SizeUk"] as string,
            SizeEu = ((IDictionary<string, object>)r)["SizeEu"] as string,
            CreatedAt = (DateTime)((IDictionary<string, object>)r)["CreatedAt"],
            CreatedBy = ((IDictionary<string, object>)r)["CreatedBy"] != null ? (int)((IDictionary<string, object>)r)["CreatedBy"] : 0,
            UpdatedAt = ((IDictionary<string, object>)r)["UpdatedAt"] as DateTime?,
            UpdatedBy = ((IDictionary<string, object>)r)["UpdatedBy"] != null ? (int)((IDictionary<string, object>)r)["UpdatedBy"] : 0,
            IsActive = (bool)((IDictionary<string, object>)r)["IsActive"]
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

    public Task<IReadOnlyList<Size>> FindAsync(System.Linq.Expressions.Expression<Func<Size, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use explicit SQL queries.");
    }

    public Task<(IReadOnlyList<Size> Items, int TotalCount)> GetPagedAsync(System.Linq.Expressions.Expression<Func<Size, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetPagedAsync(int, int).");
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public async Task<List<Size>> GetByGenderAndCategoryAsync(string gender, string category, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gender);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        const string sql = @"SELECT ""id"" AS Id, ""gender"" AS Gender, ""category"" AS Category, ""sizelabel"" AS SizeLabel,
            ""sizeus"" AS SizeUs, ""sizeuk"" AS SizeUk, ""sizeeu"" AS SizeEu, ""created"" AS CreatedAt, ""createdby"" AS CreatedBy,
            ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""sizes"" WHERE ""gender"" = @Gender AND ""category"" = @Category AND ""isactive"" = true ORDER BY ""sizelabel""";

        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Size>(new CommandDefinition(sql, new { Gender = gender, Category = category }, cancellationToken: cancellationToken))).ToList();
    }

    protected override string BuildInsertSql()
    {
        return @"INSERT INTO ""webshop"".""sizes"" (""gender"", ""category"", ""sizelabel"", ""sizeus"", ""sizeuk"", ""sizeeu"", ""isactive"", ""created"", ""createdby"", ""updatedby"") 
        VALUES (@Gender, @Category, @SizeLabel, @SizeUs, @SizeUk, @SizeEu, @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy) RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"UPDATE ""webshop"".""sizes"" SET ""gender"" = @Gender, ""category"" = @Category, ""sizelabel"" = @SizeLabel, 
        ""sizeus"" = @SizeUs, ""sizeuk"" = @SizeUk, ""sizeeu"" = @SizeEu, ""updated"" = @UpdatedAt, ""updatedby"" = @UpdatedBy WHERE ""id"" = @Id AND ""isactive"" = true";
    }
}
