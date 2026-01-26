# Response Compression Guidelines

This document provides comprehensive guidelines for configuring and optimizing response compression in the WebShop API application, following Microsoft best practices and industry standards.

[← Back to README](../../README.md)

## Table of Contents

1. [Overview](#overview)
2. [Why Response Compression?](#why-response-compression)
3. [Compression Algorithms](#compression-algorithms)
4. [Configuration](#configuration)
5. [Implementation Details](#implementation-details)
6. [Middleware Pipeline Order](#middleware-pipeline-order)
7. [Performance Considerations](#performance-considerations)
8. [Best Practices](#best-practices)
9. [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)
10. [Examples](#examples)

---

## Overview

Response compression reduces the size of HTTP responses by compressing content before sending it to clients. This significantly improves:

- **Bandwidth Usage**: Reduces data transfer by 60-90% for text-based content
- **Response Times**: Faster downloads, especially on slower connections
- **User Experience**: Faster page loads and API responses
- **Server Costs**: Lower bandwidth costs

The WebShop API implements response compression using **Brotli** (preferred) and **Gzip** (fallback) algorithms, following Microsoft's recommended configuration.

---

## Why Response Compression?

### Benefits

1. **Reduced Bandwidth**:
   - JSON responses: 60-80% size reduction
   - XML responses: 70-85% size reduction
   - HTML/text: 70-90% size reduction

2. **Faster Response Times**:
   - Smaller payloads = faster transmission
   - Especially beneficial for mobile networks and slower connections
   - Reduces Time to First Byte (TTFB) impact

3. **Better User Experience**:
   - Faster API responses
   - Reduced latency
   - Lower data usage for mobile clients

4. **Cost Savings**:
   - Lower bandwidth costs
   - Reduced CDN costs (if using CDN)
   - Lower infrastructure costs

### When Compression Helps Most

- ✅ **Text-based content**: JSON, XML, HTML, CSS, JavaScript
- ✅ **Large responses**: Lists, search results, reports
- ✅ **API responses**: Most API endpoints return JSON/XML
- ✅ **Slow connections**: Mobile networks, international users

### When Compression Doesn't Help

- ❌ **Already compressed content**: Images (PNG, JPEG, GIF), videos, audio
- ❌ **Small responses**: < 1 KB (compression overhead not worth it)
- ❌ **Binary content**: Already optimized formats

---

## Compression Algorithms

### Brotli (Preferred) ✅

**Brotli** is the preferred compression algorithm:

- **Better Compression Ratio**: 15-25% better than Gzip
- **Modern Standard**: Supported by all modern browsers (since 2016)
- **Optimal Performance**: Better balance of compression ratio vs. CPU usage
- **HTTP/2 Compatible**: Works seamlessly with HTTP/2

**Browser Support:**

- Chrome 50+ (2016)
- Firefox 44+ (2016)
- Edge 15+ (2017)
- Safari 11+ (2017)
- All modern mobile browsers

**Compression Levels:**

- Range: 0-11
- **Recommended**: 4 (optimal balance)
- **Fast**: 0-3 (lower CPU, less compression)
- **Best**: 8-11 (higher CPU, maximum compression)

### Gzip (Fallback) ✅

**Gzip** is used as a fallback for older clients:

- **Universal Support**: Supported by all browsers (since 1990s)
- **Good Compression**: 60-80% size reduction
- **Mature**: Well-tested and optimized
- **Backward Compatible**: Works with legacy clients

**Compression Levels:**

- Range: 0-9
- **Recommended**: 4 (optimal balance)
- **Fast**: 0-3 (lower CPU, less compression)
- **Best**: 7-9 (higher CPU, maximum compression)

### Algorithm Selection

The application automatically selects the best algorithm based on client support:

1. **Client sends `Accept-Encoding: br, gzip`** → Server uses **Brotli**
2. **Client sends `Accept-Encoding: gzip`** → Server uses **Gzip**
3. **Client sends no encoding** → Server sends uncompressed response

**Priority Order:**

1. Brotli (if client supports it)
2. Gzip (if client supports it)
3. Uncompressed (if client doesn't support compression)

---

## Configuration

### Configuration Model

Response compression is configured via `ResponseCompressionSettings`:

```json
{
  "ResponseCompressionOptions": {
    "Enabled": true,
    "MinimumResponseSizeBytes": 1024,
    "BrotliCompressionLevel": 4,
    "GzipCompressionLevel": 4,
    "UseBrotli": true,
    "UseGzip": true,
    "AdditionalMimeTypes": []
  }
}
```

**Note:** `EnableForHttps` is not in configuration because:

- The application **blocks all HTTP requests** (returns 400 Bad Request)
- All requests reaching compression middleware are **already HTTPS**
- Compression is **always enabled for HTTPS** (hardcoded to `true`)

### Configuration Properties

#### `Enabled` (bool)

- **Default**: `true`
- **Description**: Master switch to enable/disable response compression
- **Use Case**: Disable for debugging or if compression is handled by reverse proxy/CDN

#### `EnableForHttps` (Internal)

- **Value**: Always `true` (hardcoded)
- **Description**: Compression is always enabled for HTTPS connections
- **Rationale**: Since the application blocks all HTTP requests (doesn't redirect), all requests are HTTPS
- **Implementation**: Set to `true` in `ResponseCompressionExtensions.ConfigureResponseCompression()`
- **Note**: This setting is not configurable because HTTP requests are blocked, not redirected

#### `MinimumResponseSizeBytes` (int)

- **Default**: `1024` (1 KB)
- **Description**: Minimum response size before compression is applied
- **Rationale**: Small responses don't benefit from compression (overhead > savings)
- **Microsoft Recommendation**: 860-1024 bytes
- **Tuning**:
  - Lower (512-860): Compress more responses, slightly more CPU
  - Higher (2048+): Less CPU, but miss compressing some medium responses

#### `BrotliCompressionLevel` (int)

- **Default**: `4`
- **Range**: 0-11
- **Description**: Brotli compression level (higher = better compression, more CPU)
- **Microsoft Recommendation**: 4 (optimal balance)
- **Levels**:
  - `0-3`: Fast (lower CPU, less compression)
  - `4`: **Optimal** (recommended)
  - `5-7`: Balanced (good compression, moderate CPU)
  - `8-11`: Best (maximum compression, high CPU)

#### `GzipCompressionLevel` (int)

- **Default**: `4`
- **Range**: 0-9
- **Description**: Gzip compression level (higher = better compression, more CPU)
- **Microsoft Recommendation**: 4 (optimal balance)
- **Levels**:
  - `0-3`: Fast (lower CPU, less compression)
  - `4`: **Optimal** (recommended)
  - `5-7`: Balanced (good compression, moderate CPU)
  - `8-9`: Best (maximum compression, high CPU)

#### `UseBrotli` (bool)

- **Default**: `true`
- **Description**: Enable Brotli compression provider
- **Recommendation**: ✅ **Always enable** (better compression ratio)

#### `UseGzip` (bool)

- **Default**: `true`
- **Description**: Enable Gzip compression provider (fallback)
- **Recommendation**: ✅ **Always enable** (backward compatibility)

#### `AdditionalMimeTypes` (List<string>)

- **Default**: `[]` (empty)
- **Description**: Additional MIME types to compress beyond defaults
- **Default MIME Types** (already included):
  - `text/*` (all text types)
  - `application/json`
  - `application/xml`
  - `application/javascript`
  - `text/css`
  - `text/html`
  - `text/plain`
  - `text/xml`
- **Example**: `["application/vnd.api+json", "application/hal+json"]`

---

## Implementation Details

### Service Registration

Response compression is configured in `Core/ServiceExtensions.cs`:

```csharp
services.ConfigureResponseCompression(configuration);
```

### Extension Method

The `ConfigureResponseCompression` extension method:

1. **Binds Configuration**: Reads `ResponseCompressionOptions` from `appsettings.json`
2. **Registers Providers**: Adds Brotli and/or Gzip providers
3. **Configures MIME Types**: Sets which content types to compress
4. **Excludes Already Compressed**: Skips images, videos, audio, fonts
5. **Sets Compression Levels**: Configures optimal compression levels

### Excluded MIME Types

The following MIME types are **automatically excluded** from compression (already compressed or binary):

- `image/*` (PNG, JPEG, GIF, WebP, SVG)
- `video/*` (all video formats)
- `audio/*` (all audio formats)
- `font/*` (all font formats)
- `application/zip`, `application/gzip`, `application/x-gzip`
- `application/x-compress`, `application/x-compressed`
- `application/x-bzip2`

**Rationale**: These formats are already compressed or binary, so additional compression provides no benefit and wastes CPU.

---

## Middleware Pipeline Order

### Critical: Placement Matters

Response compression middleware **MUST** be placed early in the pipeline, **before** routing and other middleware that might modify responses.

### Correct Order

```csharp
app.EnforceHttps();               // 1. HTTPS enforcement (blocks HTTP, returns 400)
app.UseResponseCompression();     // 2. Response compression (EARLY! - all requests are HTTPS)
app.UseExceptionHandling();       // 3. Exception handling
app.UseCors();                    // 4. CORS
app.UseAuthentication();          // 5. Authentication
app.UseAuthorization();           // 6. Authorization
app.MapControllers();             // 7. Routing
```

**Note:** Since `EnforceHttps()` blocks HTTP requests (doesn't redirect), all requests reaching `UseResponseCompression()` are already HTTPS. Compression is always enabled for HTTPS.

### Why Early Placement?

1. **Response Modification**: Compression must happen before response is sent
2. **Performance**: Compress once, not multiple times
3. **Content-Type**: Must know content type before compressing
4. **Streaming**: Works with response streams efficiently

### Incorrect Placement ❌

```csharp
// ❌ WRONG - Too late in pipeline
app.UseCors();
app.UseAuthorization();
app.UseResponseCompression(); // Too late!
app.MapControllers();
```

**Problem**: Response may already be sent or modified by other middleware.

---

## Performance Considerations

### CPU vs. Bandwidth Trade-off

Compression requires CPU resources but saves bandwidth:

| Compression Level | CPU Usage | Bandwidth Savings | Use Case |
|------------------|---------|----------------------|----------|
| **Low (0-3)** | Low | 50-60% | High-traffic APIs, CPU-constrained |
| **Medium (4)** | Medium | 60-75% | **Recommended** - Optimal balance |
| **High (5-7)** | High | 75-85% | Low-traffic, bandwidth-constrained |
| **Maximum (8-11)** | Very High | 85-90% | Rarely needed |

### Microsoft Recommendations

1. **Compression Level 4**: Optimal balance for most scenarios
2. **Enable for HTTPS**: Modern implementations are secure
3. **Minimum Size**: 860-1024 bytes (don't compress tiny responses)
4. **Exclude Binary**: Skip already compressed formats

### Performance Impact

**Typical Impact:**

- **CPU Increase**: 2-5% (with level 4)
- **Bandwidth Reduction**: 60-80% for JSON/XML
- **Response Time**: 10-30% faster (depending on connection speed)
- **Memory**: Minimal increase (< 1 MB per request)

**When Compression Helps Most:**

- Large responses (> 10 KB)
- Slow connections (mobile, international)
- High bandwidth costs
- API-heavy applications

**When Compression Helps Least:**

- Small responses (< 1 KB)
- Fast local networks
- Already compressed content
- CPU-constrained servers

---

## Best Practices

### ✅ Do's

1. **Always Enable Compression**:
   - Reduces bandwidth by 60-80%
   - Minimal CPU overhead with level 4
   - Better user experience

2. **Use Brotli + Gzip**:
   - Brotli for modern clients (better compression)
   - Gzip for backward compatibility

3. **Set Optimal Compression Level**:
   - Use level 4 for both Brotli and Gzip
   - Balance between compression ratio and CPU usage

4. **Place Middleware Early**:
   - Before routing and other middleware
   - Ensures all responses are compressed

5. **Exclude Already Compressed Content**:
   - Images, videos, audio, fonts
   - Saves CPU for content that won't compress

6. **HTTPS-Only Application**:
   - This application blocks all HTTP requests (returns 400)
   - All requests are HTTPS, so compression is always enabled for HTTPS
   - No need to configure `EnableForHttps` (hardcoded to `true`)

7. **Monitor Compression Ratio**:
   - Track compression effectiveness
   - Adjust levels if needed

### ❌ Don'ts

1. **Don't Compress Small Responses**:
   - Overhead > savings for < 1 KB responses
   - Use `MinimumResponseSizeBytes` threshold

2. **Don't Use Maximum Compression**:
   - Level 11 (Brotli) or 9 (Gzip) uses too much CPU
   - Diminishing returns after level 6-7

3. **Don't Compress Binary Content**:
   - Images, videos, audio are already compressed
   - Wastes CPU with no benefit

4. **Don't Place Middleware Late**:
   - Must be before routing
   - Late placement may not compress responses

5. **HTTPS-Only Architecture**:
   - This application blocks HTTP requests (doesn't redirect)
   - All requests are HTTPS, compression always enabled
   - No configuration needed for `EnableForHttps`

6. **Don't Skip Monitoring**:
   - Monitor compression ratio
   - Track CPU usage impact

---

## Monitoring and Troubleshooting

### Monitoring Compression Effectiveness

#### 1. Response Headers

Check response headers to verify compression:

```http
HTTP/1.1 200 OK
Content-Encoding: br          # Brotli compression
Content-Type: application/json
Content-Length: 1234         # Compressed size
```

**Headers to Check:**

- `Content-Encoding: br` → Brotli compression applied
- `Content-Encoding: gzip` → Gzip compression applied
- No `Content-Encoding` → Compression not applied (may be too small or excluded)

#### 2. Compression Ratio

Calculate compression ratio:

```
Compression Ratio = (Original Size - Compressed Size) / Original Size × 100%
```

**Example:**

- Original: 10,000 bytes
- Compressed: 2,500 bytes
- Ratio: (10,000 - 2,500) / 10,000 × 100% = 75% reduction

**Expected Ratios:**

- JSON: 60-80% reduction
- XML: 70-85% reduction
- HTML: 70-90% reduction

#### 3. Performance Metrics

Monitor these metrics:

- **CPU Usage**: Should increase by 2-5% with level 4
- **Bandwidth**: Should decrease by 60-80%
- **Response Time**: Should improve by 10-30%
- **Compression Ratio**: Should be 60-80% for JSON/XML

### Troubleshooting

#### Issue: Compression Not Working

**Symptoms:**

- No `Content-Encoding` header in responses
- Large response sizes

**Solutions:**

1. Check `Enabled: true` in configuration
2. Verify middleware is in correct order (early in pipeline)
3. Check response size (must be > `MinimumResponseSizeBytes`)
4. Verify MIME type is in included list
5. Check if content is excluded (images, videos, etc.)

#### Issue: High CPU Usage

**Symptoms:**

- CPU usage > 10% increase
- Slow response times

**Solutions:**

1. Reduce compression level (4 → 3 or 2)
2. Increase `MinimumResponseSizeBytes` (1024 → 2048)
3. Exclude more MIME types if not needed
4. Consider disabling for specific endpoints

#### Issue: Low Compression Ratio

**Symptoms:**

- Compression ratio < 50%
- Small size reduction

**Solutions:**

1. Check if content is already compressed
2. Verify MIME type is correct
3. Increase compression level (4 → 5 or 6)
4. Check if response is too small (< 1 KB)

#### Issue: Compression Not Applied

**Symptoms:**

- No `Content-Encoding` header in responses
- Large response sizes

**Solutions:**

1. Check `Enabled: true` in configuration
2. Verify middleware is in correct order (early in pipeline)
3. Check response size (must be > `MinimumResponseSizeBytes`)
4. Verify MIME type is in included list
5. Check if content is excluded (images, videos, etc.)

**Note:** Since HTTP requests are blocked, all requests are HTTPS. Compression is always enabled for HTTPS (hardcoded).

---

## Examples

### Example 1: Basic Configuration

**appsettings.json:**

```json
{
  "ResponseCompressionOptions": {
    "Enabled": true,
    "BrotliCompressionLevel": 4,
    "GzipCompressionLevel": 4,
    "UseBrotli": true,
    "UseGzip": true
  }
}
```

**Result:**

- ✅ Brotli and Gzip enabled
- ✅ Optimal compression levels
- ✅ Always enabled for HTTPS (all requests are HTTPS since HTTP is blocked)
- ✅ Compresses JSON, XML, text responses

### Example 2: High-Performance Configuration

**appsettings.json:**

```json
{
  "ResponseCompressionOptions": {
    "Enabled": true,
    "MinimumResponseSizeBytes": 2048,
    "BrotliCompressionLevel": 3,
    "GzipCompressionLevel": 3,
    "UseBrotli": true,
    "UseGzip": true
  }
}
```

**Use Case**: High-traffic API with CPU constraints

**Result:**

- ✅ Faster compression (level 3)
- ✅ Only compresses larger responses (> 2 KB)
- ✅ Lower CPU usage
- ✅ Still good compression ratio (60-70%)
- ✅ Always enabled for HTTPS (all requests are HTTPS)

### Example 3: Maximum Compression Configuration

**appsettings.json:**

```json
{
  "ResponseCompressionOptions": {
    "Enabled": true,
    "MinimumResponseSizeBytes": 512,
    "BrotliCompressionLevel": 6,
    "GzipCompressionLevel": 6,
    "UseBrotli": true,
    "UseGzip": true
  }
}
```

**Use Case**: Bandwidth-constrained environment, low traffic

**Result:**

- ✅ Maximum compression (level 6)
- ✅ Compresses smaller responses (> 512 bytes)
- ✅ Higher CPU usage
- ✅ Best compression ratio (75-85%)
- ✅ Always enabled for HTTPS (all requests are HTTPS)

### Example 4: Custom MIME Types

**appsettings.json:**

```json
{
  "ResponseCompressionOptions": {
    "Enabled": true,
    "AdditionalMimeTypes": [
      "application/vnd.api+json",
      "application/hal+json",
      "application/problem+json"
    ]
  }
}
```

**Result:**

- ✅ Compresses custom JSON API formats
- ✅ Includes standard MIME types
- ✅ Optimal for API-first applications
- ✅ Always enabled for HTTPS (all requests are HTTPS)

### Example 5: Disable Compression

**appsettings.json:**

```json
{
  "ResponseCompressionOptions": {
    "Enabled": false
  }
}
```

**Use Case**: Compression handled by reverse proxy/CDN (e.g., Nginx, CloudFlare)

**Result:**

- ❌ Compression disabled in application
- ✅ Compression handled by infrastructure layer

---

## Testing

### Testing Compression

#### 1. Verify Compression Headers

```bash
# Test with curl
curl -H "Accept-Encoding: br, gzip" \
     -v https://api.example.com/api/v1/products \
     | grep -i "content-encoding"

# Expected output:
# < content-encoding: br
```

#### 2. Compare Response Sizes

```bash
# Without compression
curl -H "Accept-Encoding: identity" \
     https://api.example.com/api/v1/products \
     | wc -c

# With compression
curl -H "Accept-Encoding: br" \
     https://api.example.com/api/v1/products \
     | wc -c

# Compare sizes (should see 60-80% reduction)
```

#### 3. Test Different Algorithms

```bash
# Test Brotli
curl -H "Accept-Encoding: br" -v https://api.example.com/api/v1/products

# Test Gzip
curl -H "Accept-Encoding: gzip" -v https://api.example.com/api/v1/products

# Test no compression
curl -H "Accept-Encoding: identity" -v https://api.example.com/api/v1/products
```

### Unit Testing

```csharp
[Fact]
public async Task GetProducts_ReturnsCompressedResponse()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("Accept-Encoding", "br");
    
    // Act
    var response = await client.GetAsync("/api/v1/products");
    
    // Assert
    Assert.True(response.Content.Headers.ContentEncoding.Contains("br"));
    Assert.True(response.Content.Headers.ContentLength < 10000); // Compressed size
}
```

---

## Microsoft Guidelines Summary

### Key Recommendations

1. ✅ **Enable Compression**: Always enable for production
2. ✅ **Use Brotli + Gzip**: Best compression + backward compatibility
3. ✅ **Level 4**: Optimal compression level for both algorithms
4. ✅ **HTTPS-Only**: All requests are HTTPS (HTTP blocked), compression always enabled
5. ✅ **Minimum Size**: 860-1024 bytes threshold
6. ✅ **Exclude Binary**: Skip already compressed formats
7. ✅ **Early Middleware**: Place before routing

### Performance Targets

- **Compression Ratio**: 60-80% for JSON/XML
- **CPU Overhead**: < 5% with level 4
- **Response Time Improvement**: 10-30%
- **Bandwidth Reduction**: 60-80%

---

## Summary

- **Algorithm**: Brotli (preferred) + Gzip (fallback)
- **Compression Level**: 4 (optimal balance)
- **Minimum Size**: 1024 bytes
- **HTTPS**: Always enabled (all requests are HTTPS since HTTP is blocked)
- **Middleware Order**: Early in pipeline (before routing)
- **Exclusions**: Images, videos, audio, fonts, already compressed formats

For questions or issues, refer to the [Microsoft Response Compression documentation](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression) or contact the development team.
