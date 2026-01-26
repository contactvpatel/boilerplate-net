# AllowedHosts Configuration Guide

This guide explains the `AllowedHosts` configuration in ASP.NET Core, its security importance, Microsoft's recommendations, and best practices for production deployments.

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [What is AllowedHosts?](#what-is-allowedhosts)
- [Security Risks](#security-risks)
- [Microsoft Guidelines](#microsoft-guidelines)
- [Configuration](#configuration)
- [Environment-Specific Settings](#environment-specific-settings)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)
- [Implementation Details](#implementation-details)

## Overview

`AllowedHosts` is a built-in ASP.NET Core security feature that validates the `Host` header in incoming HTTP requests. It prevents host header injection attacks and cache poisoning by ensuring requests only come from trusted hostnames.

**Why AllowedHosts exists:**

- Prevents host header injection attacks
- Protects against cache poisoning
- Ensures requests are routed to the correct application
- Validates that requests are intended for your application

**Implementation Location:**

- Configuration: `appsettings.json` (or environment-specific files)
- Framework: Built into ASP.NET Core (automatically applied)
- Middleware: `HostFilteringMiddleware` (automatically registered)

## What is AllowedHosts?

`AllowedHosts` validates the `Host` HTTP header value against a whitelist of allowed hostnames. When a request arrives with a `Host` header that doesn't match the allowed list, ASP.NET Core returns a `400 Bad Request` response.

### How It Works

1. Client sends HTTP request with `Host` header: `Host: api.example.com`
2. ASP.NET Core checks if `api.example.com` is in the `AllowedHosts` list
3. If allowed: Request proceeds normally
4. If not allowed: Returns `400 Bad Request` with message "Invalid Host header"

### Example Request

```http
GET /api/v1/customers HTTP/1.1
Host: api.example.com
Authorization: Bearer <token>
```

If `api.example.com` is not in `AllowedHosts`, the request is rejected.

## Security Risks

### 1. Host Header Injection

**Attack Scenario:**

- Attacker sends request with malicious `Host` header: `Host: evil.com`
- If `AllowedHosts` is `"*"`, the request is accepted
- Application may generate URLs or redirects using the malicious host
- Users could be redirected to attacker's site

**Impact:**

- Phishing attacks
- Session hijacking
- Password reset token theft
- Open redirect vulnerabilities

### 2. Cache Poisoning

**Attack Scenario:**

- Attacker sends request with `Host: victim.com`
- Response is cached by proxy/CDN with wrong host
- Legitimate users receive poisoned cache entries
- Sensitive data may be exposed

**Impact:**

- Data leakage
- XSS attacks via cached responses
- Cache manipulation

### 3. Password Reset Poisoning

**Attack Scenario:**

- Attacker requests password reset with `Host: attacker.com`
- Application generates reset link using `Host` header
- Reset link points to attacker's domain
- Attacker intercepts password reset tokens

**Impact:**

- Account takeover
- Unauthorized access
- Data breach

## Microsoft Guidelines

According to Microsoft's official documentation and security best practices:

### ✅ **DO:**

1. **Always specify exact hostnames in production**
   - Never use `"*"` in production
   - List all valid hostnames explicitly
   - Include variations (with/without www, subdomains)

2. **Use environment-specific configuration**
   - Development: Can use `"*"` for convenience
   - Production: Must specify exact hostnames

3. **Include all valid hostname variations**
   - `api.example.com`
   - `www.api.example.com`
   - `api-staging.example.com` (if applicable)
   - Internal hostnames (if behind load balancer)

4. **Store configuration in secure vault**
   - Use Vault for production environments
   - Don't commit production hostnames to source control

### ❌ **DON'T:**

1. **Never use `"*"` in production**
   - Security vulnerability
   - Violates Microsoft security guidelines
   - Allows any host header

2. **Don't rely on default behavior**
   - Explicitly configure `AllowedHosts`
   - Don't assume it's secure by default

3. **Don't include untrusted domains**
   - Only include domains you control
   - Don't allow third-party domains

## Configuration

### Configuration Format

`AllowedHosts` can be configured as:

1. **Single hostname** (string):

   ```json
   "AllowedHosts": "api.example.com"
   ```

2. **Multiple hostnames** (semicolon-separated string):

   ```json
   "AllowedHosts": "api.example.com;www.api.example.com;api-staging.example.com"
   ```

3. **Wildcard** (development only):

   ```json
   "AllowedHosts": "*"
   ```

### Configuration Files

#### Base Configuration (`appsettings.json`)

```json
{
  "AllowedHosts": "*"
}
```

**Note:** Base configuration should use `"*"` for development convenience. Production overrides should be in environment-specific files or Vault.

#### Development Configuration (`appsettings.Development.json`)

```json
{
  "AllowedHosts": "*"
}
```

**Rationale:** Development environments can use wildcard for convenience when testing with different hostnames (localhost, 127.0.0.1, etc.).

#### Production Configuration (Vault)

```json
{
  "AllowedHosts": "api.example.com;www.api.example.com;api-staging.example.com"
}
```

**Rationale:** Production must specify exact hostnames. Multiple hostnames are separated by semicolons.

## Environment-Specific Settings

### Local Development

**Configuration:**

```json
{
  "AllowedHosts": "*"
}
```

**Rationale:**

- Allows testing with `localhost`, `127.0.0.1`, `localhost:5000`, etc.
- Convenient for local development
- No security risk in isolated development environment

### Dev/QA/UAT/Production

**Configuration (in Vault):**

```json
{
  "AllowedHosts": "api-dev.example.com;api-qa.example.com;api-uat.example.com;api.example.com;www.api.example.com"
}
```

**Rationale:**

- Each environment has specific hostnames
- Must specify exact hostnames (no wildcards)
- Store in Vault for security and environment-specific management

### Behind Load Balancer / Reverse Proxy

**Configuration:**

```json
{
  "AllowedHosts": "api.example.com;internal-api.example.com;10.0.0.5"
}
```

**Important Notes:**

- Include internal hostnames if load balancer forwards original host
- Include IP addresses if direct IP access is needed
- Consider using `ForwardedHeaders` middleware for proper host detection

## Best Practices

### 1. **Never Use Wildcard in Production**

❌ **Bad:**

```json
{
  "AllowedHosts": "*"
}
```

✅ **Good:**

```json
{
  "AllowedHosts": "api.example.com;www.api.example.com"
}
```

### 2. **Include All Valid Hostname Variations**

✅ **Good:**

```json
{
  "AllowedHosts": "api.example.com;www.api.example.com;api-staging.example.com"
}
```

**Include:**

- Primary domain: `api.example.com`
- WWW variant: `www.api.example.com`
- Staging/QA variants: `api-staging.example.com`
- Internal hostnames (if applicable)

### 3. **Use Environment-Specific Configuration**

✅ **Good:**

- Local Development: `"*"` in `appsettings.Development.json`
- Dev/QA/UAT/Production: Specific hostnames in Vault

### 4. **Store Production Config in Vault**

✅ **Good:**

- Store production `AllowedHosts` in Vault
- Don't commit production hostnames to source control
- Use environment variables or secure configuration management

### 5. **Document Allowed Hostnames**

✅ **Good:**

- Document why each hostname is allowed
- Review and update regularly
- Remove unused hostnames

### 6. **Test Configuration Changes**

✅ **Good:**

- Test in staging before production
- Verify all valid hostnames work
- Ensure invalid hostnames are rejected

### 7. **Monitor Rejected Requests**

✅ **Good:**

- Log `400 Bad Request` responses with "Invalid Host header"
- Monitor for attack patterns
- Alert on suspicious activity

## Troubleshooting

### Error: "Invalid Host header"

**Symptom:**

```
HTTP 400 Bad Request
Invalid Host header
```

**Causes:**

1. `Host` header doesn't match `AllowedHosts`
2. Missing hostname in configuration
3. Typo in hostname configuration

**Solutions:**

1. Verify the `Host` header in the request
2. Check `AllowedHosts` configuration
3. Ensure hostname matches exactly (case-insensitive)
4. Add missing hostname to configuration

### Requests Work in Development but Fail in Production

**Symptom:**

- Requests work locally but fail in production

**Causes:**

1. Development uses `"*"` (allows any host)
2. Production has specific hostnames
3. Production hostname not in `AllowedHosts`

**Solutions:**

1. Verify production hostname matches `AllowedHosts`
2. Check environment-specific configuration
3. Ensure Vault configuration is correct
4. Test with exact production hostname

### Load Balancer / Reverse Proxy Issues

**Symptom:**

- Requests fail behind load balancer
- Internal hostnames not recognized

**Causes:**

1. Load balancer forwards different `Host` header
2. Internal hostname not in `AllowedHosts`
3. `ForwardedHeaders` middleware not configured

**Solutions:**

1. Include internal hostname in `AllowedHosts`
2. Configure `ForwardedHeaders` middleware:

   ```csharp
   builder.Services.Configure<ForwardedHeadersOptions>(options =>
   {
       options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
   });
   ```

3. Verify load balancer configuration

### Health Check Endpoints Failing

**Symptom:**

- Health check endpoints return `400 Bad Request`

**Causes:**

1. Health checks use different hostname
2. Load balancer health checks use IP address
3. Internal health check hostname not allowed

**Solutions:**

1. Include health check hostname in `AllowedHosts`
2. Use IP address if health checks use IP
3. Configure health checks to use allowed hostname

## Implementation Details

### How ASP.NET Core Uses AllowedHosts

ASP.NET Core automatically reads `AllowedHosts` from configuration and applies `HostFilteringMiddleware`:

1. **Configuration Binding:**

   ```csharp
   // Automatically read from IConfiguration
   var allowedHosts = configuration["AllowedHosts"];
   ```

2. **Middleware Registration:**

   ```csharp
   // Automatically registered by framework
   app.UseHostFiltering();
   ```

3. **Validation:**
   - Compares request `Host` header against allowed list
   - Case-insensitive comparison
   - Returns `400 Bad Request` if not allowed

### Configuration Priority

Configuration is loaded in this order (later overrides earlier):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. Command-line arguments
5. Vault (if configured)

### Case Sensitivity

Hostname comparison is **case-insensitive**:

- `api.example.com` matches `API.EXAMPLE.COM`
- `Api.Example.Com` matches `api.example.com`

### Port Numbers

Port numbers are **included** in comparison:

- `api.example.com:5000` is different from `api.example.com:5001`
- `api.example.com` (default port) is different from `api.example.com:443`

**Recommendation:** If using standard ports (80 for HTTP, 443 for HTTPS), you can omit the port number in configuration. ASP.NET Core will match both with and without port.

## Related Documentation

- [Microsoft Documentation: Host Filtering](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/host-filtering)
- [OWASP: Host Header Injection](https://owasp.org/www-community/attacks/HTTP_Host_Header_Injection)
- [CORS Configuration Guide](cors.md) - Related security configuration

## Summary

The `AllowedHosts` configuration is a critical security feature that:

- ✅ Prevents host header injection attacks
- ✅ Protects against cache poisoning
- ✅ Validates requests are intended for your application
- ✅ Must be configured explicitly in production
- ✅ Should never use `"*"` in production environments
- ✅ Should include all valid hostname variations
- ✅ Should be stored in Vault for production

**Key Takeaways:**

1. **Development:** Use `"*"` for convenience
2. **Production:** Always specify exact hostnames (never `"*"`)
3. **Include all variations:** www, non-www, subdomains
4. **Store in Vault:** Don't commit production hostnames
5. **Monitor and review:** Regularly audit allowed hostnames
