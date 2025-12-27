namespace WebShop.Util.Models;

/// <summary>
/// ASM service authentication headers.
/// </summary>
public class AsmServiceHeaders
{
    /// <summary>
    /// ASM authentication app ID header.
    /// </summary>
    public string AuthAppId { get; set; } = string.Empty;

    /// <summary>
    /// ASM authentication app secret header.
    /// </summary>
    public string AuthAppSecret { get; set; } = string.Empty;
}
