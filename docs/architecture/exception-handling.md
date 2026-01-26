# Exception Handling Implementation Guide

## Overview

The WebShop API uses a **global exception handling middleware** to catch, log, and return standardized error responses for all unhandled exceptions. This ensures consistent error responses across the entire API, proper error logging with correlation IDs, and improved debugging capabilities while preventing sensitive information from being exposed to clients.

[← Back to README](../../README.md)

## Table of Contents

- [Why Exception Handling?](#why-exception-handling)
- [What Problem It Solves](#what-problem-it-solves)
- [How It Works](#how-it-works)
- [Architecture & Design](#architecture--design)
- [Exception Types Handled](#exception-types-handled)
- [Implementation Details](#implementation-details)
- [Response Format](#response-format)
- [Configuration](#configuration)
- [Customization](#customization)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## Why Exception Handling?

### The Problem Without Global Exception Handling

Without a global exception handler, you face:

1. **Inconsistent Error Responses**: Different controllers return errors in different formats
2. **Sensitive Information Leakage**: Stack traces and internal details exposed to clients
3. **Poor Error Tracking**: No correlation IDs, making it difficult to trace errors in logs
4. **Inconsistent Logging**: Some exceptions logged, others not; different log formats
5. **Poor User Experience**: Technical error messages shown to end users
6. **Security Risks**: Internal implementation details revealed in error messages

### The Solution: Global Exception Handling Middleware

The `ExceptionHandlingMiddleware` provides:

- **Standardized Error Responses**: All errors return the same `Response<ApiError>` format
- **Error Correlation IDs**: Unique error IDs for tracking errors in logs and support tickets
- **Structured Logging**: Consistent, structured logging with exception details, paths, and methods
- **Security**: Generic error messages for unexpected exceptions, preventing information leakage
- **Configurable Log Levels**: Different log levels based on exception type
- **Custom Response Enhancement**: Ability to add custom error details based on exception type

---

## What Problem It Solves

### 1. **Consistent Error Responses**

**Problem:** Different controllers return errors in different formats, making it difficult for clients to handle errors consistently.

**Solution:** All exceptions are caught and returned in a standardized `Response<ApiError>` format:

```json
{
  "succeeded": false,
  "data": null,
  "message": "",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440000",
      "statusCode": 400,
      "message": "Invalid argument provided."
    }
  ]
}
```

### 2. **Error Tracking & Debugging**

**Problem:** When errors occur in production, it's difficult to trace them back to specific requests or logs.

**Solution:** Each error response includes a unique `ErrorId` that:
- Is logged with the exception details
- Can be used by support teams to find the exact error in logs
- Is included in the response so clients can reference it when reporting issues

### 3. **Security & Information Leakage Prevention**

**Problem:** Unexpected exceptions may expose stack traces, file paths, database connection strings, or other sensitive information.

**Solution:** The middleware:
- Returns generic error messages for unexpected exceptions
- Only exposes exception messages for known, safe exception types (e.g., `ArgumentException`, `KeyNotFoundException`)
- Logs full exception details server-side for debugging

### 4. **Appropriate HTTP Status Codes**

**Problem:** All exceptions might return `500 Internal Server Error`, even for client errors.

**Solution:** The middleware maps exception types to appropriate HTTP status codes:
- `ArgumentException` → `400 Bad Request`
- `KeyNotFoundException` → `404 Not Found`
- `BadHttpRequestException` (413) → `413 Request Entity Too Large`
- `UnauthorizedAccessException` → `403 Forbidden`
- `OperationCanceledException` → `499 Client Closed Request`
- Unknown exceptions → `500 Internal Server Error`

### 5. **Structured Logging**

**Problem:** Exceptions are logged inconsistently, making it difficult to query and analyze errors.

**Solution:** All exceptions are logged with structured logging using a consistent template format:
- **Area**: Identifies the handler method (e.g., `ExceptionHandlingMiddleware.HandleExceptionAsync`)
- **RequestPath**: HTTP request path
- **RequestMethod**: HTTP method (GET, POST, etc.)
- **ErrorId**: Unique error correlation ID
- **Message**: Combined error message with inner exception details
- Appropriate log level based on exception type

**Log Template Format:**
```
Area: {Area}, RequestPath: {RequestPath}, RequestMethod: {RequestMethod}, ErrorId: {ErrorId}, Message: {Message}
```

This format follows the project's logging guidelines and enables easy querying and analysis of logs.

---

## How It Works

### Middleware Pipeline

The exception handling middleware is registered early in the middleware pipeline (after HTTPS enforcement, before CORS and authorization):

```
Request → HTTPS Enforcement → Exception Handling → CORS → Authorization → Controllers
```

This ensures that:
1. All exceptions from downstream middleware and controllers are caught
2. Errors are handled before CORS/authorization, ensuring proper error responses
3. HTTPS is enforced before exception handling

### Exception Handling Flow

```
1. Request enters middleware pipeline
2. Middleware calls next middleware/controller
3. If exception occurs:
   a. Exception is caught by specific catch block
   b. Error ID is generated or retrieved from exception.Data
   c. ApiError object is created with error details
   d. Response<ApiError> is constructed
   e. Custom response details are added (if configured)
   f. Log level is determined (custom or default)
   g. Exception is logged with structured data
   h. Standardized error response is returned
4. If no exception: Request continues normally
```

### Error ID Generation

Error IDs are generated using one of two methods:

1. **From Exception Data**: If the exception already has an `ErrorId` in `exception.Data["ErrorId"]`, it's reused (useful for propagating error IDs through layers)
2. **New GUID**: If no error ID exists, a new GUID is generated and stored in `exception.Data["ErrorId"]` for potential reuse

---

## Architecture & Design

### Components

The exception handling system consists of three main components:

1. **`ExceptionHandlingMiddleware`**: The core middleware that catches and processes exceptions
2. **`ExceptionHandlingOptions`**: Configuration options for customizing behavior
3. **`ExceptionHandlingExtensions`**: Extension methods for registering the middleware

### File Structure

```
src/WebShop.Api/
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs    # Core middleware implementation
├── Models/
│   ├── ExceptionHandlingOptions.cs      # Configuration options
│   ├── ApiError.cs                      # Error model
│   └── Response.cs                      # Standardized response model
└── Extensions/
    ├── ExceptionHandlingExtensions.cs   # Middleware registration
    └── Middleware/MiddlewareExtensions.cs           # Pipeline configuration
```

### Design Principles

1. **DRY (Don't Repeat Yourself)**: Common exception processing logic is centralized in `ProcessExceptionAsync`
2. **SOLID Principles**:
   - **Single Responsibility**: Each handler method handles one exception type
   - **Open/Closed**: New exception types can be added without modifying existing code
   - **Dependency Inversion**: Middleware depends on `ExceptionHandlingOptions` abstraction
3. **Separation of Concerns**: Exception handling logic is separated from business logic
4. **Configurability**: Behavior can be customized through options without modifying middleware code
5. **Structured Logging**: Uses consistent logging template following project guidelines for easy log analysis

---

## Exception Types Handled

The middleware handles the following exception types in order of specificity:

### 1. `OperationCanceledException`

**HTTP Status Code:** `499 Client Closed Request` (Non-Standard)  
**Log Level:** `Information` (default)  
**Message:** "Request was canceled by the client."

**When It Occurs:**
- Client disconnects before request completes
- Request timeout occurs
- Cancellation token is triggered

**Example:**
```csharp
// Client disconnects or timeout occurs
throw new OperationCanceledException();
```

### 2. `ArgumentException` / `ArgumentNullException`

**HTTP Status Code:** `400 Bad Request`  
**Log Level:** `Warning` (default)  
**Message:** Exception message (e.g., "Value cannot be null. (Parameter 'customerId')")

**When It Occurs:**
- Invalid parameters passed to methods
- Null arguments where not allowed
- Validation failures in business logic

**Example:**
```csharp
if (customerId <= 0)
    throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
```

### 3. `UnauthorizedAccessException`

**HTTP Status Code:** `403 Forbidden`  
**Log Level:** `Warning` (default)  
**Message:** Exception message

**When It Occurs:**
- User attempts to access resource without proper permissions
- Authorization checks fail

**Example:**
```csharp
if (!user.HasPermission("DeleteCustomer"))
    throw new UnauthorizedAccessException("You do not have permission to delete customers.");
```

### 4. `KeyNotFoundException`

**HTTP Status Code:** `404 Not Found`  
**Log Level:** `Information` (default)  
**Message:** Exception message

**When It Occurs:**
- Resource not found in dictionary/collection
- Entity not found in database (when using `KeyNotFoundException`)

**Example:**
```csharp
var customer = await _repository.GetByIdAsync(id);
if (customer == null)
    throw new KeyNotFoundException($"Customer with ID {id} not found.");
```

### 5. `InvalidOperationException` (with "not found" message)

**HTTP Status Code:** `404 Not Found`  
**Log Level:** `Information` (default)  
**Message:** Exception message

**When It Occurs:**
- Business logic throws `InvalidOperationException` with "not found" message
- Common pattern in service layers

**Example:**
```csharp
var customer = await _repository.GetByIdAsync(id);
if (customer == null)
    throw new InvalidOperationException($"Customer with ID {id} not found.");
```

### 6. `BadHttpRequestException` (413 Payload Too Large)

**HTTP Status Code:** `413 Request Entity Too Large`  
**Log Level:** `Warning` (default)  
**Message:** "Request body size exceeds the maximum allowed limit. Please reduce the request size and try again."

**When It Occurs:**
- Request body size exceeds `MaxRequestBodySize` configured in Kestrel
- Request headers exceed `MaxRequestHeadersTotalSize` or `MaxRequestHeaderCount`
- Form data exceeds `MultipartBodyLengthLimit` or `ValueCountLimit`
- Thrown by Kestrel server before request reaches middleware pipeline

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Request body size exceeds the maximum allowed limit. Please reduce the request size and try again.",
  "errors": [
    {
      "errorId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "statusCode": 413,
      "message": "Request body size exceeds the maximum allowed limit. Please reduce the request size and try again."
    }
  ]
}
```

**Configuration:**
Request size limits are configured via `HttpResilienceOptions` in `appsettings.json`:
```json
{
  "HttpResilienceOptions": {
    "MaxRequestSizeBytes": 1048576,
    "MaxRequestHeadersTotalSize": 32768,
    "MaxRequestHeaderCount": 100,
    "MaxFormValueCount": 1024
  }
}
```

**Note:** Kestrel automatically enforces these limits at the server level. When exceeded, Kestrel throws `BadHttpRequestException` which is caught by the exception handling middleware to provide a standardized JSON error response.

### 7. `Exception` (Catch-All)

**HTTP Status Code:** `500 Internal Server Error`  
**Log Level:** `Error` (default)  
**Message:** Generic message with ErrorId (unless it's `ApplicationException` or `UnauthorizedAccessException`)

**When It Occurs:**
- Any unhandled exception not caught by specific handlers
- Unexpected errors in application code
- Third-party library exceptions

**Example:**
```csharp
// Any unexpected exception
throw new Exception("Unexpected error occurred.");
```

---

## Implementation Details

### Middleware Registration

The middleware is registered in `MiddlewareExtensions.ConfigureMiddleware()`:

```csharp
app.UseExceptionHandling(options =>
{
    options.AddResponseDetails = UpdateApiErrorResponse;
    options.DetermineLogLevel = DetermineLogLevel;
});
```

### Structured Logging Template

The middleware uses a consistent structured logging template defined as a constant:

```csharp
private const string LogTemplate = "Area: {Area}, RequestPath: {RequestPath}, RequestMethod: {RequestMethod}, ErrorId: {ErrorId}, Message: {Message}";
```

This template follows the project's logging guidelines and ensures all exception logs have the same structure for easy querying and analysis.

### Exception Processing

All exceptions are processed through the common `ProcessExceptionAsync` method, which:

1. **Generates or Retrieves Error ID**
   ```csharp
   string errorId = GetOrCreateErrorId(exception);
   ```

2. **Creates ApiError Object**
   ```csharp
   List<ApiError> apiErrors = new()
   {
       new ApiError
       {
           ErrorId = errorId,
           StatusCode = (short)statusCode,
           Message = finalMessage
       }
   };
   ```

3. **Constructs Response**
   ```csharp
   Response<ApiError> errorResponse = new(null)
   {
       Succeeded = false,
       Errors = apiErrors
   };
   ```

4. **Applies Custom Response Details** (if configured)
   ```csharp
   _options.AddResponseDetails?.Invoke(context, exception, errorResponse);
   ```

5. **Determines Log Level**
   ```csharp
   LogLevel level = _options.DetermineLogLevel?.Invoke(exception) ?? defaultLogLevel;
   ```

6. **Logs Exception with Structured Logging**
   ```csharp
   // Build log message with inner exception details
   string logMessage = string.IsNullOrWhiteSpace(innerExMessage) 
       ? finalMessage 
       : $"{finalMessage} Inner exception: {innerExMessage}";

   // Use structured logging template following project guidelines
   _logger.Log(
       level,
       exception,
       LogTemplate,  // Constant: "Area: {Area}, RequestPath: {RequestPath}, RequestMethod: {RequestMethod}, ErrorId: {ErrorId}, Message: {Message}"
       area,         // e.g., "ExceptionHandlingMiddleware.HandleExceptionAsync"
       context.Request.Path,
       context.Request.Method,
       errorId,
       logMessage);
   ```

7. **Writes Response**
   ```csharp
   return WriteErrorResponseAsync(context, errorResponse, statusCode);
   ```

### Error ID Management

Error IDs can be propagated through exception layers:

```csharp
// In service layer
try
{
    // Some operation
}
catch (Exception ex)
{
    ex.Data["ErrorId"] = Guid.NewGuid().ToString();
    throw; // Re-throw with error ID preserved
}
```

The middleware will reuse the existing error ID if present, ensuring the same error ID is used throughout the call stack.

### Inner Exception Handling

The middleware extracts the innermost exception message for logging:

```csharp
private static string GetInnermostExceptionMessage(Exception exception)
{
    Exception current = exception;
    while (current.InnerException != null)
    {
        current = current.InnerException;
    }
    return current.Message;
}
```

This ensures that the root cause message is logged, even when exceptions are wrapped.

---

## Response Format

### Success Response Format

```json
{
  "succeeded": true,
  "data": { /* response data */ },
  "message": "Operation completed successfully",
  "errors": null
}
```

### Error Response Format

```json
{
  "succeeded": false,
  "data": null,
  "message": "",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440000",
      "statusCode": 400,
      "message": "Invalid argument: Customer ID must be greater than zero."
    }
  ]
}
```

### Response Properties

- **`succeeded`**: `false` for error responses
- **`data`**: `null` for error responses
- **`message`**: Empty string for error responses (error details are in `errors`)
- **`errors`**: Array of `ApiError` objects containing:
  - **`errorId`**: Unique identifier for tracking the error
  - **`statusCode`**: HTTP status code (e.g., 400, 404, 500)
  - **`message`**: Human-readable error message

### HTTP Status Codes

The middleware returns appropriate HTTP status codes:

| Exception Type | HTTP Status Code |
|---------------|------------------|
| `OperationCanceledException` | 499 (Client Closed Request) |
| `ArgumentException` | 400 (Bad Request) |
| `UnauthorizedAccessException` | 403 (Forbidden) |
| `KeyNotFoundException` | 404 (Not Found) |
| `InvalidOperationException` (not found) | 404 (Not Found) |
| `BadHttpRequestException` (413) | 413 (Request Entity Too Large) |
| `Exception` (catch-all) | 500 (Internal Server Error) |

---

## Configuration

### Basic Configuration

The middleware is configured in `MiddlewareExtensions.ConfigureMiddleware()`:

```csharp
app.UseExceptionHandling(options =>
{
    // Configure custom response details
    options.AddResponseDetails = UpdateApiErrorResponse;
    
    // Configure log level determination
    options.DetermineLogLevel = DetermineLogLevel;
});
```

### ExceptionHandlingOptions

The `ExceptionHandlingOptions` class provides two configurable properties:

#### 1. `AddResponseDetails`

An optional action to add custom details to error responses based on exception type:

```csharp
private static void UpdateApiErrorResponse(
    HttpContext context, 
    Exception ex, 
    Response<ApiError> apiError)
{
    // Add database-specific error handling
    if (ex.GetType().Name.Contains("PostgresException", StringComparison.OrdinalIgnoreCase) ||
        ex.GetType().Name.Contains("SqlException", StringComparison.OrdinalIgnoreCase))
    {
        apiError.Message = "A database error occurred. Please contact support if the problem persists.";
    }
}
```

#### 2. `DetermineLogLevel`

An optional function to determine log level based on exception type and message:

```csharp
private static LogLevel DetermineLogLevel(Exception ex)
{
    // Database connection errors should be logged as Critical
    if (ex.Message.Contains("error occurred using the connection to database", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("a network-related", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("connection to the database", StringComparison.OrdinalIgnoreCase))
    {
        return LogLevel.Critical;
    }

    // Operation canceled exceptions are informational (expected behavior)
    if (ex is OperationCanceledException)
    {
        return LogLevel.Information;
    }

    // Argument exceptions are warnings (client errors)
    if (ex is ArgumentException)
    {
        return LogLevel.Warning;
    }

    // Not found exceptions are informational (expected in some cases)
    if (ex is KeyNotFoundException || 
        (ex is InvalidOperationException && ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)))
    {
        return LogLevel.Information;
    }

    // Default to Error for unexpected exceptions
    return LogLevel.Error;
}
```

---

## Customization

### Adding Custom Exception Handlers

To add a new exception type handler:

1. **Add catch block in `InvokeAsync`**:
   ```csharp
   catch (CustomException ex)
   {
       await HandleCustomExceptionAsync(context, ex);
   }
   ```

2. **Create handler method**:
   ```csharp
   private Task HandleCustomExceptionAsync(HttpContext context, Exception exception)
   {
       return ProcessExceptionAsync(
           context,
           exception,
           HttpStatusCode.BadRequest, // Appropriate status code
           exception.Message,
           LogLevel.Warning, // Appropriate log level
           "ExceptionHandlingMiddleware.HandleCustomExceptionAsync"); // Area identifier
   }
   ```

### Customizing Error Messages

Error messages can be customized in the handler methods:

```csharp
private Task HandleArgumentExceptionAsync(HttpContext context, Exception exception)
{
    // Customize message
    string customMessage = $"Validation failed: {exception.Message}";
    
    return ProcessExceptionAsync(
        context,
        exception,
        HttpStatusCode.BadRequest,
        customMessage, // Use custom message
        LogLevel.Warning,
        "ExceptionHandlingMiddleware.HandleArgumentExceptionAsync"); // Area identifier
}
```

### Custom Response Enhancement

Enhance error responses based on exception type in `AddResponseDetails`:

```csharp
options.AddResponseDetails = (context, ex, response) =>
{
    // Add custom error details
    if (ex is BusinessException businessEx)
    {
        response.Errors?.FirstOrDefault()?.Message = businessEx.UserFriendlyMessage;
    }
    
    // Add additional context
    if (context.Request.Headers.ContainsKey("X-Request-Id"))
    {
        // Add request ID to response if needed
    }
};
```

---

## Best Practices

### 1. **Use Appropriate Exception Types**

Use specific exception types that map to appropriate HTTP status codes:

- **`ArgumentException`** for validation errors (400)
- **`KeyNotFoundException`** for missing resources (404)
- **`UnauthorizedAccessException`** for authorization failures (403)
- **`InvalidOperationException`** for business logic violations (can be 400 or 404)
- **`BadHttpRequestException`** is automatically thrown by Kestrel when request size limits are exceeded (413) - no need to throw manually

### 2. **Provide Meaningful Error Messages**

Exception messages should be clear and actionable:

```csharp
// ❌ Bad
throw new ArgumentException("Invalid input");

// ✅ Good
throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
```

### 3. **Preserve Error IDs Through Layers**

When catching and re-throwing exceptions, preserve error IDs:

```csharp
try
{
    await _service.ProcessAsync(data);
}
catch (Exception ex)
{
    // Preserve existing error ID or create new one
    if (string.IsNullOrEmpty(ex.Data["ErrorId"]?.ToString()))
    {
        ex.Data["ErrorId"] = Guid.NewGuid().ToString();
    }
    throw;
}
```

### 4. **Use Structured Logging**

The middleware uses structured logging with a consistent template format. All exceptions are logged with:
- **Area**: Handler method identifier (e.g., `ExceptionHandlingMiddleware.HandleExceptionAsync`)
- **RequestPath**: HTTP request path
- **RequestMethod**: HTTP method
- **ErrorId**: Unique error correlation ID
- **Message**: Combined error message with inner exception details

Ensure exception messages are suitable for logging:

```csharp
// ✅ Good - Clear, structured message
throw new ArgumentException($"Customer with ID {customerId} not found.", nameof(customerId));

// ❌ Bad - Unclear message
throw new Exception("Error");
```

**Log Output Example:**
```
Area: ExceptionHandlingMiddleware.HandleArgumentExceptionAsync, RequestPath: /api/v1/customers/123, RequestMethod: GET, ErrorId: 550e8400-e29b-41d4-a716-446655440000, Message: Customer ID must be greater than zero. Inner exception: Value cannot be null.
```

### 5. **Don't Expose Sensitive Information**

Avoid exposing sensitive information in exception messages:

```csharp
// ❌ Bad - Exposes database connection details
throw new Exception($"Database connection failed: {connectionString}");

// ✅ Good - Generic message
throw new Exception("Database connection failed. Please contact support.");
```

### 6. **Handle Expected Exceptions Locally**

Handle expected exceptions in controllers/services when appropriate:

```csharp
// ✅ Good - Handle expected exception locally
try
{
    var customer = await _service.GetCustomerAsync(id);
    return Ok(Response<CustomerDto>.Success(customer));
}
catch (KeyNotFoundException)
{
    return NotFound(Response<CustomerDto>.NotFound($"Customer with ID {id} not found."));
}
```

The middleware will catch any unhandled exceptions.

### 7. **Use Business Exceptions for Domain Logic**

Create custom business exceptions for domain-specific errors:

```csharp
public class CustomerNotFoundException : InvalidOperationException
{
    public CustomerNotFoundException(int customerId) 
        : base($"Customer with ID {customerId} not found.")
    {
    }
}
```

These will be caught by the appropriate handler based on their base type.

---

## Troubleshooting

### Issue: Error responses not in expected format

**Symptoms:** Error responses don't match the `Response<ApiError>` format.

**Solution:** Ensure the exception handling middleware is registered early in the pipeline, before other middleware that might catch exceptions.

### Issue: Error IDs not being preserved

**Symptoms:** Different error IDs in logs vs. response, or error IDs not propagating through layers.

**Solution:** Ensure error IDs are set in `exception.Data["ErrorId"]` before throwing:

```csharp
ex.Data["ErrorId"] = Guid.NewGuid().ToString();
throw;
```

### Issue: Sensitive information in error messages

**Symptoms:** Stack traces or internal details exposed in error responses.

**Solution:** Review exception messages and ensure generic messages are used for unexpected exceptions. The middleware already handles this, but verify custom exception types.

### Issue: Incorrect HTTP status codes

**Symptoms:** Wrong HTTP status codes returned for specific exception types.

**Solution:** Verify exception types match the expected types in the middleware handlers. Check if custom exceptions inherit from the correct base types.

### Issue: Logs not appearing

**Symptoms:** Exceptions not logged or logged at wrong level.

**Solution:** 
1. Verify `DetermineLogLevel` function is configured correctly
2. Check log level configuration in `appsettings.json`
3. Ensure logger is properly injected into middleware

### Issue: Custom response details not applied

**Symptoms:** `AddResponseDetails` action not modifying error responses.

**Solution:** 
1. Verify `AddResponseDetails` is configured in middleware registration
2. Check that the action is not throwing exceptions (wrap in try-catch if needed)
3. Ensure response object is not null

### Issue: Inner exception message not captured

**Symptoms:** Only outer exception message logged, not root cause.

**Solution:** The middleware automatically extracts innermost exception messages. If this isn't working, verify exception chain is properly constructed when throwing exceptions.

---

## Summary

The global exception handling middleware provides:

✅ **Standardized error responses** across the entire API  
✅ **Error correlation IDs** for tracking and debugging  
✅ **Structured logging** with consistent template format following project guidelines  
✅ **Security** by preventing information leakage  
✅ **Appropriate HTTP status codes** based on exception types  
✅ **Configurability** for custom response details and log levels  
✅ **DRY principles** with centralized exception processing  
✅ **Consistent log format** with Area, RequestPath, RequestMethod, ErrorId, and Message fields  

**Structured Logging Template:**
All exceptions are logged using the format:
```
Area: {Area}, RequestPath: {RequestPath}, RequestMethod: {RequestMethod}, ErrorId: {ErrorId}, Message: {Message}
```

This ensures consistent error handling, improved debugging capabilities, better log analysis, and enhanced user experience across the WebShop API.

