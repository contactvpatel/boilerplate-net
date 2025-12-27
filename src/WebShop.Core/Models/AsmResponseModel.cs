namespace WebShop.Core.Models;

/// <summary>
/// Model representing application security information from the ASM service.
/// </summary>
public class AsmResponseModel
{
    /// <summary>
    /// Application identifier.
    /// </summary>
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>
    /// Application name.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// List of permissions/roles for this application.
    /// </summary>
    public List<string> Permissions { get; set; } = new List<string>();

    /// <summary>
    /// Indicates if the user has access to this application.
    /// </summary>
    public bool HasAccess { get; set; }
}

