namespace WebShop.Core.Models;

/// <summary>
/// Wrapper for MIS API responses that include success flag, data, and optional errors.
/// </summary>
/// <typeparam name="T">The type of items in the response data list.</typeparam>
public class MisResponse<T>
{
    /// <summary>Indicates whether the request succeeded.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Response payload when successful.</summary>
    public List<T>? Data { get; set; }

    /// <summary>Optional message from the API.</summary>
    public string? Message { get; set; }

    /// <summary>Errors when the request did not succeed.</summary>
    public List<MisErrorModel>? Errors { get; set; }
}

/// <summary>
/// Represents a single error in an MIS API response.
/// </summary>
public class MisErrorModel
{
    /// <summary>Error identifier.</summary>
    public string? ErrorId { get; set; }

    /// <summary>HTTP or application status code.</summary>
    public int StatusCode { get; set; }

    /// <summary>Human-readable error message.</summary>
    public string? Message { get; set; }
}
