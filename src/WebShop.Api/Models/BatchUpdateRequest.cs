namespace WebShop.Api.Models;

/// <summary>
/// Request model for batch update operations.
/// </summary>
/// <typeparam name="T">The type of update data.</typeparam>
public class BatchUpdateRequest<T>
{
    /// <summary>
    /// The unique identifier of the entity to update.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The update data for the entity.
    /// </summary>
    public T Data { get; set; } = default!;
}
