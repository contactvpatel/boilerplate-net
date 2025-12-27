# ValidationFilter Implementation Guide

## Overview

The `ValidationFilter` is a global action filter that automatically validates incoming request models using FluentValidation and returns standardized error responses. This filter ensures consistent validation behavior across all API endpoints without requiring manual validation code in controllers.

## Table of Contents

- [Why ValidationFilter Exists](#why-validationfilter-exists)
- [Why FluentValidation?](#why-fluentvalidation)
- [What Problem It Solves](#what-problem-it-solves)
- [How It Works](#how-it-works)
- [Architecture & Design](#architecture--design)
- [Benefits](#benefits)
- [Implementation Details](#implementation-details)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Why ValidationFilter Exists

### The Problem with Manual Validation

Before implementing `ValidationFilter`, controllers had to manually check `ModelState.IsValid` in every action method:

```csharp
[HttpPost]
public async Task<ActionResult<Response<ProductDto>>> Create([FromBody] CreateProductDto createDto)
{
    if (!ModelState.IsValid)
    {
        // Manual error collection and formatting
        List<ApiError> errors = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = 400,
                Message = $"{x.Key}: {e.ErrorMessage}"
            }))
            .ToList();
        return BadRequest(new Response<ProductDto>(null, false, "Validation failed", errors));
    }
    
    // Actual business logic...
}
```

**Issues with this approach:**
- **Code Duplication**: Same validation check repeated in every controller action
- **Inconsistency**: Different error formatting across endpoints
- **Maintenance Burden**: Changes to validation logic require updates in multiple places
- **Violates DRY Principle**: Don't Repeat Yourself
- **Easy to Forget**: Developers might skip validation checks

### The Solution

`ValidationFilter` centralizes validation logic and automatically:
1. Validates all incoming request models using FluentValidation
2. Returns standardized error responses
3. Logs validation failures with context
4. Eliminates the need for manual validation in controllers

## Why FluentValidation?

### Comparison with Alternatives

#### **Data Annotations**

**Limitations:**
- **Limited Expressiveness**: Hard to express complex business rules
- **Tight Coupling**: Validation attributes mixed with DTOs, violating separation of concerns
- **No Dependency Injection**: Cannot inject services for validation (e.g., checking database)
- **Limited Reusability**: Rules are tied to specific properties
- **Hard to Test**: Validation logic embedded in attributes is harder to unit test
- **No Conditional Validation**: Difficult to implement "validate if X, then Y" scenarios

**Example (Data Annotations):**
```csharp
public class CreateProductDto
{
    [Required(ErrorMessage = "Product name is required.")]
    [MaxLength(500, ErrorMessage = "Product name must not exceed 500 characters.")]
    public string Name { get; set; } = string.Empty;
    
    // Complex rules are difficult or impossible
    // Cannot check database for duplicate names
    // Cannot validate based on other properties easily
}
```

#### **Manual Validation in Controllers**

**Limitations:**
- **Code Duplication**: Same validation logic repeated across controllers
- **Inconsistency**: Different validation approaches in different places
- **Maintenance Burden**: Changes require updates in multiple locations
- **No Reusability**: Validation logic cannot be shared
- **Hard to Test**: Validation mixed with controller logic

**Example (Manual Validation):**
```csharp
[HttpPost]
public async Task<ActionResult> Create([FromBody] CreateProductDto dto)
{
    // Validation scattered throughout controller
    if (string.IsNullOrWhiteSpace(dto.Name))
    {
        return BadRequest("Name is required.");
    }
    if (dto.Name.Length > 500)
    {
        return BadRequest("Name too long.");
    }
    // More validation...
    // Business logic...
}
```

#### **FluentValidation - The Chosen Solution**

**Advantages:**

1. **Separation of Concerns**
   - Validators are separate classes, keeping DTOs clean
   - Validation logic is isolated from business logic
   - Follows Single Responsibility Principle

2. **Expressiveness**
   - Fluent, readable syntax for complex validation rules
   - Easy to express conditional validation
   - Supports custom validation logic

3. **Dependency Injection**
   - Validators can inject services (repositories, other services)
   - Enables database-backed validation (e.g., check for duplicates)
   - Supports async validation operations

4. **Testability**
   - Validators are plain C# classes, easy to unit test
   - Can test validation logic independently
   - No framework dependencies in tests

5. **Reusability**
   - Validators can be composed and reused
   - Base validators for common rules
   - Validator inheritance for shared rules

6. **Complex Rules Support**
   - Cross-property validation
   - Conditional validation (When/Unless)
   - Custom validators for business-specific rules
   - Async validation for database checks

7. **Better Error Messages**
   - Context-aware error messages
   - Property-level and rule-level messages
   - Support for localization

**Example (FluentValidation):**
```csharp
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    private readonly IProductRepository _productRepository;
    
    public CreateProductDtoValidator(IProductRepository productRepository)
    {
        _productRepository = productRepository;
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(500)
            .WithMessage("Product name must not exceed 500 characters.")
            .MustAsync(async (name, cancellation) => 
                !await _productRepository.ExistsByNameAsync(name, cancellation))
            .WithMessage("A product with this name already exists.");
        
        // Complex conditional validation
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .When(x => x.IsActive)
            .WithMessage("Active products must have a price greater than 0.");
        
        // Cross-property validation
        RuleFor(x => x.DiscountPrice)
            .LessThan(x => x.Price)
            .When(x => x.DiscountPrice.HasValue)
            .WithMessage("Discount price must be less than regular price.");
    }
}
```

### Benefits of FluentValidation

#### 1. **Clean DTOs**
DTOs remain focused on data structure, not validation rules:
```csharp
// Clean DTO - no validation attributes
public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

#### 2. **Testable Validation Logic**
Validators are easy to unit test:
```csharp
[Fact]
public void CreateProductDtoValidator_ShouldFail_WhenNameIsEmpty()
{
    var validator = new CreateProductDtoValidator(mockRepository);
    var dto = new CreateProductDto { Name = "" };
    
    var result = validator.Validate(dto);
    
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == "Name");
}
```

#### 3. **Database-Backed Validation**
Validators can check database for business rules:
```csharp
RuleFor(x => x.Email)
    .MustAsync(async (email, cancellation) => 
        !await _userRepository.EmailExistsAsync(email, cancellation))
    .WithMessage("This email is already registered.");
```

#### 4. **Conditional Validation**
Validate based on other properties:
```csharp
RuleFor(x => x.PhoneNumber)
    .NotEmpty()
    .When(x => x.ContactMethod == ContactMethod.Phone)
    .WithMessage("Phone number is required when contact method is Phone.");
```

#### 5. **Composable Rules**
Reuse validation rules across validators:
```csharp
public class BaseProductValidator : AbstractValidator<ProductDto>
{
    protected BaseProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(500);
    }
}

public class CreateProductDtoValidator : BaseProductValidator
{
    public CreateProductDtoValidator()
    {
        // Inherits base rules, adds specific rules
        RuleFor(x => x.Name)
            .MustAsync(async (name, cancellation) => 
                !await _productRepository.ExistsByNameAsync(name, cancellation));
    }
}
```

#### 6. **Async Validation**
Support for asynchronous validation operations:
```csharp
RuleFor(x => x.Username)
    .MustAsync(async (username, cancellation) => 
        await _userService.IsUsernameAvailableAsync(username, cancellation))
    .WithMessage("Username is already taken.");
```

#### 7. **Custom Validators**
Create reusable custom validators:
```csharp
public class ValidEmailValidator : PropertyValidator<string, string>
{
    protected override bool IsValid(PropertyValidatorContext<string> context)
    {
        return EmailAddressAttribute.IsValid(context.PropertyValue);
    }
}

// Usage
RuleFor(x => x.Email)
    .SetValidator(new ValidEmailValidator());
```

### Integration with ValidationFilter

FluentValidation works seamlessly with `ValidationFilter`:

1. **Automatic Execution**: FluentValidation validators run automatically via `AddFluentValidationAutoValidation()`
2. **ModelState Population**: Validation errors are automatically added to `ModelState`
3. **Filter Processing**: `ValidationFilter` reads from `ModelState` and formats standardized responses
4. **No Controller Code**: Controllers don't need any validation code

This combination provides:
- **Automatic Validation**: No manual checks needed
- **Consistent Responses**: Standardized error format
- **Separation of Concerns**: Validation logic separate from controllers
- **Testability**: Both validators and filters can be tested independently

## What Problem It Solves

### 1. **Consistency**
All validation errors are returned in the same format across all endpoints, ensuring a consistent API experience for clients.

### 2. **Separation of Concerns**
Controllers focus on business logic, not validation. Validation is handled at the filter level, following the Single Responsibility Principle.

### 3. **Maintainability**
Validation logic is centralized. Changes to error formatting or logging only need to be made in one place.

### 4. **Developer Experience**
Developers don't need to remember to add validation checks. It happens automatically for all endpoints.

### 5. **Observability**
All validation failures are logged with context (controller, action, version), making debugging easier.

## How It Works

### Execution Flow

```
1. Client sends HTTP request with request body
   ↓
2. ASP.NET Core model binding populates ModelState
   ↓
3. FluentValidation validators execute automatically
   ↓
4. ValidationFilter.OnActionExecutionAsync() is called
   ↓
5. Filter checks ModelState.IsValid
   ↓
6a. If invalid:
    - Logs validation failure
    - Transforms errors to ApiError objects
    - Returns BadRequest with Response<T> model
    - Stops execution (doesn't call next())
   ↓
6b. If valid:
    - Calls next() to continue to controller action
```

### Integration with FluentValidation

The filter works seamlessly with FluentValidation:

1. **FluentValidation Auto-Validation**: Configured via `AddFluentValidationAutoValidation()` in `Core/ServiceExtensions.cs`
2. **Validator Registration**: All validators are registered from the Business assembly
3. **ModelState Population**: FluentValidation automatically populates `ModelState` with validation errors
4. **Filter Processing**: `ValidationFilter` reads from `ModelState` and formats the response

## Architecture & Design

### Filter Registration

The filter is registered globally in `Core/ServiceExtensions.cs`:

```csharp
services.AddControllers(options =>
{
    options.Filters.Add<JwtTokenAuthenticationFilter>();
    options.Filters.Add<ValidationFilter>(); // Global filter
});
```

**Why Global?**
- Ensures all endpoints are validated consistently
- No need to remember to add `[ServiceFilter(typeof(ValidationFilter))]` to each controller
- Automatic validation for all incoming requests

### Filter Order

Filters execute in this order:
1. `JwtTokenAuthenticationFilter` (authentication)
2. `ValidationFilter` (validation)
3. Controller action

**Why this order?**
- Authentication happens first (security)
- Validation happens after authentication (efficiency - don't validate if not authenticated)
- Business logic executes last

## Benefits

### 1. **Reduced Code Duplication**
- **Before**: Validation code in every controller action (~10-15 lines per action)
- **After**: Zero validation code in controllers

### 2. **Consistent Error Responses**
All validation errors follow the same structure:
```json
{
  "succeeded": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    {
      "errorId": "guid",
      "statusCode": 400,
      "message": "Name: Product name is required."
    }
  ]
}
```

### 3. **Better Logging**
Validation failures are logged with full context:
- Controller name
- Action name
- API version
- Error details

### 4. **Easier Testing**
- Validation logic is isolated in the filter
- Controllers can be tested without worrying about validation
- Filter can be tested independently

### 5. **Performance**
- Validation happens early in the pipeline
- Invalid requests are rejected before reaching business logic
- Reduces unnecessary database queries

## Implementation Details

### Filter Implementation

```csharp
public class ValidationFilter(ILogger<ValidationFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, 
        ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            // Log validation failure
            // Transform errors to ApiError
            // Return BadRequest
            return;
        }
        
        await next(); // Continue to controller
    }
}
```

### Error Transformation

The filter transforms `ModelState` errors into `ApiError` objects:

```csharp
List<ApiError> errors = context.ModelState
    .Where(x => x.Value?.Errors.Count > 0)
    .SelectMany(x => x.Value!.Errors.Select(e => new ApiError
    {
        ErrorId = Guid.NewGuid().ToString(),
        StatusCode = (short)HttpStatusCode.BadRequest,
        Message = $"{x.Key}: {e.ErrorMessage}"
    }))
    .ToList();
```

**Error Format**: `"{PropertyName}: {ErrorMessage}"`
- Example: `"Name: Product name is required."`
- Example: `"Email: Email address must be in a valid format."`

### Logging

Validation failures are logged at `Warning` level with structured logging:

```csharp
_logger.LogWarning(
    LogTemplate,
    "ValidationFilter.OnActionExecutionAsync",
    action,
    controller,
    version,
    "Model validation failed");
```

**Log Template**: `"Area: {Area}, Action: {Action}, Controller: {Controller}, Version: {Version}, Message: {Message}"`

## Configuration

### FluentValidation Setup

In `Core/ServiceExtensions.cs`:

```csharp
private static void ConfigureFluentValidation(this IServiceCollection services)
{
    // Register all validators from Business assembly
    services.AddValidatorsFromAssemblyContaining<CreateProductDto>();
    
    // Enable automatic validation
    services.AddFluentValidationAutoValidation();
    
    // Disable default ASP.NET Core model validation
    services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });
}
```

### Creating Validators

Validators are created in `src/WebShop.Business/Validators/`:

```csharp
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(500)
            .WithMessage("Product name must not exceed 500 characters.");
    }
}
```

## Usage Examples

### Example 1: Valid Request

**Request:**
```http
POST /api/v1/products
Content-Type: application/json

