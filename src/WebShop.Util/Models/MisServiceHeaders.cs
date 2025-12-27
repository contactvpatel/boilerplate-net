namespace WebShop.Util.Models;

/// <summary>
/// MIS service authentication headers.
/// </summary>
public class MisServiceHeaders
{
    /// <summary>
    /// MIS authentication app ID header.
    /// </summary>
    public string AuthAppId { get; set; } = string.Empty;

    /// <summary>
    /// MIS authentication app secret header.
    /// </summary>
    public string AuthAppSecret { get; set; } = string.Empty;
}
