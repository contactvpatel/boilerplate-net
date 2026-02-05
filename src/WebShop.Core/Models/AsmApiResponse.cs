namespace WebShop.Core.Models;

/// <summary>
/// Response from ASM containing a user's application security (what they can access).
/// </summary>
public class AsmApiResponse
{
    /// <summary>The user's security entries: role/position and per-application access.</summary>
    public List<AsmResponseModel> Data { get; set; } = [];

    /// <summary>Error details if the request to ASM failed.</summary>
    public List<ApiErrorModel> Errors { get; set; } = [];

    /// <summary>Message from ASM (e.g. error or status).</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Whether the request to ASM succeeded.</summary>
    public bool Succeeded { get; set; }
}
