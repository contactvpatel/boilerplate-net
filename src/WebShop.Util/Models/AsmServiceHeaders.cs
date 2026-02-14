using System.ComponentModel.DataAnnotations;

namespace WebShop.Util.Models;

/// <summary>
/// ASM service authentication headers.
/// </summary>
public class AsmServiceHeaders
{
    /// <summary>
    /// ASM authentication app ID header.
    /// </summary>
    [Required]
    public string AuthAppId { get; set; } = string.Empty;

    /// <summary>
    /// ASM authentication app secret header.
    /// </summary>
    [Required]
    public string AuthAppSecret { get; set; } = string.Empty;
}
