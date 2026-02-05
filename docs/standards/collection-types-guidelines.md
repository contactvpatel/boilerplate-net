# Collection Types Guidelines for .NET 10 and Modern C#

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Quick Decision Matrix](#quick-decision-matrix)
- [Collection Types Explained](#collection-types-explained)
- [References](#references)

---

## Overview

This document provides guidance on when to use different collection types (`IEnumerable`, `IReadOnlyList`, `IList`, `List<T>`, etc.) in .NET 10 and modern C# applications, with a focus on performance, maintainability, and best practices.

## Quick Decision Matrix

| Use Case | Recommended Type | Why |
|----------|-----------------|-----|
| **API Return Types (Sequential)** | `IReadOnlyList<T>` or `IReadOnlyCollection<T>` | Immutable contract, prevents modification, better performance |
| **API Return Types (Key-Value)** | `IReadOnlyDictionary<TKey, TValue>` | Immutable key-value pairs, prevents modification |
| **Internal Method Returns (Materialized)** | `List<T>` or `IReadOnlyList<T>` | Already materialized, no need for abstraction |
| **Lazy/Deferred Execution** | `IEnumerable<T>` | LINQ queries, database queries, streaming |
| **Method Parameters (Read-only)** | `IReadOnlyList<T>` or `IReadOnlyCollection<T>` | Clear intent, prevents modification |
| **Method Parameters (Modifiable)** | `ICollection<T>` or `IList<T>` | Allows modification |
| **Dapper Queries** | `IAsyncEnumerable<T>` or `Task<List<T>>` | Async streaming or materialized results |
| **Caching** | `IReadOnlyList<T>` or `List<T>` | Materialized, no deferred execution issues |
| **Unique Items / Membership Checks** | `HashSet<T>` or `ISet<T>` | O(1) lookup, prevents duplicates |
| **Key-Value Lookups** | `Dictionary<TKey, TValue>` | O(1) key access, efficient mapping |
| **FIFO Processing** | `Queue<T>` | First-in-first-out semantics |
| **LIFO Processing** | `Stack<T>` | Last-in-first-out semantics |
| **Thread-Safe Dictionary** | `ConcurrentDictionary<TKey, TValue>` | Lock-free operations |
| **Immutable Collections** | `ImmutableList<T>`, `ImmutableDictionary<TKey, TValue>` | Thread-safe, functional style |

## Collection Types Explained

### 1. `IEnumerable<T>` - Deferred Execution

**When to Use:**
- ✅ LINQ queries that haven't been materialized
- ✅ Database queries (Dapper `IQueryable<T>`)
- ✅ Streaming data from external APIs
- ✅ Methods that return queryable results
- ✅ When you want lazy evaluation

**When NOT to Use:**
- ❌ API return types (causes multiple enumerations)
- ❌ Cached results (defeats caching purpose)
- ❌ When you need count/index access
- ❌ When result is already materialized

**Performance Considerations:**
- ⚠️ **Multiple enumerations are expensive** - Each enumeration executes the query again
- ⚠️ **Count() behavior** - `Count()` enumerates entire collection unless source implements `ICollection<T>` (then O(1))
- ⚠️ **No index access** - Cannot use `[index]` syntax

**Example:**
```csharp
// ✅ GOOD: Deferred execution for query building
public IQueryable<Product> GetProductsQueryable()
{
    return _context.Products.Where(p => p.IsActive);
}

// ❌ BAD: Returning IEnumerable from materialized list
public IEnumerable<ProductDto> GetProducts()
{
    List<Product> products = await connection.QueryAsync<Product>("SELECT * FROM webshop.products WHERE isactive = true")();
    return products.Select(p => MapToDto(p)); // Should return IReadOnlyList
}
```

### 2. `IReadOnlyList<T>` - Immutable Materialized Collection

**When to Use:**
- ✅ **API return types** (Best practice for public APIs)
- ✅ Cached results
- ✅ Method parameters that shouldn't be modified
- ✅ When you need index access (`[index]`)
- ✅ When you need count without enumeration
- ✅ When result is already materialized

**Performance Considerations:**
- ✅ **Single enumeration** - Materialized, no re-execution
- ✅ **O(1) index access** - Direct array/list access
- ✅ **O(1) count** - No enumeration needed
- ✅ **Immutable contract** - Prevents accidental modification

**Example:**
```csharp
// ✅ GOOD: API return type
[HttpGet]
public async Task<ActionResult<Response<IReadOnlyList<CustomerDto>>>> GetAll(CancellationToken cancellationToken)
{
    IReadOnlyList<CustomerDto> customers = await _customerService.GetAllAsync(cancellationToken);
    return Ok(Response<IReadOnlyList<CustomerDto>>.Success(customers));
}

// ✅ GOOD: Cached result (always materialize)
public async Task<IReadOnlyList<DepartmentDto>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken)
{
    return await _cacheService.GetOrCreateAsync(
        $"departments-division-{divisionId}",
        async cancel =>
        {
            var models = await _coreService.GetAllDepartmentsAsync(divisionId, cancel);
            return models.Adapt<IReadOnlyList<DepartmentDto>>().OrderBy(x => x.Name).ToList();
        },
        expiration: TimeSpan.FromHours(24),
        cancellationToken: cancellationToken);
}
```

### 3. `IReadOnlyCollection<T>` - Immutable Collection (No Index)

**When to Use:**
- ✅ When you need count but not index access
- ✅ API return types when index access isn't needed
- ✅ Slightly more flexible than `IReadOnlyList<T>` (accepts more collection types)

**Performance Considerations:**
- ✅ Same benefits as `IReadOnlyList<T>` except no index access
- ✅ More flexible - accepts `HashSet<T>`, `Queue<T>`, etc.

**Example:**
```csharp
// ✅ GOOD: When index access not needed
public async Task<IReadOnlyCollection<OrderDto>> GetOrdersByStatusAsync(OrderStatus status)
{
    // HashSet doesn't implement IReadOnlyList, but implements IReadOnlyCollection
    var orders = await _repository.FindAsync(o => o.Status == status);
    return orders.ToHashSet(); // Returns IReadOnlyCollection
}
```

### 4. `List<T>` - Mutable Materialized Collection

**When to Use:**
- ✅ Internal method implementations
- ✅ Building collections incrementally
- ✅ When you need to modify the collection
- ✅ Performance-critical code (avoids interface indirection)

**When NOT to Use:**
- ❌ Public API return types (use `IReadOnlyList<T>`)
- ❌ Method parameters (use `ICollection<T>` or `IList<T>`)

**Performance Considerations:**
- ✅ **Best performance** - No interface indirection
- ✅ **Direct memory access** - Array-backed
- ✅ **Mutable** - Can add/remove items

**Example:**
```csharp
// ✅ GOOD: Internal implementation using LINQ (C# 14 best practice)
private List<ProductDto> BuildProductList(IEnumerable<Product> products)
{
    return products.Select(MapToDto).ToList();
}

// ✅ ALTERNATIVE: Using collection expression with spread (C# 14)
private IReadOnlyList<ProductDto> BuildProductList(IEnumerable<Product> products)
{
    return [..products.Select(MapToDto)];
}

// ❌ BAD: Public API return
public List<ProductDto> GetProducts() // Should be IReadOnlyList
{
    return _products.ToList();
}
```

### 5. `IList<T>` / `ICollection<T>` - Mutable Interface

**When to Use:**
- ✅ Method parameters that need modification
- ✅ When you need to accept various mutable collections
- ✅ Internal APIs that modify collections

**When NOT to Use:**
- ❌ Return types (prefer concrete `List<T>` or `IReadOnlyList<T>`)
- ❌ Read-only scenarios (use `IReadOnlyList<T>`)

**Example:**
```csharp
// ✅ GOOD: Method parameter that modifies collection
public void AddProducts(ICollection<ProductDto> products)
{
    foreach (var product in products)
    {
        _repository.Add(MapToEntity(product));
    }
}
```

### 6. `IAsyncEnumerable<T>` - Async Streaming (.NET Core 3.0+, .NET 5+)

**When to Use:**
- ✅ Streaming large datasets from database
- ✅ Processing data in chunks
- ✅ When you want to start processing before all data is loaded
- ✅ Memory-efficient for large collections

**Performance Considerations:**
- ✅ **Memory efficient** - Processes items as they arrive
- ✅ **Faster time-to-first-result** - Starts processing immediately
- ✅ **Backpressure handling** - Consumer controls flow

**Example:**
```csharp
// ✅ GOOD: Streaming large dataset
public async IAsyncEnumerable<ProductDto> StreamProductsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var product in _context.Products
        
        .AsAsyncEnumerable()
        .WithCancellation(cancellationToken))
    {
        yield return MapToDto(product);
    }
}
```

### 7. `HashSet<T>` / `ISet<T>` - Unique Items Collection

**When to Use:**
- ✅ **Deduplication** - Removing duplicate items
- ✅ **Fast membership checks** - `Contains()` is O(1)
- ✅ **Set operations** - Union, Intersect, Except, SymmetricExcept
- ✅ **When order doesn't matter** - Unordered collection
- ✅ **Performance-critical lookups** - Faster than `List<T>.Contains()` which is O(n)

**When NOT to Use:**
- ❌ When you need ordering (use `SortedSet<T>` or `List<T>`)
- ❌ When you need index access (use `List<T>`)
- ❌ When duplicates are allowed (use `List<T>`)
- ❌ API return types (prefer `IReadOnlyCollection<T>` or convert to `IReadOnlyList<T>`)

**Performance Considerations:**
- ✅ **O(1) average lookup** - `Contains()`, `Add()`, `Remove()`
- ✅ **O(1) average insertion** - Very fast
- ✅ **No duplicates** - Automatically prevents duplicates
- ⚠️ **Unordered** - No guarantee of order
- ⚠️ **Memory overhead** - Slightly more memory than `List<T>`

**Example:**
```csharp
// ✅ GOOD: Deduplication using C# 14 collection expressions
public IReadOnlyList<int> GetUniqueCustomerIds(IEnumerable<Order> orders)
{
    HashSet<int> uniqueIds = orders.Select(o => o.CustomerId).ToHashSet();
    return uniqueIds.ToList(); // Convert to IReadOnlyList for API
}

// ✅ ALTERNATIVE: Using collection expression syntax (C# 14)
public IReadOnlyList<int> GetUniqueCustomerIds(IEnumerable<Order> orders)
{
    return [..orders.Select(o => o.CustomerId).Distinct()];
}

// ✅ GOOD: Fast membership check
public bool HasPermission(int userId, HashSet<int> allowedUserIds)
{
    return allowedUserIds.Contains(userId); // O(1) lookup
}

// ✅ GOOD: Set operations
public HashSet<int> GetCommonCustomerIds(HashSet<int> set1, HashSet<int> set2)
{
    return set1.Intersect(set2).ToHashSet(); // Or use set1.IntersectWith(set2)
}

// ✅ GOOD: Convert HashSet to IReadOnlyList for API return
public IReadOnlyList<int> GetUniqueCustomerIds(IEnumerable<Order> orders)
{
    HashSet<int> uniqueIds = orders.Select(o => o.CustomerId).ToHashSet();
    return uniqueIds.ToList(); // Convert to IReadOnlyList
}

// ❌ BAD: Using List for membership checks
public bool HasPermission(int userId, List<int> allowedUserIds)
{
    return allowedUserIds.Contains(userId); // O(n) lookup - slow!
}
```

### 8. `Dictionary<TKey, TValue>` / `IDictionary<TKey, TValue>` - Key-Value Collection

**When to Use:**
- ✅ **Fast key-based lookups** - O(1) average access by key
- ✅ **Mapping relationships** - One-to-one mappings
- ✅ **Caching by key** - Efficient key-based caching
- ✅ **Grouping results** - Grouping data by a key
- ✅ **When you need key-value pairs** - Natural fit for key-value scenarios

**When NOT to Use:**
- ❌ When you need sorted order (use `SortedDictionary<TKey, TValue>`)
- ❌ When you only need sequential access (use `List<T>`)
- ❌ API return types (prefer `IReadOnlyDictionary<TKey, TValue>`)
- ❌ When you need null keys with non-nullable reference types (use nullable type like `string?` for key)

**Performance Considerations:**
- ✅ **O(1) average key lookup** - `TryGetValue()`, `ContainsKey()`, indexer
- ✅ **O(1) average insertion** - Very fast
- ✅ **Efficient memory** - Hash table implementation
- ✅ **Insertion order preserved** - In .NET Core 2.0+ and .NET Framework 4.7.1+, insertion order is maintained
- ⚠️ **Key uniqueness** - Duplicate keys not allowed
- ⚠️ **Null keys** - Null keys allowed only if `TKey` is a nullable reference type (e.g., `string?`)

**Example:**
```csharp
// ✅ GOOD: Fast lookups by key
public async Task<CustomerDto?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken)
{
    Dictionary<int, CustomerDto> customerCache = await GetCustomerCacheAsync(cancellationToken);
    
    // O(1) lookup - much faster than List.FirstOrDefault()
    if (customerCache.TryGetValue(customerId, out CustomerDto? customer))
    {
        return customer;
    }
    
    return null;
}

// ✅ GOOD: Grouping data
public Dictionary<string, IReadOnlyList<OrderDto>> GroupOrdersByStatus(IEnumerable<Order> orders)
{
    return orders
        .GroupBy(o => o.Status.ToString())
        .ToDictionary(g => g.Key, g => g.Select(MapToDto).ToList() as IReadOnlyList<OrderDto>);
}

// ✅ GOOD: Building lookup dictionary (C# 14 best practice)
public Dictionary<int, ProductDto> BuildProductLookup(IEnumerable<Product> products)
{
    return products
        .Select(MapToDto)
        .ToDictionary(p => p.Id, p => p);
}

// ✅ ALTERNATIVE: Using collection expression with spread (C# 14)
public IReadOnlyDictionary<int, ProductDto> BuildProductLookup(IEnumerable<Product> products)
{
    return products
        .Select(MapToDto)
        .ToDictionary(p => p.Id, p => p);
}

// ✅ GOOD: Internal implementation using LINQ (C# 14 best practice)
private Dictionary<string, int> CountItemsByCategory(IEnumerable<Product> products)
{
    return products
        .GroupBy(p => p.Category)
        .ToDictionary(g => g.Key, g => g.Count());
}

// ✅ ALTERNATIVE: Using collection expression for small dictionaries (C# 14)
private IReadOnlyDictionary<string, int> GetDefaultCounts()
{
    return new Dictionary<string, int>
    {
        ["Category1"] = 0,
        ["Category2"] = 0,
        ["Category3"] = 0
    };
}

// ❌ BAD: Using List for key lookups
public CustomerDto? GetCustomerById(int customerId, List<CustomerDto> customers)
{
    return customers.FirstOrDefault(c => c.Id == customerId); // O(n) - slow!
}
```

### 9. `IReadOnlyDictionary<TKey, TValue>` - Immutable Key-Value Collection

**When to Use:**
- ✅ **API return types** - Immutable key-value pairs
- ✅ **Cached mappings** - Read-only cached dictionaries
- ✅ **Method parameters** - When you need to pass dictionaries but don't want modification
- ✅ **Configuration data** - Immutable configuration mappings

**Performance Considerations:**
- ✅ Same performance as `Dictionary<TKey, TValue>` for read operations
- ✅ **Immutable contract** - Prevents accidental modification
- ✅ **O(1) key lookup** - Same as Dictionary

**Example:**
```csharp
// ✅ GOOD: API return type
[HttpGet]
public async Task<ActionResult<Response<IReadOnlyDictionary<string, int>>>> GetOrderCountsByStatus()
{
    IReadOnlyDictionary<string, int> counts = await _orderService.GetOrderCountsByStatusAsync();
    return Ok(Response<IReadOnlyDictionary<string, int>>.Success(counts));
}

// ✅ GOOD: Cached mapping
public async Task<IReadOnlyDictionary<int, DepartmentDto>> GetDepartmentLookupAsync(CancellationToken cancellationToken)
{
    return await _cacheService.GetOrCreateAsync(
        "department-lookup",
        async cancel =>
        {
            var departments = await _coreService.GetAllDepartmentsAsync(cancel);
            return departments.ToDictionary(d => d.Id, d => MapToDto(d));
        },
        expiration: TimeSpan.FromHours(24),
        cancellationToken: cancellationToken);
}

// ✅ GOOD: Method parameter
public void ProcessOrders(IReadOnlyDictionary<int, OrderDto> orders)
{
    // Can read but not modify
    foreach (var (orderId, order) in orders)
    {
        ProcessOrder(order);
    }
}
```

### 10. `Queue<T>` - First-In-First-Out Collection

**When to Use:**
- ✅ **FIFO processing** - Process items in order they were added
- ✅ **Task queues** - Background job processing
- ✅ **Breadth-first traversal** - Graph/tree algorithms
- ✅ **Message processing** - Process messages in order
- ✅ **Request queuing** - Queue requests for processing

**When NOT to Use:**
- ❌ When you need LIFO (use `Stack<T>`)
- ❌ When you need random access (use `List<T>`)
- ❌ When you need key-based access (use `Dictionary<TKey, TValue>`)
- ❌ API return types (convert to `IReadOnlyList<T>` if needed)

**Note**: `Queue<T>` implements `IEnumerable<T>` but there is no `IQueue<T>` interface in .NET. Use `Queue<T>` directly or `IEnumerable<T>` for method parameters.

**Performance Considerations:**
- ✅ **O(1) enqueue/dequeue** - Very fast operations
- ✅ **Efficient memory** - Circular buffer implementation
- ⚠️ **No random access** - Cannot access items by index
- ⚠️ **No peeking at middle** - Only access front/back

**Example:**
```csharp
// ✅ GOOD: Task queue processing
public async Task ProcessOrdersAsync(IEnumerable<Order> orders, CancellationToken cancellationToken)
{
    Queue<Order> orderQueue = new(orders);
    
    while (orderQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
    {
        Order order = orderQueue.Dequeue(); // FIFO - oldest first
        await ProcessOrderAsync(order, cancellationToken);
    }
}

// ✅ GOOD: Breadth-first search (C# 14 best practice)
public List<Node> BreadthFirstSearch(Node root)
{
    List<Node> result = [];
    Queue<Node> queue = new([root]); // Collection expression initialization
    
    while (queue.Count > 0)
    {
        Node current = queue.Dequeue();
        result.Add(current);
        
        foreach (Node child in current.Children)
        {
            queue.Enqueue(child);
        }
    }
    
    return result;
}

// ✅ GOOD: Background job queue
public class BackgroundJobProcessor
{
    private readonly Queue<Job> _jobQueue = new();
    
    public void EnqueueJob(Job job)
    {
        _jobQueue.Enqueue(job);
    }
    
    public async Task ProcessNextJobAsync()
    {
        if (_jobQueue.TryDequeue(out Job? job))
        {
            await ExecuteJobAsync(job);
        }
    }
}
```

### 11. `Stack<T>` - Last-In-First-Out Collection

**When to Use:**
- ✅ **LIFO processing** - Process most recent items first
- ✅ **Undo/Redo operations** - Command pattern implementations
- ✅ **Expression evaluation** - Postfix notation, calculator
- ✅ **Depth-first traversal** - Graph/tree algorithms
- ✅ **Call stack simulation** - Recursive algorithm conversion
- ✅ **Backtracking algorithms** - Path finding, maze solving

**When NOT to Use:**
- ❌ When you need FIFO (use `Queue<T>`)
- ❌ When you need random access (use `List<T>`)
- ❌ API return types (convert to `IReadOnlyList<T>` if needed)

**Note**: `Stack<T>` implements `IEnumerable<T>` but there is no `IStack<T>` interface in .NET. Use `Stack<T>` directly or `IEnumerable<T>` for method parameters.

**Performance Considerations:**
- ✅ **O(1) push/pop** - Very fast operations
- ✅ **Efficient memory** - Array-backed implementation
- ⚠️ **No random access** - Cannot access items by index
- ⚠️ **Only top access** - Can only access the top item

**Example:**
```csharp
// ✅ GOOD: Undo/Redo functionality (C# 14 best practice with primary constructor)
public class UndoRedoManager<T>
{
    private readonly Stack<T> _undoStack = [];
    private readonly Stack<T> _redoStack = [];
    
    public void ExecuteAction(T state)
    {
        _undoStack.Push(state);
        _redoStack.Clear(); // Clear redo when new action is performed
    }
    
    public T? Undo()
    {
        if (_undoStack.Count == 0) return default;
        
        T current = _undoStack.Pop();
        _redoStack.Push(current);
        return _undoStack.Count > 0 ? _undoStack.Peek() : default;
    }
    
    public T? Redo()
    {
        if (_redoStack.Count == 0) return default;
        
        T state = _redoStack.Pop();
        _undoStack.Push(state);
        return state;
    }
}

// ✅ GOOD: Expression evaluation (postfix notation) - C# 14 best practice
public int EvaluatePostfixExpression(string[] tokens)
{
    Stack<int> stack = []; // Collection expression initialization
    
    foreach (string token in tokens)
    {
        if (int.TryParse(token, out int number))
        {
            stack.Push(number);
        }
        else
        {
            int b = stack.Pop();
            int a = stack.Pop();
            int result = token switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => a / b,
                _ => throw new ArgumentException($"Unknown operator: {token}")
            };
            stack.Push(result);
        }
    }
    
    return stack.Pop();
}

// ✅ GOOD: Depth-first search (C# 14 best practice)
public List<Node> DepthFirstSearch(Node root)
{
    List<Node> result = []; // Collection expression
    Stack<Node> stack = new([root]); // Collection expression initialization
    
    while (stack.Count > 0)
    {
        Node current = stack.Pop();
        result.Add(current);
        
        // Push children in reverse order to maintain left-to-right traversal
        for (int i = current.Children.Count - 1; i >= 0; i--)
        {
            stack.Push(current.Children[i]);
        }
    }
    
    return result;
}
```

### 12. `ConcurrentDictionary<TKey, TValue>` - Thread-Safe Dictionary

**When to Use:**
- ✅ **Multi-threaded scenarios** - When multiple threads access the dictionary
- ✅ **High-concurrency caching** - Thread-safe caching without locks
- ✅ **Shared state** - When multiple threads need to read/write
- ✅ **Performance-critical concurrent operations** - Lock-free operations

**When NOT to Use:**
- ❌ Single-threaded scenarios (use `Dictionary<TKey, TValue>` - less overhead)
- ❌ When you need ordering (use `ConcurrentDictionary` with custom ordering logic)
- ❌ API return types (prefer `IReadOnlyDictionary<TKey, TValue>`)

**Performance Considerations:**
- ✅ **Lock-free reads** - Very fast concurrent reads
- ✅ **Fine-grained locking** - Better than locking entire dictionary
- ✅ **Thread-safe** - No need for external synchronization
- ⚠️ **Slightly slower than Dictionary** - Overhead for thread safety
- ⚠️ **Memory overhead** - More memory than regular Dictionary

**Example:**
```csharp
// ✅ GOOD: Thread-safe caching
public class ThreadSafeCache<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, TValue> _cache = new();
    
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        return _cache.GetOrAdd(key, valueFactory);
    }
    
    public bool TryGetValue(TKey key, out TValue? value)
    {
        return _cache.TryGetValue(key, out value);
    }
    
    public void AddOrUpdate(TKey key, TValue value, Func<TKey, TValue, TValue> updateFactory)
    {
        _cache.AddOrUpdate(key, value, updateFactory);
    }
}

// ✅ GOOD: Concurrent aggregation (C# 14 best practice)
public Dictionary<string, int> CountItemsConcurrently(IEnumerable<Item> items)
{
    ConcurrentDictionary<string, int> counts = [];
    
    Parallel.ForEach(items, item =>
    {
        counts.AddOrUpdate(
            item.Category,
            1,
            (key, oldValue) => oldValue + 1);
    });
    
    return new Dictionary<string, int>(counts);
}

// ✅ GOOD: Thread-safe lookup with lazy initialization
public class ProductService
{
    private readonly ConcurrentDictionary<int, Task<ProductDto>> _productCache = new();
    
    public async Task<ProductDto> GetProductAsync(int productId, CancellationToken cancellationToken)
    {
        Task<ProductDto> task = _productCache.GetOrAdd(productId, id =>
        {
            return Task.Run(async () =>
            {
                var product = await _repository.GetByIdAsync(id, cancellationToken);
                return MapToDto(product);
            });
        });
        return await task;
    }
}

// ✅ ALTERNATIVE: Simpler approach with Lazy<T>
public class ProductService
{
    private readonly ConcurrentDictionary<int, Lazy<Task<ProductDto>>> _productCache = new();
    
    public async Task<ProductDto> GetProductAsync(int productId, CancellationToken cancellationToken)
    {
        Lazy<Task<ProductDto>> lazy = _productCache.GetOrAdd(productId, id =>
            new Lazy<Task<ProductDto>>(async () =>
            {
                var product = await _repository.GetByIdAsync(id, cancellationToken);
                return MapToDto(product);
            }));
        return await lazy.Value;
    }
}
```

### 13. Immutable Collections - `ImmutableList<T>`, `ImmutableDictionary<TKey, TValue>`, etc.

**When to Use:**
- ✅ **Thread-safe shared state** - Multiple threads can safely read
- ✅ **Functional programming style** - Immutable data structures
- ✅ **Configuration data** - Immutable configuration
- ✅ **When you need snapshot semantics** - Each operation returns a new collection
- ✅ **Value semantics** - Collections that behave like values

**When NOT to Use:**
- ❌ Performance-critical mutable operations (use mutable collections)
- ❌ When you need frequent modifications (overhead of creating new collections)
- ❌ Single-threaded scenarios with frequent updates (use mutable collections)

**Performance Considerations:**
- ✅ **Thread-safe reads** - No locking needed for reads
- ✅ **Structural sharing** - Efficient memory usage through sharing
- ⚠️ **Modification overhead** - Each modification creates a new collection
- ⚠️ **Slower than mutable collections** - For frequent modifications

**Example:**
```csharp
// ✅ GOOD: Immutable configuration
public class AppConfiguration
{
    public ImmutableDictionary<string, string> Settings { get; }
    
    public AppConfiguration(Dictionary<string, string> settings)
    {
        Settings = settings.ToImmutableDictionary();
    }
    
    public AppConfiguration WithSetting(string key, string value)
    {
        return new AppConfiguration(Settings.SetItem(key, value).ToDictionary());
    }
}

// ✅ GOOD: Thread-safe shared state
public class ProductCatalog
{
    private ImmutableList<Product> _products = ImmutableList<Product>.Empty;
    
    public ImmutableList<Product> Products => _products; // Thread-safe read
    
    public void AddProduct(Product product)
    {
        ImmutableInterlocked.Update(ref _products, list => list.Add(product));
    }
    
    public void RemoveProduct(int productId)
    {
        ImmutableInterlocked.Update(ref _products, list => 
            list.RemoveAll(p => p.Id == productId));
    }
}

// ✅ GOOD: Functional style transformations
public ImmutableList<OrderDto> GetActiveOrders(IEnumerable<Order> orders)
{
    return orders
        .Where(o => o.IsActive)
        .Select(MapToDto)
        .ToImmutableList();
}
```

### 14. `PriorityQueue<TElement, TPriority>` - Priority Queue (.NET 6+)

**When to Use:**
- ✅ **Priority-based processing** - Process items by priority
- ✅ **Task scheduling** - Schedule tasks by priority
- ✅ **Dijkstra's algorithm** - Shortest path algorithms
- ✅ **Event processing** - Process events by priority
- ✅ **Resource allocation** - Allocate resources by priority

**Performance Considerations:**
- ✅ **O(log n) enqueue/dequeue** - Efficient for priority operations
- ✅ **O(1) peek** - Fast access to highest priority item
- ✅ **Heap-based** - Binary heap implementation

**Example:**
```csharp
// ✅ GOOD: Priority-based task processing (C# 14 best practice)
public class TaskScheduler
{
    private readonly PriorityQueue<Task, int> _taskQueue = [];
    
    public void ScheduleTask(Task task, int priority)
    {
        _taskQueue.Enqueue(task, priority); // Lower number = higher priority
    }
    
    public async Task ProcessNextTaskAsync()
    {
        if (_taskQueue.TryDequeue(out Task? task, out int priority))
        {
            await ExecuteTaskAsync(task);
        }
    }
    
    public Task? PeekNextTask()
    {
        return _taskQueue.TryPeek(out Task? task, out _) ? task : null;
    }
}

// ✅ GOOD: Event processing by priority (C# 14 best practice)
public async Task ProcessEventsAsync(IEnumerable<Event> events)
{
    PriorityQueue<Event, int> eventQueue = [];
    
    foreach (var evt in events)
    {
        int priority = evt.Type switch
        {
            EventType.Critical => 1,
            EventType.High => 2,
            EventType.Medium => 3,
            EventType.Low => 4,
            _ => 5
        };
        eventQueue.Enqueue(evt, priority);
    }
    
    while (eventQueue.Count > 0)
    {
        Event evt = eventQueue.Dequeue();
        await ProcessEventAsync(evt);
    }
}
```

## Best Practices by Layer

### API Controllers

**Guideline**: Always return `IReadOnlyList<T>` or `IReadOnlyDictionary<TKey, TValue>` for collections. Never return `IEnumerable<T>` as it can cause multiple enumerations during JSON serialization.

```csharp
// ✅ BEST: Use IReadOnlyList for return types (C# 14 best practice)
[HttpGet]
[ProducesResponseType(typeof(Response<IReadOnlyList<CustomerDto>>), StatusCodes.Status200OK)]
public async Task<ActionResult<Response<IReadOnlyList<CustomerDto>>>> GetAll(
    CancellationToken cancellationToken)
{
    IReadOnlyList<CustomerDto> customers = await _customerService.GetAllAsync(cancellationToken);
    return Ok(Response<IReadOnlyList<CustomerDto>>.Success(customers));
}

// ✅ GOOD: Key-value pairs using IReadOnlyDictionary
[HttpGet]
[ProducesResponseType(typeof(Response<IReadOnlyDictionary<string, int>>), StatusCodes.Status200OK)]
public async Task<ActionResult<Response<IReadOnlyDictionary<string, int>>>> GetOrderCountsByStatus(
    CancellationToken cancellationToken)
{
    IReadOnlyDictionary<string, int> counts = await _orderService.GetOrderCountsByStatusAsync(cancellationToken);
    return Ok(Response<IReadOnlyDictionary<string, int>>.Success(counts));
}

// ❌ AVOID: IEnumerable in API returns - causes multiple enumerations
public async Task<ActionResult<Response<IEnumerable<CustomerDto>>>> GetAll(
    CancellationToken cancellationToken)
{
    // This can cause multiple enumerations during JSON serialization
    IEnumerable<CustomerDto> customers = await _customerService.GetAllAsync(cancellationToken);
    return Ok(Response<IEnumerable<CustomerDto>>.Success(customers));
}
```

### Service Layer

**Guideline**: Return `IReadOnlyList<T>` from service interfaces. Internal implementations can use `List<T>` for building, then materialize and return as `IReadOnlyList<T>`. Use collection expressions (C# 14) where appropriate.

```csharp
// ✅ BEST: Return IReadOnlyList from service methods
public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, CustomerDto>> GetCustomerLookupAsync(CancellationToken cancellationToken = default);
}

// ✅ GOOD: Internal implementation using List<T> with C# 14 collection expressions
public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
{
    List<Customer> customers = await _repository.GetAllAsync(cancellationToken);
    
    // Materialize to List, return as IReadOnlyList
    return customers
        .Select(MapToDto)
        .ToList();
}

// ✅ GOOD: Building dictionary lookup
public async Task<IReadOnlyDictionary<int, CustomerDto>> GetCustomerLookupAsync(
    CancellationToken cancellationToken = default)
{
    List<Customer> customers = await _repository.GetAllAsync(cancellationToken);
    
    // Materialize as Dictionary for O(1) lookups
    return customers
        .Select(MapToDto)
        .ToDictionary(c => c.Id, c => c);
}

// ✅ GOOD: Using collection expressions for small collections (C# 14)
public IReadOnlyList<string> GetDefaultStatuses()
{
    return ["Pending", "Processing", "Completed", "Cancelled"]; // Collection expression
}
```

### Repository Layer

**Guideline**: Return `List<T>` for materialized queries or `IAsyncEnumerable<T>` for streaming large datasets. Dapper always uses read-only queries by default. Use modern async patterns.

```csharp
// ✅ GOOD: Dapper queries - return List<T> for materialized results
public async Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
{
    return await _context.Customers
         // Important for read-only queries
        .Where(c => c.IsActive)
        .OrderBy(c => c.Name)
        );
}

// ✅ BEST: For large datasets, use IAsyncEnumerable for streaming
public async IAsyncEnumerable<Customer> StreamAllAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (Customer customer in _context.Customers
        
        .Where(c => c.IsActive)
        .OrderBy(c => c.Name)
        .AsAsyncEnumerable()
        .WithCancellation(cancellationToken))
    {
        yield return customer;
    }
}

// ✅ GOOD: Returning Dictionary from repository when needed
public async Task<Dictionary<int, Customer>> GetCustomerLookupAsync(
    CancellationToken cancellationToken = default)
{
    return await _context.Customers
        
        .ToDictionaryAsync(c => c.Id, c => c, cancellationToken);
}
```

### Caching Layer

**Guideline**: Always materialize collections before caching. Cache `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`, or `HashSet<T>` - never cache `IEnumerable<T>`. Use collection expressions for small cached collections.

```csharp
// ✅ BEST: Always materialize cached results (List)
public async Task<IReadOnlyList<DepartmentDto>> GetAllDepartmentsAsync(
    int divisionId, 
    CancellationToken cancellationToken = default)
{
    return await _cacheService.GetOrCreateAsync(
        $"departments-division-{divisionId}",
        async cancel =>
        {
            IEnumerable<DepartmentModel> models = await _coreService.GetAllDepartmentsAsync(divisionId, cancel);
            
            // Materialize immediately - don't cache IEnumerable
            return models
                .Adapt<IEnumerable<DepartmentDto>>()
                .OrderBy(x => x.Name)
                .ToList(); // Materialize!
        },
        expiration: TimeSpan.FromHours(24),
        cancellationToken: cancellationToken);
}

// ✅ BEST: Always materialize cached results (Dictionary)
public async Task<IReadOnlyDictionary<int, DepartmentDto>> GetDepartmentLookupAsync(
    CancellationToken cancellationToken = default)
{
    return await _cacheService.GetOrCreateAsync(
        "department-lookup",
        async cancel =>
        {
            IEnumerable<DepartmentModel> models = await _coreService.GetAllDepartmentsAsync(1, cancel);
            
            // Materialize as Dictionary for O(1) lookups
            return models
                .Adapt<IEnumerable<DepartmentDto>>()
                .ToDictionary(d => d.Id, d => d); // Materialize!
        },
        expiration: TimeSpan.FromHours(24),
        cancellationToken: cancellationToken);
}

// ✅ BEST: Caching HashSet for membership checks
public async Task<HashSet<int>> GetAllowedUserIdsAsync(CancellationToken cancellationToken = default)
{
    return await _cacheService.GetOrCreateAsync(
        "allowed-user-ids",
        async cancel =>
        {
            IEnumerable<int> userIds = await _repository.GetAllowedUserIdsAsync(cancel);
            
            // Materialize as HashSet for O(1) Contains()
            return userIds.ToHashSet(); // Materialize!
        },
        expiration: TimeSpan.FromMinutes(30),
        cancellationToken: cancellationToken);
}

// ✅ GOOD: Using collection expressions for small cached collections (C# 14)
public IReadOnlyList<string> GetCachedStatuses()
{
    // Small collections can use collection expressions directly
    return ["Active", "Inactive", "Pending"];
}

// ❌ BAD: Caching IEnumerable defeats the purpose
public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(
    int divisionId, 
    CancellationToken cancellationToken = default)
{
    return await _cacheService.GetOrCreateAsync(
        $"departments-division-{divisionId}",
        async cancel =>
        {
            IEnumerable<DepartmentModel> models = await _coreService.GetAllDepartmentsAsync(divisionId, cancel);
            return models.Adapt<IEnumerable<DepartmentDto>>(); // Not materialized!
        },
        expiration: TimeSpan.FromHours(24),
        cancellationToken: cancellationToken);
}
```

## Performance Comparison

### Multiple Enumerations

```csharp
// ❌ BAD: IEnumerable - Multiple enumerations execute query multiple times
IEnumerable<ProductDto> products = GetProducts();
int count = products.Count(); // Enumerates once
var first = products.First(); // Enumerates again!
var list = products.ToList(); // Enumerates third time!

// ✅ GOOD: IReadOnlyList - Materialized, no re-execution
IReadOnlyList<ProductDto> products = await GetProductsAsync();
int count = products.Count; // O(1) - no enumeration
var first = products[0]; // O(1) - direct access
var list = products.ToList(); // Just copies reference
```

### Memory Allocation

```csharp
// IEnumerable with deferred execution
// - Minimal memory until enumeration
// - But multiple enumerations = multiple allocations

// IReadOnlyList / List<T>
// - Single allocation when materialized
// - Better for repeated access
// - Better for caching
```

## .NET 10 and C# 14 Features

### Collection Expressions (C# 12+, Enhanced in C# 14)

**Best Practice**: Use collection expressions for initializing collections. They work with `IReadOnlyList<T>`, `List<T>`, arrays, spans, and dictionaries.

```csharp
// ✅ Modern syntax - works with IReadOnlyList, List, arrays, spans
IReadOnlyList<string> items = ["item1", "item2", "item3"];
List<int> numbers = [1, 2, 3, 4, 5];
int[] array = [10, 20, 30];

// ✅ Spread operator for combining collections
IReadOnlyList<int> existingList = [1, 2, 3];
IReadOnlyList<int> combined = [..existingList, 6, 7, 8]; // [1, 2, 3, 6, 7, 8]

// ✅ Dictionary initialization with collection expressions (C# 14)
Dictionary<string, int> dict = new()
{
    ["apple"] = 1,
    ["banana"] = 2,
    ["cherry"] = 3
};

// ✅ Using collection expressions in method returns
public IReadOnlyList<string> GetDefaultStatuses() => ["Pending", "Processing", "Completed"];

// ✅ Collection expressions with LINQ
var filtered = [..numbers.Where(n => n > 2)]; // [3, 4, 5]
```

### Primary Constructors and Collection Initialization (C# 14)

**Best Practice**: Use primary constructors with collection initialization for cleaner code.

```csharp
// ✅ GOOD: Primary constructor with collection initialization
public class OrderService(ICacheService cacheService)
{
    private readonly IReadOnlyList<string> _validStatuses = 
        ["Pending", "Processing", "Completed", "Cancelled"];
    
    public bool IsValidStatus(string status) => _validStatuses.Contains(status);
}

// ✅ GOOD: Using collection expressions in readonly fields
public class Configuration
{
    private readonly HashSet<string> _allowedDomains = 
        ["example.com", "test.com", "dev.com"].ToHashSet();
    
    public bool IsAllowedDomain(string domain) => _allowedDomains.Contains(domain);
}
```

### Materialization Best Practices

**Best Practice**: Always materialize collections before returning from methods or caching.

```csharp
// ✅ Always materialize before returning from methods
var materialized = query.ToList(); // Returns List<T>
IReadOnlyList<T> readonlyList = query.ToList(); // Implicitly converts to IReadOnlyList<T>

// ✅ For async, use ToListAsync()
var materialized = await query);

// ✅ Materialize dictionaries
var dict = items.ToDictionary(item => item.Id, item => item);

// ✅ Materialize hash sets
var set = items.Select(i => i.Category).ToHashSet();
```

### Null-Conditional Operators with Collections (C# 14)

**Best Practice**: Use null-conditional operators for safe collection access.

```csharp
// ✅ Safe collection access
int? count = items?.Count; // Returns null if items is null
var first = items?.FirstOrDefault(); // Safe access

// ✅ Safe dictionary access
string? value = dictionary?.GetValueOrDefault("key");

// ✅ Safe collection operations
var result = orders?.Where(o => o.IsActive).ToList() ?? [];
```

### Extension Methods and Collection Enhancements (C# 14)

**Best Practice**: Use extension methods to enhance collection functionality while maintaining clean code.

```csharp
// ✅ Extension method for common collection operations
public static class CollectionExtensions
{
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
        => source.ToList();
    
    public static bool IsNullOrEmpty<T>(this ICollection<T>? collection)
        => collection == null || collection.Count == 0;
    
    public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> source)
        where TKey : notnull
        => source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
}
```

## Migration Guide

### Step 1: Update API Return Types

```csharp
// Before
public async Task<ActionResult<Response<IEnumerable<CustomerDto>>>> GetAll(...)

// After
public async Task<ActionResult<Response<IReadOnlyList<CustomerDto>>>> GetAll(...)
```

### Step 2: Update Service Interfaces

```csharp
// Before
Task<IEnumerable<CustomerDto>> GetAllAsync(...);

// After
Task<IReadOnlyList<CustomerDto>> GetAllAsync(...);
```

### Step 3: Update Service Implementations

```csharp
// Before
public async Task<IEnumerable<CustomerDto>> GetAllAsync(...)
{
    var customers = await _repository.GetAllAsync(...);
    return customers.Select(MapToDto); // Deferred execution
}

// After
public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(...)
{
    var customers = await _repository.GetAllAsync(...);
    return customers.Select(MapToDto).ToList(); // Materialized
}
```

### Step 4: Update Repository Methods

```csharp
// Before
public async Task<IEnumerable<T>> GetAllAsync(...)
{
    return await _context.Set<T>().ToListAsync(...);
}

// After - Option 1: Return List<T> (more flexible)
public async Task<List<T>> GetAllAsync(...)
{
    return await _context.Set<T>().ToListAsync(...);
}

// After - Option 2: Return IReadOnlyList<T> (immutable contract)
public async Task<IReadOnlyList<T>> GetAllAsync(...)
{
    return await _context.Set<T>().ToListAsync(...);
}
```

## Common Pitfalls

### Pitfall 1: Caching IEnumerable

```csharp
// ❌ BAD: Caching IEnumerable
var cached = await _cache.GetOrCreateAsync("key", async () => 
{
    return await _db.Products.Select(p => Map(p)); // Not materialized!
});

// ✅ GOOD: Materialize before caching
var cached = await _cache.GetOrCreateAsync("key", async () => 
{
    return (await _db.Products.ToListAsync()).Select(p => Map(p)).ToList();
});
```

### Pitfall 2: Multiple Enumerations in API

```csharp
// ❌ BAD: Multiple enumerations during serialization
IEnumerable<ProductDto> products = GetProducts();
// JSON serializer may enumerate multiple times

// ✅ GOOD: Materialized collection
IReadOnlyList<ProductDto> products = await GetProductsAsync();
// Single enumeration, serialized once
```

### Pitfall 3: Using IEnumerable for Count

```csharp
// ❌ BAD: Count() may enumerate entire collection (unless source implements ICollection<T>)
IEnumerable<Product> products = GetProducts();
if (products.Count() > 0) // May enumerate all items if not ICollection<T>!

// ✅ GOOD: Materialized collection
IReadOnlyList<Product> products = await GetProductsAsync();
if (products.Count > 0) // O(1) operation
```

## Summary

### Golden Rules

1. **API Return Types (Sequential)**: Always use `IReadOnlyList<T>` or `IReadOnlyCollection<T>`
2. **API Return Types (Key-Value)**: Always use `IReadOnlyDictionary<TKey, TValue>`
3. **Cached Results**: Always materialize (use `ToList()`, `ToArray()`, etc.)
4. **Dapper Queries**: Return `List<T>` or `IAsyncEnumerable<T>`
5. **Method Parameters (Read-only)**: Use `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, or `IReadOnlyDictionary<TKey, TValue>`
6. **Method Parameters (Modifiable)**: Use `ICollection<T>`, `IList<T>`, or `IDictionary<TKey, TValue>`
7. **Internal Implementations**: Use `List<T>`, `Dictionary<TKey, TValue>`, or `HashSet<T>` for best performance
8. **Deferred Execution**: Use `IEnumerable<T>` only when you need lazy evaluation
9. **Unique Items**: Use `HashSet<T>` for O(1) membership checks
10. **Key-Value Lookups**: Use `Dictionary<TKey, TValue>` for O(1) key access
11. **Thread-Safe**: Use `ConcurrentDictionary<TKey, TValue>` for concurrent scenarios
12. **FIFO Processing**: Use `Queue<T>` for first-in-first-out
13. **LIFO Processing**: Use `Stack<T>` for last-in-first-out

### Performance Priority

**Sequential Collections:**
1. **Best**: `List<T>` (concrete type, no indirection)
2. **Good**: `IReadOnlyList<T>` (immutable contract, materialized)
3. **Acceptable**: `IEnumerable<T>` (when deferred execution is needed)
4. **Avoid**: `IEnumerable<T>` for materialized results

**Key-Value Collections:**
1. **Best**: `Dictionary<TKey, TValue>` (O(1) lookups, mutable)
2. **Good**: `IReadOnlyDictionary<TKey, TValue>` (O(1) lookups, immutable)
3. **Thread-Safe**: `ConcurrentDictionary<TKey, TValue>` (concurrent access)

**Set Collections:**
1. **Best**: `HashSet<T>` (O(1) membership checks, unique items)
2. **Thread-Safe**: `ConcurrentBag<T>` (for concurrent scenarios, but allows duplicates - not a true set)
3. **Thread-Safe Set**: Use `ConcurrentDictionary<T, byte>` with dummy values for thread-safe set operations

### Readability Priority

1. **Best**: `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>` (clear intent, immutable)
2. **Good**: `List<T>`, `Dictionary<TKey, TValue>`, `HashSet<T>` (clear, but mutable)
3. **Acceptable**: `IEnumerable<T>` (unclear if materialized)


## Quick Reference: Collection Type Selection

| Need | Use This | Example Scenario |
|------|----------|------------------|
| **API return (list)** | `IReadOnlyList<T>` | Returning customers, products, orders |
| **API return (key-value)** | `IReadOnlyDictionary<TKey, TValue>` | Returning counts by status, grouped data |
| **Fast membership check** | `HashSet<T>` | Checking if user has permission, deduplication |
| **Fast key lookup** | `Dictionary<TKey, TValue>` | Finding customer by ID, product by SKU |
| **FIFO processing** | `Queue<T>` | Processing orders in order, task queues |
| **LIFO processing** | `Stack<T>` | Undo/redo, expression evaluation |
| **Priority processing** | `PriorityQueue<TElement, TPriority>` | Task scheduling, event processing |
| **Thread-safe dictionary** | `ConcurrentDictionary<TKey, TValue>` | Multi-threaded caching, shared state |
| **Thread-safe immutable** | `ImmutableList<T>`, `ImmutableDictionary<TKey, TValue>` | Shared configuration, functional style |
| **Streaming large data** | `IAsyncEnumerable<T>` | Large database queries, file processing |
| **Internal building** | `List<T>`, `Dictionary<TKey, TValue>` | Building collections incrementally |
| **Deferred execution** | `IEnumerable<T>` | LINQ queries, database queries |
| **Cached results** | `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>` | Always materialize before caching |

## References

- [.NET Performance Best Practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/performance/)
- [C# Collection Types](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/collections)
- [IAsyncEnumerable Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1)
- [Collection Expressions (C# 12)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/collection-expressions)
- [C# 14 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [Primary Constructors (C# 12)](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#primary-constructors)

