using System.ComponentModel.DataAnnotations;

namespace WebShop.Util.Models;

/// <summary>
/// ASM service endpoint paths.
/// </summary>
public class AsmServiceEndpoints
{
    /// <summary>
    /// Endpoint for application security.
    /// </summary>
    [Required]
    public string ApplicationSecurity { get; set; } = string.Empty;
}
