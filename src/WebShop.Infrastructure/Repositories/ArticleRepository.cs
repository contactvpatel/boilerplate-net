using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories.Base;

namespace WebShop.Infrastructure.Repositories;

public class ArticleRepository : DapperRepositoryBase<Article>, IArticleRepository
{
    protected override string TableName => "articles";

    public ArticleRepository(IDapperConnectionFactory connectionFactory, IDapperTransactionManager? transactionManager = null, ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory) { }

    public async Task<Article?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""productid"" AS ProductId, ""ean"" AS Ean, ""colorid"" AS ColorId, ""size"" AS Size, ""description"" AS Description,
            ""originalprice"" AS OriginalPrice, ""reducedprice"" AS ReducedPrice, ""taxrate"" AS TaxRate, ""discountinpercent"" AS DiscountInPercent, ""currentlyactive"" AS CurrentlyActive,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive 
            FROM ""webshop"".""articles"" WHERE ""id"" = @Id AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Article>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""productid"" AS ProductId, ""ean"" AS Ean, ""colorid"" AS ColorId, ""size"" AS Size, ""description"" AS Description,
            ""originalprice"" AS OriginalPrice, ""reducedprice"" AS ReducedPrice, ""taxrate"" AS TaxRate, ""discountinpercent"" AS DiscountInPercent, ""currentlyactive"" AS CurrentlyActive,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""articles"" WHERE ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Article>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<(IReadOnlyList<Article> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = @"SELECT ""id"" AS Id, ""productid"" AS ProductId, ""ean"" AS Ean, ""colorid"" AS ColorId, ""size"" AS Size, ""description"" AS Description,
            ""originalprice"" AS OriginalPrice, ""reducedprice"" AS ReducedPrice, ""taxrate"" AS TaxRate, ""discountinpercent"" AS DiscountInPercent, ""currentlyactive"" AS CurrentlyActive,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive,
            COUNT(*) OVER() AS TotalCount FROM ""webshop"".""articles"" WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(sql, new { Offset = offset, PageSize = pageSize })).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Article>(), 0);
        }

        int total = (int)((IDictionary<string, object>)results[0])["TotalCount"];
        return (results.Select(r => new Article
        {
            Id = (int)((IDictionary<string, object>)r)["Id"],
            ProductId = ((IDictionary<string, object>)r)["ProductId"] as int?,
            Ean = ((IDictionary<string, object>)r)["Ean"] as string,
            ColorId = ((IDictionary<string, object>)r)["ColorId"] as int?,
            Size = ((IDictionary<string, object>)r)["Size"] as int?,
            Description = ((IDictionary<string, object>)r)["Description"] as string,
            OriginalPrice = ((IDictionary<string, object>)r)["OriginalPrice"] as decimal?,
            ReducedPrice = ((IDictionary<string, object>)r)["ReducedPrice"] as decimal?,
            TaxRate = ((IDictionary<string, object>)r)["TaxRate"] as decimal?,
            DiscountInPercent = ((IDictionary<string, object>)r)["DiscountInPercent"] as int?,
            CurrentlyActive = ((IDictionary<string, object>)r)["CurrentlyActive"] as bool?,
            CreatedAt = (DateTime)((IDictionary<string, object>)r)["CreatedAt"],
            CreatedBy = ((IDictionary<string, object>)r)["CreatedBy"] != null ? (int)((IDictionary<string, object>)r)["CreatedBy"] : 0,
            UpdatedAt = ((IDictionary<string, object>)r)["UpdatedAt"] as DateTime?,
            UpdatedBy = ((IDictionary<string, object>)r)["UpdatedBy"] != null ? (int)((IDictionary<string, object>)r)["UpdatedBy"] : 0,
            IsActive = (bool)((IDictionary<string, object>)r)["IsActive"]
        }).ToList(), total);
    }

    public async IAsyncEnumerable<Article> GetAllStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Article> items = await GetAllAsync(cancellationToken);
        foreach (Article item in items)
        {
            yield return item;
        }
    }

    public Task<IReadOnlyList<Article>> FindAsync(System.Linq.Expressions.Expression<Func<Article, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use explicit SQL queries.");
    }

    public Task<(IReadOnlyList<Article> Items, int TotalCount)> GetPagedAsync(System.Linq.Expressions.Expression<Func<Article, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetPagedAsync(int, int).");
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public async Task<List<Article>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""productid"" AS ProductId, ""ean"" AS Ean, ""colorid"" AS ColorId, ""size"" AS Size, ""description"" AS Description,
            ""originalprice"" AS OriginalPrice, ""reducedprice"" AS ReducedPrice, ""taxrate"" AS TaxRate, ""discountinpercent"" AS DiscountInPercent, ""currentlyactive"" AS CurrentlyActive,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""articles"" WHERE ""productid"" = @ProductId AND ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Article>(new CommandDefinition(sql, new { ProductId = productId }, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<List<Article>> GetActiveArticlesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"SELECT ""id"" AS Id, ""productid"" AS ProductId, ""ean"" AS Ean, ""colorid"" AS ColorId, ""size"" AS Size, ""description"" AS Description,
            ""originalprice"" AS OriginalPrice, ""reducedprice"" AS ReducedPrice, ""taxrate"" AS TaxRate, ""discountinpercent"" AS DiscountInPercent, ""currentlyactive"" AS CurrentlyActive,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""articles"" WHERE ""currentlyactive"" = true AND ""isactive"" = true ORDER BY ""id""";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Article>(new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();
    }

    public async Task<Article?> GetByEanAsync(string ean, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ean);
        const string sql = @"SELECT ""id"" AS Id, ""productid"" AS ProductId, ""ean"" AS Ean, ""colorid"" AS ColorId, ""size"" AS Size, ""description"" AS Description,
            ""originalprice"" AS OriginalPrice, ""reducedprice"" AS ReducedPrice, ""taxrate"" AS TaxRate, ""discountinpercent"" AS DiscountInPercent, ""currentlyactive"" AS CurrentlyActive,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""articles"" WHERE ""ean"" = @Ean AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Article>(new CommandDefinition(sql, new { Ean = ean }, cancellationToken: cancellationToken));
    }

    protected override string BuildInsertSql()
    {
        return @"INSERT INTO ""webshop"".""articles"" (""productid"", ""ean"", ""colorid"", ""size"", ""description"", ""originalprice"", ""reducedprice"", ""taxrate"", ""discountinpercent"", ""currentlyactive"", ""isactive"", ""created"", ""createdby"", ""updatedby"")
        VALUES (@ProductId, @Ean, @ColorId, @Size, @Description, @OriginalPrice, @ReducedPrice, @TaxRate, @DiscountInPercent, @CurrentlyActive, @IsActive, @CreatedAt, @CreatedBy, @UpdatedBy) RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"UPDATE ""webshop"".""articles"" SET ""productid"" = @ProductId, ""ean"" = @Ean, ""colorid"" = @ColorId, ""size"" = @Size, ""description"" = @Description, 
        ""originalprice"" = @OriginalPrice, ""reducedprice"" = @ReducedPrice, ""taxrate"" = @TaxRate, ""discountinpercent"" = @DiscountInPercent, ""currentlyactive"" = @CurrentlyActive, ""updated"" = @UpdatedAt, ""updatedby"" = @UpdatedBy WHERE ""id"" = @Id AND ""isactive"" = true";
    }
}