{
  "name": "T-Shirt",
  "category": "Clothing",
  "gender": "unisex"
}
```

**Response:** `201 Created` (validation passes, product created)

### Example 2: Invalid Request

**Request:**
```http
POST /api/v1/products
Content-Type: application/json

{
  "name": "",
  "category": "A very long category name that exceeds the maximum length of 100 characters and should fail validation"
}
```

**Response:** `400 Bad Request`
```json
{
  "succeeded": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    {
      "errorId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "statusCode": 400,
      "message": "Name: Product name is required."
    },
    {
      "errorId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "statusCode": 400,
      "message": "Category: Category must not exceed 100 characters."
    }
  ]
}
```

### Example 3: Controller Code (No Validation Needed)

```csharp
[HttpPost]
[ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<Response<ProductDto>>> Create(
    [FromBody] CreateProductDto createDto, 
    CancellationToken cancellationToken)
{
    // No validation check needed - ValidationFilter handles it!
    ProductDto product = await _productService.CreateAsync(createDto, cancellationToken);
    Response<ProductDto> response = new(product, "Product created successfully");
    return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
}
```

## Best Practices

### 1. **Use FluentValidation for Complex Rules**
- Business rules (e.g., age requirements, format validation)
- Cross-field validation
- Custom validation logic

### 2. **Keep Validators in Business Layer**
- Validators are business logic, not infrastructure
- Located in `src/WebShop.Business/Validators/`
- Follows Clean Architecture principles

### 3. **Provide Clear Error Messages**
```csharp
// Good
.WithMessage("Product name is required.")

// Bad
.WithMessage("Invalid.")
```

### 4. **Use Consistent Naming**
- Validator class: `{DtoName}Validator`
- File name: `{DtoName}Validator.cs`
- Example: `CreateProductDtoValidator.cs`

### 5. **Don't Skip Validation in Controllers**
- The filter handles it automatically
- Removing validation code from controllers is intentional
- Trust the filter to do its job

### 6. **Test Validators Independently**
```csharp
[Fact]
public void CreateProductDtoValidator_ShouldFail_WhenNameIsEmpty()
{
    var validator = new CreateProductDtoValidator();
    var dto = new CreateProductDto { Name = "" };
    
    var result = validator.Validate(dto);
    
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == "Name");
}
```

## Troubleshooting

### Issue: Validation Not Running

**Symptoms:** Invalid requests reach controller actions

**Solutions:**
1. Verify `ValidationFilter` is registered in `Core/ServiceExtensions.cs`
2. Check that `AddFluentValidationAutoValidation()` is called
3. Ensure validators are registered: `AddValidatorsFromAssemblyContaining<>()`
4. Verify `SuppressModelStateInvalidFilter = true` is set

### Issue: Errors Not Formatted Correctly

**Symptoms:** Errors don't match expected `Response<T>` format

**Solutions:**
1. Check `ValidationFilter` error transformation logic
2. Verify `ApiError` model structure
3. Ensure `Response<T>` constructor is used correctly

### Issue: Validation Runs Twice

**Symptoms:** Validation errors appear twice in response

**Solutions:**
1. Ensure `SuppressModelStateInvalidFilter = true`
2. Remove any manual `ModelState.IsValid` checks in controllers
3. Verify only one `ValidationFilter` is registered

### Issue: Validators Not Found

**Symptoms:** Validation doesn't run, no errors

**Solutions:**
1. Check validator class names end with `Validator`
2. Verify validators inherit from `AbstractValidator<T>`
3. Ensure validators are in the Business assembly
4. Check `AddValidatorsFromAssemblyContaining<>()` uses correct type

## Related Documentation

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [ASP.NET Core Filters](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters)
- [Standardized Response Model](../src/WebShop.Api/Models/Response.cs)

## Summary

The `ValidationFilter` provides:
- ✅ Automatic validation for all endpoints
- ✅ Consistent error responses
- ✅ Centralized validation logic
- ✅ Better developer experience
- ✅ Improved maintainability
- ✅ Enhanced observability

By using `ValidationFilter`, developers can focus on business logic while ensuring all incoming requests are properly validated and errors are returned in a consistent, standardized format.

