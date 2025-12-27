using System.Net;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Utility class for validating external service URLs to prevent SSRF (Server-Side Request Forgery) attacks.
/// </summary>
public static class UrlValidator
{
    /// <summary>
    /// Validates that a URL is safe for external service communication.
    /// Only allows HTTPS URLs to external (non-localhost) hosts.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="uri">The parsed URI if validation succeeds.</param>
    /// <returns>True if the URL is valid and safe, false otherwise.</returns>
    public static bool IsValidExternalUrl(string? url, out Uri? uri)
    {
        uri = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Parse the URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
        {
            return false;
        }

        // Security: Only allow HTTPS for external services
        if (uri.Scheme != "https")
        {
            uri = null;
            return false;
        }

        // Security: Prevent localhost/internal network access (SSRF protection)
        string host = uri.Host.ToLowerInvariant();

        // Block localhost variations
        if (host == "localhost" ||
            host == "127.0.0.1" ||
            host == "::1" ||
            host == "0.0.0.0")
        {
            uri = null;
            return false;
        }

        // Block private IP ranges (RFC 1918)
        if (host.StartsWith("192.168.") ||
            host.StartsWith("10.") ||
            host.StartsWith("172.") && IsPrivate172Range(host))
        {
            uri = null;
            return false;
        }

        // Block link-local addresses
        if (host.StartsWith("169.254."))
        {
            uri = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a host is in the 172.16.0.0/12 private range.
    /// </summary>
    private static bool IsPrivate172Range(string host)
    {
        // 172.16.0.0 to 172.31.255.255
        if (IPAddress.TryParse(host, out IPAddress? address))
        {
            byte[] bytes = address.GetAddressBytes();
            if (bytes.Length == 4 && bytes[0] == 172)
            {
                return bytes[1] >= 16 && bytes[1] <= 31;
            }
        }
        return false;
    }
}

