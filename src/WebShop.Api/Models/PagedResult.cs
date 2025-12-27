namespace WebShop.Api.Models;

/// <summary>
/// Represents a paginated result set with metadata about the pagination.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    public PagedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Gets the index of the first item on the current page (1-based).
    /// </summary>
    public int FirstItemIndex => TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1;

    /// <summary>
    /// Gets the index of the last item on the current page (1-based).
    /// </summary>
    public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>An empty paged result.</returns>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 20)
    {
        return new PagedResult<T>(Array.Empty<T>(), pageNumber, pageSize, 0);
    }
}
