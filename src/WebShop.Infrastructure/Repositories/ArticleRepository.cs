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
            COUNT(*) OVER() AS ""TotalCount"" FROM ""webshop"".""articles"" WHERE ""isactive"" = true ORDER BY ""id"" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        using IDbConnection connection = GetReadConnection();
        List<dynamic> results = (await connection.QueryAsync(new CommandDefinition(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken))).ToList();
        if (results.Count == 0)
        {
            return (Array.Empty<Article>(), 0);
        }

        int total = Convert.ToInt32(GetDictValue((IDictionary<string, object>)results[0], "TotalCount"));
        return (results.Select(r =>
        {
            var d = (IDictionary<string, object>)r;
            return new Article
            {
                Id = Convert.ToInt32(GetDictValue(d, "Id")),
                ProductId = GetDictValue(d, "ProductId") != null ? Convert.ToInt32(GetDictValue(d, "ProductId")) : null,
                Ean = GetDictValue(d, "Ean") as string,
                ColorId = GetDictValue(d, "ColorId") != null ? Convert.ToInt32(GetDictValue(d, "ColorId")) : null,
                Size = GetDictValue(d, "Size") != null ? Convert.ToInt32(GetDictValue(d, "Size")) : null,
                Description = GetDictValue(d, "Description") as string,
                OriginalPrice = GetDictValue(d, "OriginalPrice") as decimal?,
                ReducedPrice = GetDictValue(d, "ReducedPrice") as decimal?,
                TaxRate = GetDictValue(d, "TaxRate") as decimal?,
                DiscountInPercent = GetDictValue(d, "DiscountInPercent") != null ? Convert.ToInt32(GetDictValue(d, "DiscountInPercent")) : null,
                CurrentlyActive = GetDictValue(d, "CurrentlyActive") as bool?,
                CreatedAt = (DateTime)GetDictValue(d, "CreatedAt")!,
                CreatedBy = GetDictValue(d, "CreatedBy") != null ? Convert.ToInt32(GetDictValue(d, "CreatedBy")) : 0,
                UpdatedAt = GetDictValue(d, "UpdatedAt") as DateTime?,
                UpdatedBy = GetDictValue(d, "UpdatedBy") != null ? Convert.ToInt32(GetDictValue(d, "UpdatedBy")) : 0,
                IsActive = (bool)GetDictValue(d, "IsActive")!
            };
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

    public async Task<IReadOnlyList<Article>> FindByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<Article>();
        }

        const string sql = @"SELECT ""id"" AS Id, ""productid"" AS ProductId, ""ean"" AS Ean, ""colorid"" AS ColorId, ""size"" AS Size, ""description"" AS Description,
            ""originalprice"" AS OriginalPrice, ""reducedprice"" AS ReducedPrice, ""taxrate"" AS TaxRate, ""discountinpercent"" AS DiscountInPercent, ""currentlyactive"" AS CurrentlyActive,
            ""created"" AS CreatedAt, ""createdby"" AS CreatedBy, ""updated"" AS UpdatedAt, ""updatedby"" AS UpdatedBy, ""isactive"" AS IsActive
            FROM ""webshop"".""articles"" WHERE ""id"" = ANY(@Ids) AND ""isactive"" = true";
        using IDbConnection connection = GetReadConnection();
        return (await connection.QueryAsync<Article>(new CommandDefinition(sql, new { Ids = ids.ToArray() }, cancellationToken: cancellationToken))).ToList();
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

    private static object? GetDictValue(IDictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out object? v) ? v : d.TryGetValue(key.ToLowerInvariant(), out v) ? v : null;
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
