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
/// Label repository using hybrid Dapper approach for optimal performance.
/// Direct Dapper mapping for reads, shared base class for writes.
/// </summary>
public class LabelRepository : DapperRepositoryBase<Label>, ILabelRepository
{
    protected override string TableName => "labels";

    public LabelRepository(
        IDapperConnectionFactory connectionFactory,
        IDapperTransactionManager? transactionManager = null,
        ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory)
    {
    }

    public async Task<Label?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""slugname"" AS SlugName,
                ""icon"" AS Icon,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""labels""
            WHERE ""id"" = @Id AND ""isactive"" = true";

        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Label>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Label>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""slugname"" AS SlugName,
                ""icon"" AS Icon,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""labels""
            WHERE ""isactive"" = true
            ORDER BY ""id""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Label> results = await connection.QueryAsync<Label>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return results.ToList();
    }

    public async Task<(IReadOnlyList<Label> Items, int TotalCount)> GetPagedAsync(
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
                ""slugname"" AS SlugName,
                ""icon"" AS Icon,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive,
                COUNT(*) OVER() AS TotalCount
            FROM ""webshop"".""labels""
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
            return (Array.Empty<Label>(), 0);
        }

        int totalCount = (int)((IDictionary<string, object>)resultList[0])["TotalCount"];
        List<Label> labels = resultList.Select(MapToLabel).ToList();
        return (labels, totalCount);
    }

    public async IAsyncEnumerable<Label> GetAllStreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""slugname"" AS SlugName,
                ""icon"" AS Icon,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""labels""
            WHERE ""isactive"" = true
            ORDER BY ""id""";

        using IDbConnection connection = GetReadConnection();
        IEnumerable<Label> results = await connection.QueryAsync<Label>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        foreach (Label label in results)
        {
            yield return label;
        }
    }

    public Task<IReadOnlyList<Label>> FindAsync(
        System.Linq.Expressions.Expression<Func<Label, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Dapper does not support LINQ expressions. " +
            "Use specific repository methods with explicit SQL queries.");
    }

    public Task<(IReadOnlyList<Label> Items, int TotalCount)> GetPagedAsync(
        System.Linq.Expressions.Expression<Func<Label, bool>> predicate,
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

    public async Task<Label?> GetBySlugNameAsync(string slugName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slugName);

        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""name"" AS Name,
                ""slugname"" AS SlugName,
                ""icon"" AS Icon,
                ""created"" AS CreatedAt,
                ""createdby"" AS CreatedBy,
                ""updated"" AS UpdatedAt,
                ""updatedby"" AS UpdatedBy,
                ""isactive"" AS IsActive
            FROM ""webshop"".""labels""
            WHERE ""slugname"" = @SlugName AND ""isactive"" = true";

        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Label>(
            new CommandDefinition(sql, new { SlugName = slugName }, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    protected override string BuildInsertSql()
    {
        return @"
            INSERT INTO ""webshop"".""labels"" (
                ""name"", ""slugname"", ""icon"",
                ""isactive"", ""created"", ""createdby"", ""updatedby""
            )
            VALUES (
                @Name, @SlugName, @Icon,
                @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy
            )
            RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"
            UPDATE ""webshop"".""labels""
            SET 
                ""name"" = @Name,
                ""slugname"" = @SlugName,
                ""icon"" = @Icon,
                ""updated"" = @UpdatedAt,
                ""updatedby"" = @UpdatedBy
            WHERE ""id"" = @Id AND ""isactive"" = true";
    }

    private static Label MapToLabel(dynamic row)
    {
        IDictionary<string, object> dict = (IDictionary<string, object>)row;
        return new Label
        {
            Id = (int)dict["Id"],
            Name = dict["Name"] != null ? (string)dict["Name"] : null,
            SlugName = dict["SlugName"] != null ? (string)dict["SlugName"] : null,
            Icon = dict["Icon"] != null ? (byte[])dict["Icon"] : null,
            CreatedAt = (DateTime)dict["CreatedAt"],
            CreatedBy = dict["CreatedBy"] != null ? (int)dict["CreatedBy"] : 0,
            UpdatedAt = dict["UpdatedAt"] != null ? (DateTime?)dict["UpdatedAt"] : null,
            UpdatedBy = dict["UpdatedBy"] != null ? (int)dict["UpdatedBy"] : 0,
            IsActive = (bool)dict["IsActive"]
        };
    }
}
