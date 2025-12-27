# Scalar UI Implementation Guide

## Overview

The WebShop API uses **Scalar** for interactive API documentation. Scalar provides a modern, beautiful, and feature-rich API reference interface that automatically generates documentation from OpenAPI specifications. The implementation follows Scalar's official ASP.NET Core integration guidelines using the basic setup pattern with auto-discovery. It includes automatic version placeholder replacement (transforming `v{version}` to `v1`) through middleware transformation, ensuring developers see actual version numbers in API paths.

## Table of Contents

- [Why Scalar?](#why-scalar)
- [What Problem It Solves](#what-problem-it-solves)
- [How It Works](#how-it-works)
- [Architecture & Design](#architecture--design)
- [Benefits](#benefits)
- [Implementation Details](#implementation-details)
- [Configuration](#configuration)
- [OpenAPI Document Transformation](#openapi-document-transformation)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Why Scalar?

### The Problem with Traditional API Documentation

Traditional API documentation tools (like Swagger UI) have limitations:

1. **Outdated UI/UX**: Older interfaces that don't match modern design standards
2. **Limited Interactivity**: Basic request/response viewing without advanced features
3. **Poor Developer Experience**: Difficult to test APIs directly from documentation
4. **Version Management**: Limited support for API versioning and multiple document handling
5. **Customization**: Limited ability to customize appearance and behavior
6. **Performance**: Heavier bundle sizes and slower load times

### The Solution: Scalar

Scalar provides:

- **Modern UI/UX**: Beautiful, responsive interface with dark mode support
- **Enhanced Interactivity**: Advanced request building, testing, and response viewing
- **Better Developer Experience**: Intuitive interface for exploring and testing APIs
- **Version Support**: Built-in support for multiple API versions and document management
- **Customization**: Extensive configuration options for branding and behavior
- **Performance**: Optimized bundle size and fast load times
- **Type Safety**: Better code generation with type-safe client libraries
- **Auto-Filled Parameters**: Automatic handling of default values for path parameters

## What Problem It Solves

### 1. **Interactive API Documentation**

**Problem:** Developers need to manually construct API requests, copy-paste URLs, and manage authentication tokens when testing APIs.

**Solution:** Scalar provides an interactive interface where developers can:

- Build requests visually
- Test endpoints directly from the documentation
- See real-time request/response examples
- Manage authentication automatically

### 2. **API Version Management**

**Problem:** Managing multiple API versions in documentation is complex and error-prone.

**Solution:** Scalar supports multiple OpenAPI documents with version selection, allowing developers to easily switch between API versions.

### 3. **Path Parameter Defaults**

**Problem:** Developers must manually type version parameters (e.g., `/api/v1/...`) when testing APIs, leading to errors and frustration.

**Solution:** OpenAPI document transformation automatically sets default values for path parameters, so Scalar auto-fills them in the UI.

### 4. **Developer Onboarding**

**Problem:** New developers struggle to understand API structure and available endpoints.

**Solution:** Scalar provides an intuitive, searchable interface that makes API exploration easy and efficient.

### 5. **Consistent Documentation**

**Problem:** Documentation can become outdated or inconsistent across different tools.

**Solution:** Scalar automatically generates documentation from OpenAPI specs, ensuring it always matches the actual API implementation.

## How It Works

### Execution Flow

```
1. Application starts
   ↓
2. ConfigureOpenApiEndpoints() is called
   ↓
3. Reads AppSettings:Environment from appsettings.json
   ↓
4. If Environment is NOT "Production":
   ↓
4a. MapOpenApi() is called
    - Registers OpenAPI document endpoint
    - Generates default route (auto-discovered by Scalar)
   ↓
4b. MapScalarApiReference() is called
    - Configures Scalar UI
    - Auto-discovers OpenAPI document from MapOpenApi()
    - Scalar UI available at /scalar
   ↓
5. UseOpenApiTransformationMiddleware() is registered AFTER endpoints
   - Only intercepts /openapi/* requests
   - Transforms JSON to replace version placeholders
   ↓
6. Developer accesses /scalar
   ↓
7. Scalar auto-discovers and loads OpenAPI document
   ↓
8. OpenAPI transformation middleware intercepts response
   - Replaces v{version} with v1
   - Replaces {version} with 1
   - Updates Content-Length header
   ↓
9. Scalar renders interactive documentation
   - Shows actual version numbers in paths (e.g., /api/v1/addresses)
   - Displays all API endpoints
   - Provides interactive testing interface
```

### OpenAPI Document Transformation

The transformation middleware automatically:

1. **Replaces Version Placeholders**: Converts `v{version}` to `v1` and `{version}` to `1` in API paths
   - This ensures Scalar displays actual version numbers (e.g., `/api/v1/addresses`) instead of placeholders (e.g., `/api/v{version}/addresses`)
2. **Adds Default Values** (if needed): Injects `"default":"1"` into version path parameters in the OpenAPI spec
   - This enables Scalar to auto-fill version parameters when testing APIs
3. **Middleware Registration**: Must be registered **after** `MapOpenApi()` to properly intercept endpoint responses

## Architecture & Design

### Component Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Scalar UI (/scalar)                       │
│  - Interactive API documentation                             │
│  - Request/response testing                                  │
│  - Auto-discovered OpenAPI document                          │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ Auto-Discovers OpenAPI Document
                        │
┌───────────────────────▼─────────────────────────────────────┐
│         OpenAPI Document Endpoint (auto-generated)          │
│  - Generated by MapOpenApi()                                │
│  - Scalar automatically finds it                             │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ Intercepted by Middleware
                        │
┌───────────────────────▼─────────────────────────────────────┐
│      OpenAPI Transformation Middleware                      │
│  - Only intercepts /openapi/* requests                     │
│  - Replaces v{version} with v1                             │
│  - Replaces {version} with 1                                │
│  - Updates Content-Length header                            │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ Transformed JSON
                        │
┌───────────────────────▼─────────────────────────────────────┐
│              Microsoft.AspNetCore.OpenApi                    │
│  - Generates OpenAPI 3.0 specification                      │
│  - From controller attributes and route templates            │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

1. **Scalar.AspNetCore Package**
   - Provides `MapScalarApiReference()` extension method
   - Handles Scalar UI rendering and auto-discovery
   - Automatically finds OpenAPI documents generated by `MapOpenApi()`

2. **Microsoft.AspNetCore.OpenApi Package**
   - Provides `MapOpenApi()` extension method
   - Generates OpenAPI 3.0 specification from controllers
   - Creates default OpenAPI document endpoint

3. **OpenAPI Transformation Middleware**
   - Intercepts only `/openapi/*` requests (early return for others)
   - Transforms JSON to replace version placeholders (`v{version}` → `v1`)
   - Updates Content-Length header after transformation
   - Must be registered **after** endpoints are mapped

4. **Configuration-Based Access**
   - Scalar UI visibility controlled by `AppSettings:Environment` in appsettings.json
   - Disabled when Environment is "Production" (case-insensitive)
   - Available for all other environment values (Dev, QA, UAT, etc.)

## Benefits

### 1. **Improved Developer Experience**

- **Intuitive Interface**: Modern, clean UI that's easy to navigate
- **Interactive Testing**: Test APIs directly from documentation
- **Auto-Filled Parameters**: No need to manually type version numbers
- **Search Functionality**: Quickly find endpoints and operations

### 2. **Better API Discovery**

- **Visual Organization**: Endpoints organized by tags and operations
- **Request/Response Examples**: See example payloads and responses
- **Schema Documentation**: Detailed schema information with types
- **Code Generation**: Generate client code in multiple languages

### 3. **Version Management**

- **Multiple Documents**: Support for multiple API versions
- **Version Selection**: Easy switching between versions
- **Default Version**: Automatic selection of default version
- **Version-Specific Documentation**: Each version has its own documentation

### 4. **Security**

- **Configuration-Based Access**: Visibility controlled by `AppSettings:Environment` setting
- **No Production Exposure**: Scalar UI automatically disabled when Environment is "Production"
- **Flexible Control**: Can be enabled/disabled per environment via configuration
- **Controlled Access**: Can be further restricted via authentication if needed

### 5. **Maintainability**

- **Automatic Generation**: Documentation generated from code
- **Always Up-to-Date**: Changes in code automatically reflect in documentation
- **No Manual Updates**: No need to manually maintain documentation

## Implementation Details

### File Locations

- **Service Configuration**: `src/WebShop.Api/Extensions/Features/OpenApiExtensions.cs`
- **Transformation Helper**: `src/WebShop.Api/Helpers/OpenApiTransformer.cs`
- **Service Registration**: Called from `src/WebShop.Api/Extensions/Core/ServiceExtensions.cs`
- **Endpoint Configuration**: Called from `src/WebShop.Api/Program.cs`

### Package Installation

The following packages are required (managed via `Directory.Packages.props`):

```xml
<!-- In Directory.Packages.props -->
<PackageVersion Include="Scalar.AspNetCore" Version="..." />
<PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="..." />

<!-- In WebShop.Api.csproj -->
<PackageReference Include="Scalar.AspNetCore" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" />
```

### Service Registration

OpenAPI services are registered in the service layer:

```csharp
// Located in: src/WebShop.Api/Extensions/Features/OpenApiExtensions.cs
public static void ConfigureOpenApi(this IServiceCollection services)
{
    services.AddOpenApi();
}
```

This is called from `ServiceExtensions.ConfigureApiServices()` during application startup.

### Basic Configuration

The Scalar UI is configured following the official Scalar ASP.NET Core integration guidelines. The implementation uses the basic setup pattern:

```csharp
// Located in: src/WebShop.Api/Extensions/Features/OpenApiExtensions.cs
public static void ConfigureOpenApiEndpoints(this WebApplication app)
{
    string? environment = app.Configuration.GetValue<string>("AppSettings:Environment");
    
    // Show Scalar UI for all environments except Production
    if (!string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
    {
        // Basic setup as per Scalar documentation
        app.MapOpenApi();
        app.MapScalarApiReference();
    }
    
    // Register transformation middleware AFTER endpoints are mapped
    // This ensures it can intercept OpenAPI responses
    app.UseOpenApiTransformationMiddleware();
}
```

**Key Points:**

- Uses `MapOpenApi()` without parameters - generates default OpenAPI document route
- Uses `MapScalarApiReference()` without parameters - auto-discovers the OpenAPI document
- Transformation middleware is registered **after** endpoints to properly intercept responses
- No custom routes or complex configuration needed - Scalar handles discovery automatically

### OpenAPI Transformation Middleware

The middleware intercepts OpenAPI document responses and transforms them to replace version placeholders. **Important:** The middleware must be registered **after** `MapOpenApi()` is called to properly intercept endpoint responses.

```csharp
// Located in: src/WebShop.Api/Extensions/Features/OpenApiExtensions.cs
private static void UseOpenApiTransformationMiddleware(this WebApplication app)
{
    app.Use(async (context, next) =>
    {
        // Only intercept OpenAPI document requests
        if (!context.Request.Path.StartsWithSegments("/openapi"))
        {
            await next();
            return;
        }

        Stream originalBodyStream = context.Response.Body;
        using MemoryStream responseBody = new();
        context.Response.Body = responseBody;

        await next();

        // Transform OpenAPI JSON responses to replace version placeholders
        if (ShouldTransformOpenApiResponse(context))
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            string originalJson = await new StreamReader(responseBody).ReadToEndAsync();
            string transformedJson = OpenApiTransformer.Transform(originalJson);
            
            // Update Content-Length header
            context.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(transformedJson);
            
            // Write transformed content
            responseBody.SetLength(0);
            await responseBody.WriteAsync(System.Text.Encoding.UTF8.GetBytes(transformedJson));
        }

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    });
}

// Helper method to determine if transformation is needed
private static bool ShouldTransformOpenApiResponse(HttpContext context)
{
    // Only transform successful OpenAPI document responses
    return context.Request.Path.StartsWithSegments("/openapi") &&
           context.Response.StatusCode == 200 &&
           (context.Response.ContentType?.Contains("application/json") == true ||
            context.Response.ContentType?.Contains("application/openapi+json") == true);
}
```

**Key Implementation Details:**

- **Early return for non-OpenAPI requests** - Only processes `/openapi/*` paths to avoid interfering with other routes
- **Registered after endpoints** - Must be called after `MapOpenApi()` to intercept endpoint responses
- **Content-Length update** - Updates the header after transformation to match new content size
- **Uses OpenApiTransformer** - Delegates transformation logic to the helper class in `src/WebShop.Api/Helpers/OpenApiTransformer.cs`
- **Response validation** - `ShouldTransformOpenApiResponse()` checks path, status code (200), and content type (application/json or application/openapi+json)

### OpenAPI Document Transformation

The transformation is handled by the `OpenApiTransformer` helper class (located in `src/WebShop.Api/Helpers/OpenApiTransformer.cs`), which:

1. **Replaces version placeholders in paths:**
   - `v{version}` → `v1`
   - `{version}` → `1`

2. **Adds default values for version path parameters** using regex patterns:
   - Pattern 1: Matches version path parameter and adds `"default":"1"` property
   - Pattern 2: Handles edge cases where default might already exist but is empty/null

**Implementation:**

```csharp
// Located in: src/WebShop.Api/Helpers/OpenApiTransformer.cs
public static string Transform(string json)
{
    // Replace version placeholders with default version (1) in paths
    json = json.Replace("v{version}", "v1").Replace("{version}", "1");

    // Add default value for version path parameter in the OpenAPI spec
    // Pattern 1: Matches version path parameter and adds default value
    json = Regex.Replace(
        json,
        @"""name""\s*:\s*""version""([^}]*?)""in""\s*:\s*""path""([^}]*?)(})",
        @"""name"":""version""$1""in"":""path""$2,""default"":""1""$3",
        RegexOptions.IgnoreCase);

    // Pattern 2: Handle edge case where default might already exist but is empty/null
    json = Regex.Replace(
        json,
        @"""name""\s*:\s*""version""([^}]*?)""default""\s*:\s*(?:""""|null|""[^""]*"")",
        @"""name"":""version""$1""default"":""1""",
        RegexOptions.IgnoreCase);

    return json;
}
```

The transformation ensures that Scalar displays actual version numbers (e.g., `/api/v1/addresses`) instead of placeholders (e.g., `/api/v{version}/addresses`).

## Configuration

### Configuration-Based Access

Scalar UI visibility is controlled by the `AppSettings:Environment` setting in `appsettings.json`. Scalar UI is automatically disabled when the Environment value is "Production" (case-insensitive):

```csharp
public static void ConfigureOpenApiEndpoints(this WebApplication app)
{
    string? environment = app.Configuration.GetValue<string>("AppSettings:Environment");
    
    // Show Scalar UI for all environments except Production
    if (!string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
    {
        // Basic setup as per Scalar documentation
        app.MapOpenApi();
        app.MapScalarApiReference();
    }
    
    // Register transformation middleware AFTER endpoints are mapped
    app.UseOpenApiTransformationMiddleware();
}
```

**Configuration Example** (`appsettings.json`):

```json
{
  "AppSettings": {
    "Environment": "Dev"  // Set to "Production" to disable Scalar UI
  }
}
```

**Behavior:**

- ✅ **Environment = "Development"**: Scalar UI is shown
- ✅ **Environment = "QA"**: Scalar UI is shown
- ✅ **Environment = "UAT"**: Scalar UI is shown
- ❌ **Environment = "Production"**: Scalar UI is hidden

### Document Configuration

The current implementation uses Scalar's auto-discovery feature. When `MapScalarApiReference()` is called without parameters, Scalar automatically discovers the OpenAPI document generated by `MapOpenApi()`. No explicit document configuration is needed.

**For future multi-version support**, documents can be configured using the `AddDocument` method:

```csharp
app.MapScalarApiReference(options => options
    .AddDocument("v1", "WebShop API v1", isDefault: true)
    .AddDocument("v2", "WebShop API v2", isDefault: false));
```

### OpenAPI Route Pattern

When using auto-discovery (current implementation), Scalar automatically finds the OpenAPI document. For custom configurations, the route pattern supports placeholders:

```csharp
.WithOpenApiRoutePattern("/openapi/{documentName}.json")
```

The `{documentName}` placeholder is replaced with the actual document name when Scalar fetches the OpenAPI document.

### HTTP Client Code Generation

Configure the default HTTP client for code samples:

```csharp
.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
```

This generates C# HttpClient code samples in Scalar UI.

## OpenAPI Document Transformation

### Why Transformation is Needed

ASP.NET Core's OpenAPI generator creates paths with placeholders like `/api/v{version}/products`. Scalar needs:

1. Actual values instead of placeholders (for display)
2. Default values for path parameters (for auto-fill)

### Transformation Process

1. **Version Placeholder Replacement**
   - `v{version}` → `v1`
   - `{version}` → `1`

2. **Default Value Injection**
   - Finds version path parameters in the OpenAPI spec
   - Adds `"default":"1"` property to the parameter definition
   - Enables Scalar to auto-fill the parameter

### Transformation Patterns

**Pattern 1: Standard Parameter Structure**
Matches version path parameters and adds the `"default":"1"` property. The regex pattern:

- Matches `"name":"version"` followed by any properties
- Finds `"in":"path"`
- Adds `,"default":"1"` before the closing brace

Example transformation:

```json
{
  "name": "version",
  "in": "path",
  "required": true,
  "schema": { "type": "string" }
}
```

Becomes:

```json
{
  "name": "version",
  "in": "path",
  "required": true,
  "schema": { "type": "string" },
  "default": "1"
}
```

**Pattern 2: Edge Case Handling**
Handles cases where a default might already exist but is empty (`""`), null, or has an invalid value. The regex pattern:

- Matches `"name":"version"` followed by any properties
- Finds existing `"default"` property with empty/null/invalid value
- Replaces it with `"default":"1"`

This ensures that even if the OpenAPI spec already has a default value for the version parameter, it will be set to "1" for consistency.

## Usage Examples

### Accessing Scalar UI

1. **Development Environment**

   ```
   https://localhost:7109/scalar
   ```

   Scalar UI loads and auto-discovers the OpenAPI document generated by `MapOpenApi()`.

2. **OpenAPI Document Endpoint**

   ```
   https://localhost:7109/openapi/v1.json
   ```

   The OpenAPI document endpoint generated by `MapOpenApi()`. When called without parameters, it generates a default route based on API versioning configuration.

   **Note:** The exact route depends on your API versioning setup. With default configuration, it's typically `/openapi/v1.json` or `/openapi/v1/openapi.json`.

### Using Scalar UI

1. **Browse Endpoints**
   - Navigate through API endpoints organized by tags
   - Expand operations to see details

2. **Test Endpoints**
   - Click "Try it out" on any endpoint
   - Fill in required parameters (version is auto-filled)
   - Click "Execute" to send request
   - View response in the UI

3. **View Schemas**
   - Click on schema names to see detailed structure
   - View request/response examples
   - See data types and validation rules

4. **Generate Client Code**
   - Select an endpoint
   - Choose code generation target (C# HttpClient)
   - Copy generated code

### Adding Multiple API Versions

The current implementation uses Scalar's auto-discovery with a single document. To support multiple API versions in the future:

```csharp
// Map multiple OpenAPI documents
app.MapOpenApi("v1");
app.MapOpenApi("v2");

// Configure Scalar with multiple documents
app.MapScalarApiReference(options => options
    .WithTitle("WebShop API Documentation")
    .WithOpenApiRoutePattern("/openapi/{documentName}.json")
    .AddDocument("v1", "WebShop API v1", "/openapi/v1/openapi.json", isDefault: true)
    .AddDocument("v2", "WebShop API v2", "/openapi/v2/openapi.json", isDefault: false));
```

Users can then switch between versions using the version selector in Scalar UI.

**Note:** When adding multiple versions, ensure the transformation middleware handles all OpenAPI document routes correctly.

## Best Practices

### 1. **Environment Restrictions**

✅ **DO**: Set `AppSettings:Environment` to "Production" in production environments

```json
{
  "AppSettings": {
    "Environment": "Production"  // Disables Scalar UI
  }
}
```

✅ **DO**: Use configuration-based control for flexibility

```csharp
string? environment = app.Configuration.GetValue<string>("AppSettings:Environment");
if (!string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
```

❌ **DON'T**: Expose Scalar UI in Production without authentication

❌ **DON'T**: Hardcode environment checks - use configuration instead

### 2. **Simple Setup**

✅ **DO**: Use Scalar's auto-discovery feature

```csharp
app.MapOpenApi();              // Generates OpenAPI document
app.MapScalarApiReference();   // Auto-discovers it
```

✅ **DO**: Keep configuration minimal when auto-discovery works

❌ **DON'T**: Over-configure when the basic setup is sufficient

### 3. **Middleware Order**

✅ **DO**: Register transformation middleware **after** endpoints

```csharp
app.MapOpenApi();                          // First
app.MapScalarApiReference();              // Second
app.UseOpenApiTransformationMiddleware(); // Third
```

❌ **DON'T**: Register transformation middleware before endpoints

### 4. **OpenAPI Transformation**

✅ **DO**: Transform OpenAPI documents to replace version placeholders

- Replaces `v{version}` with `v1` in paths
- Replaces `{version}` with `1` in paths
- Improves developer experience by showing actual version numbers

✅ **DO**: Only intercept OpenAPI requests in middleware

```csharp
if (!context.Request.Path.StartsWithSegments("/openapi"))
{
    await next();
    return;  // Skip transformation for non-OpenAPI requests
}
```

✅ **DO**: Update Content-Length header after transformation

```csharp
context.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(transformedJson);
```

❌ **DON'T**: Skip transformation if using version placeholders in paths
❌ **DON'T**: Transform non-OpenAPI responses

### 5. **Future Enhancements**

When adding custom configuration (titles, code generation, etc.):

✅ **DO**: Use Scalar's configuration options when needed

```csharp
app.MapScalarApiReference(options => options
    .WithTitle("WebShop API Documentation")
    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
```

✅ **DO**: Start with basic setup and add configuration only when needed

❌ **DON'T**: Over-configure when the basic setup works

## Troubleshooting

### Issue: Scalar UI Not Loading

**Symptoms:**

- Blank page when accessing `/scalar`
- 404 error on Scalar routes

**Solutions:**

1. **Check Configuration**: Verify `AppSettings:Environment` is not "Production"

   ```json
   {
     "AppSettings": {
       "Environment": "Dev"  // Should NOT be "Production"
     }
   }
   ```

   ```csharp
   // Verify configuration value
   string? environment = app.Configuration.GetValue<string>("AppSettings:Environment");
   // Should NOT equal "Production" (case-insensitive)
   ```

2. **Verify Package Installation**: Check that `Scalar.AspNetCore` is installed

   ```bash
   dotnet list package | grep Scalar
   ```

3. **Check Route Registration**: Ensure `ConfigureOpenApiEndpoints()` is called in `Program.cs`

### Issue: Version Placeholders Not Replaced

**Symptoms:**

- Paths show `v{version}` instead of `v1` (e.g., `/api/v{version}/addresses`)
- Paths show `{version}` instead of `1`

**Solutions:**

1. **Verify Transformation Middleware Order**: Ensure it's registered **after** `MapOpenApi()`

   ```csharp
   app.MapOpenApi();                          // First - register endpoint
   app.MapScalarApiReference();              // Second - configure Scalar
   app.UseOpenApiTransformationMiddleware(); // Third - intercept responses
   ```

2. **Check Middleware Interception**: Verify middleware only processes `/openapi/*` requests

   ```csharp
   if (!context.Request.Path.StartsWithSegments("/openapi"))
   {
       await next();
       return;  // Should skip non-OpenAPI requests
   }
   ```

3. **Check OpenAPI Document**: Verify transformation is working
   - Access the OpenAPI endpoint directly (e.g., `/openapi/v1/openapi.json`)
   - Check that paths show `v1` instead of `v{version}`

4. **Verify Content-Length Update**: Ensure Content-Length header is updated after transformation

   ```csharp
   context.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(transformedJson);
   ```

### Issue: OpenAPI Document Not Found

**Symptoms:**

- Scalar shows "Document 'v1' could not be loaded" error
- 404 when accessing the OpenAPI document endpoint

**Solutions:**

1. **Check OpenAPI Registration**: Ensure `MapOpenApi()` is called **before** `MapScalarApiReference()`

   ```csharp
   app.MapOpenApi();              // Must be called first
   app.MapScalarApiReference();   // Then Scalar can auto-discover
   ```

2. **Verify Middleware Order**: Transformation middleware must be registered **after** endpoints

   ```csharp
   app.MapOpenApi();
   app.MapScalarApiReference();
   app.UseOpenApiTransformationMiddleware();  // Must be after endpoints
   ```

3. **Check Environment Configuration**: Ensure `AppSettings:Environment` is not "Production"

   ```json
   {
     "AppSettings": {
       "Environment": "Dev"  // Should NOT be "Production"
     }
   }
   ```

4. **Verify OpenAPI Document is Accessible**: Test the OpenAPI endpoint directly in browser
   - Navigate to the default OpenAPI route (usually `/openapi/v1/openapi.json` or similar)
   - Ensure it returns valid JSON

### Issue: Multiple Versions Not Working

**Symptoms:**

- Only one version appears in Scalar
- Cannot switch between versions

**Solutions:**

1. **Verify Multiple Documents**: Ensure all versions are added

   ```csharp
   .AddDocument("v1", ...)
   .AddDocument("v2", ...)
   ```

2. **Check OpenAPI Endpoints**: Ensure all OpenAPI documents are accessible
   - `/openapi/v1.json` should work
   - `/openapi/v2.json` should work

3. **Verify Route Pattern**: Ensure placeholder is used correctly

   ```csharp
   .WithOpenApiRoutePattern($"/openapi/{{documentName}}.json")
   ```

### Issue: Transformation Not Working

**Symptoms:**

- Version placeholders still in paths (e.g., `/api/v{version}/addresses` instead of `/api/v1/addresses`)
- Paths show `{version}` instead of actual version numbers

**Solutions:**

1. **Check Middleware Order**: Transformation middleware must be **after** `MapOpenApi()`

   ```csharp
   app.MapOpenApi();                          // First - register endpoint
   app.MapScalarApiReference();              // Second - configure Scalar
   app.UseOpenApiTransformationMiddleware(); // Third - intercept responses
   ```

2. **Verify Middleware Intercepts Requests**: Ensure middleware only processes `/openapi/*` paths

   ```csharp
   if (!context.Request.Path.StartsWithSegments("/openapi"))
   {
       await next();
       return;  // Skip transformation for non-OpenAPI requests
   }
   ```

3. **Verify Response Interception**: Check `ShouldTransformOpenApiResponse()` logic
   - Path must start with `/openapi`
   - Content-Type must be `application/json` or `application/openapi+json`
   - Status code must be 200

4. **Check Content-Length**: Ensure Content-Length header is updated after transformation

   ```csharp
   context.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(transformedJson);
   ```

5. **Test Transformation**: Verify the OpenAPI document endpoint shows transformed paths
   - Access the OpenAPI endpoint directly (e.g., `/openapi/v1/openapi.json`)
   - Check that paths show `v1` instead of `v{version}`

### Issue: Scalar UI Disabled When It Should Be Enabled

**Symptoms:**

- Cannot access `/scalar` route
- Scalar UI not available even in non-production environments

**Solutions:**

1. **Check Configuration Setting**: Verify `AppSettings:Environment` in `appsettings.json`

   ```json
   {
     "AppSettings": {
       "Environment": "Dev"  // Should NOT be "Production"
     }
   }
   ```

   The value is case-insensitive, but must NOT be "Production" to enable Scalar UI.

2. **Check Configuration Override**: Verify if `appsettings.Production.json` or environment variables override the setting

   ```bash
   # Check environment variables
   echo $AppSettings__Environment
   ```

3. **Verify Configuration Reading**: Ensure the configuration is being read correctly

   ```csharp
   string? environment = app.Configuration.GetValue<string>("AppSettings:Environment");
   // Should NOT equal "Production" (case-insensitive comparison)
   ```

4. **Check Configuration Binding**: Ensure `AppSettings` section exists and is properly formatted in `appsettings.json`

## Summary

Scalar UI provides a modern, interactive API documentation experience that:

✅ **Simple setup** - Uses basic `MapOpenApi()` and `MapScalarApiReference()` following Scalar's official guidelines  
✅ **Auto-discovery** - Scalar automatically discovers the OpenAPI document without custom configuration  
✅ **Version placeholder replacement** - Transforms `v{version}` to `v1` in API paths automatically  
✅ **Configuration-aware** - Visibility controlled by `AppSettings:Environment`, disabled in Production for security  
✅ **Minimal code** - Clean, simple implementation without unnecessary complexity  
✅ **Developer-friendly** - Intuitive interface for API exploration and testing  

### Current Implementation

The implementation follows the official Scalar ASP.NET Core integration pattern:

```csharp
// Service Registration (in ConfigureApiServices)
services.ConfigureOpenApi();  // Calls services.AddOpenApi()

// Endpoint Configuration (in ConfigureOpenApiEndpoints)
// Only in non-Production environments
app.MapOpenApi();                    // Generates OpenAPI document
app.MapScalarApiReference();         // Auto-discovers and displays it
app.UseOpenApiTransformationMiddleware();  // Transforms version placeholders
```

**File Locations:**

- Service registration: `src/WebShop.Api/Extensions/Features/OpenApiExtensions.cs` (namespace: `WebShop.Api.Extensions.Features`) → `ConfigureOpenApi()`
- Endpoint configuration: `src/WebShop.Api/Extensions/Features/OpenApiExtensions.cs` → `ConfigureOpenApiEndpoints()`
- Transformation helper: `src/WebShop.Api/Helpers/OpenApiTransformer.cs` (namespace: `WebShop.Api.Helpers`)
- Called from: `src/WebShop.Api/Program.cs` → `app.ConfigureOpenApiEndpoints()`

**Key Benefits:**

- **No custom routes needed** - Scalar handles discovery automatically
- **No complex configuration** - Uses Scalar's default behavior
- **Transformation middleware** - Only intercepts `/openapi/*` requests to replace version placeholders
- **Production-safe** - Automatically disabled when `AppSettings:Environment` is "Production"

The implementation ensures that developers can easily discover, understand, and test APIs with actual version numbers displayed (e.g., `/api/v1/addresses`) instead of placeholders, significantly improving the developer experience.

## References

- [Scalar ASP.NET Core Integration Guide](https://guides.scalar.com/scalar/scalar-api-references/integrations/net-aspnet-core/integration)
- [Scalar Configuration Documentation](https://guides.scalar.com/scalar/scalar-api-references/configuration)
- [Microsoft.AspNetCore.OpenApi Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi)
