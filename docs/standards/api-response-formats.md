# API Response Formats Guide

This comprehensive guide documents all possible response formats returned by the WebShop .NET API, including success responses, error responses, validation failures, and special cases handled by middleware and filters.

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Response Structure](#response-structure)
- [Success Responses](#success-responses)
- [Error Responses](#error-responses)
- [Validation Errors](#validation-errors)
- [Authentication & Authorization Errors](#authentication--authorization-errors)
- [Rate Limiting Responses](#rate-limiting-responses)
- [Middleware Responses](#middleware-responses)
- [Health Check Responses](#health-check-responses)
- [Special Headers](#special-headers)

---

## Overview

The WebShop API uses standardized JSON response formats for all endpoints. Responses follow a consistent structure with proper HTTP status codes, structured error information, and correlation IDs for debugging.

**Key Principles:**
- **Consistent Format**: All responses use the same `Response<T>` wrapper
- **Structured Errors**: Errors include unique IDs, status codes, and detailed messages
- **Correlation**: All responses include trace IDs for request tracking
- **Validation**: Automatic model validation with detailed error reporting
- **Security**: Sensitive data is automatically masked/removed

---

## Response Structure

All API responses follow this standardized JSON structure:

```json
{
  "succeeded": true|false,
  "data": { ... } | null,
  "message": "Human-readable message",
  "errors": [
    {
      "errorId": "unique-guid",
      "statusCode": 400,
      "message": "Detailed error description"
    }
  ] | null
}
```

### Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `succeeded` | boolean | Operation success status |
| `data` | object \| null | Response payload (null for errors) |
| `message` | string | Human-readable operation description |
| `errors` | array \| null | Error details (null for success) |

---

## Success Responses

### 200 OK - Standard Success

**Description**: Request completed successfully with data.

**Example Response:**
```json
{
  "succeeded": true,
  "data": {
    "id": 123,
    "name": "John Doe",
    "email": "john.doe@[REDACTED]",
    "createdAt": "2024-01-15T10:30:00Z"
  },
  "message": "Customer retrieved successfully",
  "errors": null
}
```

**HTTP Status**: `200 OK`

**Usage**: GET, PUT, PATCH operations that return data

### 201 Created - Resource Created

**Description**: New resource successfully created.

**Example Response:**
```json
{
  "succeeded": true,
  "data": {
    "id": 456,
    "name": "New Product",
    "sku": "PROD-2024-001",
    "price": 99.99,
    "createdAt": "2024-01-15T10:30:00Z"
  },
  "message": "Product created successfully",
  "errors": null
}
```

**HTTP Status**: `201 Created`

**Usage**: POST operations that create new resources

### 200 OK - Paginated Results

**Description**: Successful paginated query with metadata.

**Example Response:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      { "id": 1, "name": "Product A" },
      { "id": 2, "name": "Product B" }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasPreviousPage": false,
    "hasNextPage": true,
    "firstItemIndex": 1,
    "lastItemIndex": 20
  },
  "message": "Retrieved page 1 of 8 (20 of 150 total products)",
  "errors": null
}
```

**HTTP Status**: `200 OK`

**Usage**: GET operations with pagination parameters

---

## Error Responses

### 400 Bad Request - General Error

**Description**: Invalid request data or business logic violation.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Invalid product data",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440000",
      "statusCode": 400,
      "message": "Product price cannot be negative"
    }
  ]
}
```

**HTTP Status**: `400 Bad Request`

**Common Causes:**
- Invalid data formats
- Business rule violations
- Missing required fields

### 404 Not Found - Resource Not Found

**Description**: Requested resource does not exist.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Customer not found",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440001",
      "statusCode": 404,
      "message": "Customer with ID 999 not found."
    }
  ]
}
```

**HTTP Status**: `404 Not Found`

**Common Causes:**
- Invalid resource ID
- Deleted or inactive resources
- Typo in URL path

### 500 Internal Server Error - Server Error

**Description**: Unexpected server error or unhandled exception.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Error occurred in the API. Please use the ErrorId [550e8400-e29b-41d4-a716-446655440002] and contact support team if the problem persists.",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440002",
      "statusCode": 500,
      "message": "An unexpected error occurred while processing your request."
    }
  ]
}
```

**HTTP Status**: `500 Internal Server Error`

**Common Causes:**
- Database connection failures
- External service unavailability
- Unhandled exceptions in code

### 413 Request Entity Too Large - Payload Too Large

**Description**: Request body exceeds maximum allowed size.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Request body size exceeds the maximum allowed limit. Please reduce the request size and try again.",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440003",
      "statusCode": 413,
      "message": "Request body too large. The request entity is larger than limits defined by server."
    }
  ]
}
```

**HTTP Status**: `413 Request Entity Too Large`

**Common Causes:**
- File uploads that are too large
- Bulk operations with too many items

### 499 Client Closed Request - Request Cancelled

**Description**: Client cancelled the request before completion.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Request was canceled by the client.",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440004",
      "statusCode": 499,
      "message": "Request was canceled by the client."
    }
  ]
}
```

**HTTP Status**: `499 Client Closed Request`

**Common Causes:**
- User navigates away during long-running requests
- Browser tab closed
- Network interruption

---

## Validation Errors

### 400 Bad Request - Model Validation Failed

**Description**: Request model failed automatic validation.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440005",
      "statusCode": 400,
      "message": "Name: The Name field is required."
    },
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440006",
      "statusCode": 400,
      "message": "Email: The Email field is not a valid e-mail address."
    },
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440007",
      "statusCode": 400,
      "message": "Price: The field Price must be between 0.01 and 999999.99."
    }
  ]
}
```

**HTTP Status**: `400 Bad Request`

**Validation Rules Applied:**
- Required field validation
- Data type validation
- Range/length constraints
- Custom business rules

---

## Authentication & Authorization Errors

### 401 Unauthorized - Authentication Required

**Description**: Request lacks valid authentication credentials.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Authentication failed",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440008",
      "statusCode": 401,
      "message": "Token is missing or invalid"
    }
  ]
}
```

