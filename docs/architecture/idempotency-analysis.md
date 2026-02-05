# API Idempotency Analysis

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Current State Analysis](#current-state-analysis)
- [Required Fixes](#required-fixes)
- [Implementation Status](#implementation-status)
- [Summary](#summary)
- [Testing Checklist](#testing-checklist)
- [References](#references)

---

## Overview

Idempotency ensures that making the same request multiple times produces the same result as making it once. This is critical for:
- **Reliability**: Handling network retries and timeouts
- **Consistency**: Preventing duplicate operations
- **User Experience**: Safe retry behavior

## Current State Analysis

### ✅ **GET Operations** - Idempotent
All GET operations are naturally idempotent (read-only).

### ⚠️ **PUT Operations** - Partially Idempotent
- **Status**: Should be idempotent by design (full update)
- **Issue**: If resource doesn't exist, returns 404 (correct)
- **Issue**: If called multiple times with same data, should produce same result (needs verification)
- **Recommendation**: ✅ Current implementation is acceptable

### ✅ **PATCH Operations** - Idempotent
- **Status**: All services check for changes before saving ✅
- **Verified Services with Change Detection**:
  - ✅ `ProductService.PatchAsync` - Checks `hasChanges`
  - ✅ `CustomerService.PatchAsync` - Checks `hasChanges`
  - ✅ `AddressService.PatchAsync` - Checks `hasChanges`
  - ✅ `StockService.PatchAsync` - Checks `hasChanges`
  - ✅ `OrderService.PatchAsync` - Checks `hasChanges`
  - ✅ `ArticleService.PatchAsync` - Checks `hasChanges`
  - ✅ `SizeService.PatchAsync` - Checks `hasChanges`
  - ✅ `ColorService.PatchAsync` - Checks `hasChanges`
  - ✅ `LabelService.PatchAsync` - Checks `hasChanges`

### ✅ **DELETE Operations** - Idempotent (FIXED)
- **Previous Behavior**:
  - First DELETE call: Entity exists → Soft delete → Returns `true` → Controller returns 204 ✅
  - Second DELETE call: Entity not found (filtered by `IsActive`) → Returns `false` → Controller returns 404 ❌
- **Fixed Behavior**:
  - First DELETE call: Entity exists → Soft delete → Returns `true` → Controller returns 204 ✅
  - Second DELETE call: Entity exists but soft-deleted → Returns `true` → Controller returns 204 ✅ (Idempotent)
  - DELETE on never-existed entity: Returns `false` → Controller returns 404 ✅ (Correct)
- **Solution Implemented**: Added `ExistsAsync(id, includeSoftDeleted: true)` to check existence including soft-deleted entities

### ❌ **POST Operations** - NOT Idempotent
- **Current Behavior**: No idempotency key support
- **Problem**: Retrying a POST creates duplicate resources
- **Solution**: Implement idempotency key support using `Idempotency-Key` header

### ⚠️ **Batch Operations** - Partially Idempotent
- **Status**: Handle non-existent resources gracefully (skip them)
- **Issue**: No idempotency key support for batch POST operations
- **Recommendation**: Add idempotency key support for batch creates

## Required Fixes

### 1. ✅ Fix DELETE Operations (High Priority) - COMPLETED
**Problem**: DELETE returns 404 on already-deleted resources instead of 204.

**Solution**: Add `ExistsAsync(int id, bool includeSoftDeleted = false)` method to repository:
```csharp
public virtual async Task<bool> ExistsAsync(int id, bool includeSoftDeleted = false, CancellationToken cancellationToken = default)
{
    string whereClause = includeSoftDeleted
        ? @"""id"" = @Id"
        : @"""id"" = @Id AND ""isactive"" = true";

    string sql = $@"SELECT EXISTS(SELECT 1 FROM ""{Schema}"".""{TableName}"" WHERE {whereClause})";

    using IDbConnection connection = GetReadConnection();
    bool exists = await connection.QueryFirstOrDefaultAsync<bool>(
        new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken))
        .ConfigureAwait(false);

    return exists;
}
```

**Implementation Completed**: All service `DeleteAsync` methods have been updated with the following pattern:
```csharp
public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
{
    // Check if entity exists (including soft-deleted) for idempotency
    bool exists = await _repository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
    
    if (!exists)
    {
        // Never existed - return false (controller will return 404)
        _logger.LogWarning("Entity not found for deletion. EntityId: {EntityId}", id);
        return false;
    }
    
    // Check if already soft-deleted
    T? entity = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
    if (entity == null)
    {
        // Already soft-deleted - return true for idempotency (controller will return 204)
        _logger.LogInformation("Entity already deleted. EntityId: {EntityId}", id);
        return true;
    }
    
    // Perform soft delete
    await _repository.DeleteAsync(entity, cancellationToken).ConfigureAwait(false);
    await // Dapper commits immediately(cancellationToken).ConfigureAwait(false);
    _logger.LogInformation("Entity deleted successfully. EntityId: {EntityId}", id);
    return true;
}
```

**Services Updated**:
- ✅ `ProductService.DeleteAsync`
- ✅ `CustomerService.DeleteAsync`
- ✅ `ArticleService.DeleteAsync`
- ✅ `StockService.DeleteAsync`
- ✅ `OrderService.DeleteAsync`
- ✅ `AddressService.DeleteAsync`
- ✅ `SizeService.DeleteAsync`
- ✅ `ColorService.DeleteAsync`
- ✅ `LabelService.DeleteAsync`

### 2. ✅ Verify All PATCH Operations Check for Changes - COMPLETED
**Status**: All PATCH operations have been verified to check for changes before saving. No fixes needed.

### 3. Add Idempotency Key Support for POST Operations (Medium Priority)
**Implementation**:
1. Create `IdempotencyMiddleware` to handle `Idempotency-Key` header
2. Store idempotency keys in cache (with TTL)
3. Return cached response if key exists
4. Apply to all POST endpoints

**Header**: `Idempotency-Key: <unique-key>`

**Response**:
- First request: Process normally, cache response, return 201
- Duplicate request: Return cached response with 200 OK (or 201 if same resource)

### 4. Add Idempotency Key Support for Batch POST Operations
**Implementation**: Similar to single POST, but handle batch operations with a single idempotency key.

## Implementation Status

### ✅ Completed
1. **✅ High Priority**: Fix DELETE operations to be idempotent - **COMPLETED**
2. **✅ Medium Priority**: Verify all PATCH operations check for changes - **COMPLETED**

### 🔄 Pending (Future Enhancements)
3. **Medium Priority**: Add idempotency key support for POST operations
4. **Low Priority**: Add idempotency key support for batch POST operations

## Summary

### Current Idempotency Status
- ✅ **GET Operations**: Fully idempotent (read-only)
- ✅ **PUT Operations**: Idempotent by design (full update)
- ✅ **PATCH Operations**: Idempotent (checks for changes before saving)
- ✅ **DELETE Operations**: Idempotent (returns 204 even if already deleted)
- ⚠️ **POST Operations**: Not idempotent (requires idempotency key support - future enhancement)
- ✅ **Batch Operations**: Handle non-existent resources gracefully

## Testing Checklist

### ✅ Verified/Implemented
- [x] DELETE on non-existent resource returns 404
- [x] DELETE on existing resource returns 204
- [x] DELETE on already-deleted resource returns 204 (idempotent)
- [x] PATCH with no changes doesn't save to database
- [x] PATCH with same data multiple times produces same result
- [x] Batch DELETE handles duplicates correctly (skips non-existent resources)

### ⏸️ Future Enhancements
- [ ] POST with same idempotency key returns cached response
- [ ] POST without idempotency key creates new resource
- [ ] Batch POST with idempotency key support

## References

- [RFC 7231 - HTTP/1.1 Semantics](https://tools.ietf.org/html/rfc7231#section-4.2.2)
- [Microsoft REST API Guidelines - Idempotency](https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#92-idempotency)
- [Stripe API - Idempotency Keys](https://stripe.com/docs/api/idempotent_requests)
