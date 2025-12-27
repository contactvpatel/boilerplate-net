namespace WebShop.Core.Interfaces.Base;

/// <summary>
/// Interface for accessing the current authenticated user's context information.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the current user's ID from the JWT token.
    /// </summary>
    /// <returns>The user ID, or null if not available.</returns>
    string? GetUserId();

    /// <summary>
    /// Gets the current user's JWT token.
    /// </summary>
    /// <returns>The JWT token, or null if not available.</returns>
    string? GetToken();
}