**HTTP Status**: `401 Unauthorized`

**Common Causes:**
- Missing Authorization header
- Invalid or expired JWT token
- Malformed token format

### 401 Unauthorized - Token Expired

**Description**: JWT token has expired and needs refresh.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Authentication failed",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440009",
      "statusCode": 401,
      "message": "Token has expired"
    }
  ]
}
```

**HTTP Status**: `401 Unauthorized`

### 403 Forbidden - Access Denied

**Description**: Authentication succeeded but user lacks required permissions.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Authentication failed",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440010",
      "statusCode": 403,
      "message": "Insufficient permissions to access this resource"
    }
  ]
}
```

**HTTP Status**: `403 Forbidden`

**Common Causes:**
- User role lacks required permissions
- Resource ownership validation failed
- Administrative access restrictions

---

## Rate Limiting Responses

### 429 Too Many Requests - Rate Limit Exceeded

**Description**: Request rate limit exceeded for user/IP.

**Example Response:**
```json
{
  "succeeded": false,
  "data": null,
  "message": "Rate limit exceeded. Please try again later.",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440011",
      "statusCode": 429,
      "message": "Too many requests. Please retry after the specified time."
    }
  ]
}
```

**HTTP Headers:**
```
Retry-After: 60
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1640995200
```

**HTTP Status**: `429 Too Many Requests`

**Rate Limit Policies:**
- **Global**: 100 requests per minute per user/IP
- **Strict**: 10 requests per minute (auth endpoints)
- **Permissive**: 500 requests per minute (read endpoints)

---

## Middleware Responses

### API Version Deprecation Headers

**Description**: API version is deprecated with migration guidance.

**Response Headers:**
```
Deprecation: true
Sunset: Wed, 21 Oct 2025 07:28:00 GMT
Link: <https://api.example.com/v2/customers>; rel="successor-version"
```

**HTTP Status**: `200 OK` (response body unchanged)

**Usage**: Automatic headers added for deprecated API versions

### CORS Preflight Responses

**Description**: CORS preflight request handled automatically.

**HTTP Status**: `200 OK` or `404 Not Found`

**Headers:**
```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization
Access-Control-Max-Age: 86400
```

---

## Health Check Responses

### 200 OK - All Systems Healthy

**Description**: All health checks passed.

**Example Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "version": "1.0.0",
  "totalDuration": "125.50ms",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "PostgreSQL database connection",
      "duration": "45.20ms",
      "tags": ["database", "postgresql"]
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": "Redis cache connection",
      "duration": "12.30ms",
      "tags": ["cache", "redis"]
    }
  ]
}
```

**HTTP Status**: `200 OK`

### 503 Service Unavailable - Health Check Failed

**Description**: One or more health checks failed.

**Example Response:**
```json
{
  "status": "Unhealthy",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "version": "1.0.0",
  "totalDuration": "234.80ms",
  "checks": [
    {
      "name": "database",
      "status": "Unhealthy",
      "description": "PostgreSQL database connection failed",
      "duration": "180.50ms",
      "tags": ["database", "postgresql"],
      "exception": "NpgsqlException: Connection timeout"
    }
  ]
}
```

**HTTP Status**: `503 Service Unavailable`

### Health Check Endpoints

| Endpoint | Description | Response Format |
|----------|-------------|-----------------|
| `GET /health` | Overall health | Enhanced JSON |
| `GET /health/live` | Liveness probe | Simple JSON |
| `GET /health/ready` | Readiness probe | Simple JSON |
| `GET /health/db` | Database health | Simple JSON |

---

## Special Headers

### Standard Response Headers

**All Responses Include:**
```
Content-Type: application/json
X-Correlation-ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

