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
/// Color repository using hybrid Dapper approach for optimal performance.
/// Direct Dapper mapping for reads, shared base class for writes.
/// </summary>
public class ColorRepository : DapperRepositoryBase<Color>, IColorRepository
{
    protected override string TableName => "colors";

    public ColorRepository(
        IDapperConnectionFactory connectionFactory,
        IDapperTransactionManager? transactionManager = null,
        ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory)
    {
    }

    public async Task<Color?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""rgb"" AS Rgb,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""colors""
            WHERE ""id"" = @Id AND ""isactive"" = true";

        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Color>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Color>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""rgb"" AS Rgb,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""colors""
            WHERE ""isactive"" = true
            ORDER BY ""id""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Color> results = await connection.QueryAsync<Color>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return results.ToList();
    }

    public async Task<(IReadOnlyList<Color> Items, int TotalCount)> GetPagedAsync(
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
                ""rgb"" AS Rgb,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive,
                COUNT(*) OVER() AS TotalCount
            FROM ""webshop"".""colors""
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
            return (Array.Empty<Color>(), 0);
        }

        int totalCount = (int)((IDictionary<string, object>)resultList[0])["TotalCount"];
        List<Color> colors = resultList.Select(MapToColor).ToList();
        return (colors, totalCount);
    }

    public async IAsyncEnumerable<Color> GetAllStreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""rgb"" AS Rgb,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""colors""
            WHERE ""isactive"" = true
            ORDER BY ""id""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Color> results = await connection.QueryAsync<Color>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        foreach (Color color in results)
        {
            yield return color;
        }
    }

    public Task<IReadOnlyList<Color>> FindAsync(
        System.Linq.Expressions.Expression<Func<Color, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Dapper does not support LINQ expressions. " +
            "Use specific repository methods with explicit SQL queries.");
    }

    public Task<(IReadOnlyList<Color> Items, int TotalCount)> GetPagedAsync(
        System.Linq.Expressions.Expression<Func<Color, bool>> predicate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Dapper does not support LINQ expressions for pagination filtering. " +
            "Use GetPagedAsync(int pageNumber, int pageSize) for simple pagination.");
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public async Task<Color?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""rgb"" AS Rgb,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""colors""
            WHERE ""name"" = @Name AND ""isactive"" = true";

        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Color>(
            new CommandDefinition(sql, new { Name = name }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    protected override string BuildInsertSql()
    {
        return @"
            INSERT INTO ""webshop"".""colors"" (
                ""name"", ""rgb"",
                ""isactive"", ""created"", ""createdby"", ""updatedby""
            )
            VALUES (
                @Name, @Rgb,
                @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy
            )
            RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"
            UPDATE ""webshop"".""colors""
            SET 
                ""name"" = @Name,
                ""rgb"" = @Rgb,
                ""updated"" = @UpdatedAt,
                ""updatedby"" = @UpdatedBy
            WHERE ""id"" = @Id AND ""isactive"" = true";
    }

    private static Color MapToColor(dynamic row)
    {
        IDictionary<string, object> dict = (IDictionary<string, object>)row;
        return new Color
        {
            Id = (int)dict["Id"],
            Name = dict["Name"] != null ? (string)dict["Name"] : null,
            Rgb = dict["Rgb"] != null ? (string)dict["Rgb"] : null,
            CreatedAt = (DateTime)dict["CreatedAt"],
            CreatedBy = dict["CreatedBy"] != null ? (int)dict["CreatedBy"] : 0,
            UpdatedAt = dict["UpdatedAt"] != null ? (DateTime?)dict["UpdatedAt"] : null,
            UpdatedBy = dict["UpdatedBy"] != null ? (int)dict["UpdatedBy"] : 0,
            IsActive = (bool)dict["IsActive"]
        };
    }
}
