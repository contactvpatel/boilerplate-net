namespace WebShop.Api.Models;

/// <summary>
/// Query parameters for paginated list endpoints.
/// Use page=0 or omit to return all items (non-paginated); use page>=1 with pageSize for pagination.
/// </summary>
public class PaginationQuery
{
    /// <summary>
    /// Page number (1-based). Use 0 or omit for non-paginated results (returns all items).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page (1-100). Applied only when <see cref="Page"/> is greater than 0.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Whether the client requested pagination (Page &gt; 0).
    /// </summary>
    public bool IsPaginated => Page > 0;
}