### Error Response Headers

**Error Responses Include:**
```
X-Error-ID: 550e8400-e29b-41d4-a716-446655440000
```

### Rate Limiting Headers

**Rate Limited Responses Include:**
```
Retry-After: 60
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1640995200
```

### API Versioning Headers

**Versioned Responses Include:**
```
api-supported-versions: 1, 2
api-deprecated-versions: 1
```

---

## Error Handling Best Practices

### For API Consumers

1. **Check `succeeded` field first** - Primary success indicator
2. **Handle all error status codes** - Don't assume 200 = success
3. **Use error IDs for support** - Include in bug reports
4. **Respect rate limits** - Check Retry-After header
5. **Validate response structure** - Handle missing fields gracefully

### For API Developers

1. **Use consistent response format** - Always use `Response<T>` wrapper
2. **Include meaningful messages** - Help developers understand errors
3. **Add correlation IDs** - Essential for distributed tracing
4. **Mask sensitive data** - Automatic in error responses
5. **Log errors appropriately** - Include context but not secrets

### Error ID Usage

Error IDs are unique GUIDs that correlate:
- API error responses
- Server-side logs
- Distributed traces
- Support ticket tracking

**Example Support Request:**
```
Error ID: 550e8400-e29b-41d4-a716-446655440000
Endpoint: POST /api/v1/customers
Time: 2024-01-15T10:30:00Z
User: user123
```

---

## Response Time Guidelines

| Operation Type | Target Response Time | Max Acceptable |
|----------------|---------------------|----------------|
| Simple GET | <100ms | <500ms |
| Complex GET | <200ms | <1s |
| POST/PUT/PATCH | <300ms | <2s |
| Bulk operations | <2s | <10s |
| File uploads | <5s | <30s |

**Timeout Handling:**
- Client timeouts: 30 seconds
- Server timeouts: 60 seconds for complex operations
- Database timeouts: 30 seconds

---

## Testing Response Formats

### Unit Tests for Controllers

```csharp
[Fact]
public async Task GetCustomer_ReturnsNotFound_WhenCustomerDoesNotExist()
{
    // Arrange
    var mockService = new Mock<ICustomerService>();
    mockService.Setup(s => s.GetByIdAsync(999, default))
               .ReturnsAsync((CustomerDto?)null);

    var controller = new CustomerController(mockService.Object, _logger);

    // Act
    var result = await controller.GetById(999);

    // Assert
    var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
    var response = Assert.IsType<Response<CustomerDto>>(notFoundResult.Value);
    Assert.False(response.Succeeded);
    Assert.Contains("not found", response.Message.ToLower());
}
```

### Integration Tests

```csharp
[Fact]
public async Task CreateCustomer_ReturnsValidationErrors_WhenModelIsInvalid()
{
    // Arrange
    var client = _factory.CreateClient();
    var invalidCustomer = new { Name = "", Email = "invalid-email" };

    // Act
    var response = await client.PostAsJsonAsync("/api/v1/customers", invalidCustomer);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var errorResponse = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
    Assert.False(errorResponse!.Succeeded);
    Assert.Contains(errorResponse.Errors, e => e.Message.Contains("required"));
}
```

---

## Summary

The WebShop API provides consistent, structured responses across all endpoints with:

- ✅ **Standardized Format**: `Response<T>` wrapper for all responses
- ✅ **Detailed Error Information**: Unique error IDs and status codes
- ✅ **Security**: Automatic PII masking and sensitive data protection
- ✅ **Correlation**: Trace IDs for request tracking and debugging
- ✅ **Validation**: Comprehensive model validation with clear error messages
- ✅ **Rate Limiting**: Proper throttling with informative headers
- ✅ **Health Checks**: Detailed system status reporting
- ✅ **API Versioning**: Deprecation warnings and migration guidance

All responses follow RESTful conventions with appropriate HTTP status codes and structured JSON payloads designed for both human developers and automated API consumers.

---

## References

- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines) - REST and API design practices
- [RFC 7807: Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807) - Standardized error response format (aligns with our `errors` array)
- [Exception Handling](../architecture/exception-handling.md) - How exceptions map to this response format