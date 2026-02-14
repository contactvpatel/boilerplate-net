namespace WebShop.Business.Models;

/// <summary>
/// Represents the result of an operation that may succeed with a value or fail with not-found.
/// Replaces null returns for explicit success/not-found handling.
/// </summary>
/// <typeparam name="T">The type of the value when successful.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess)
    {
        _value = value;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    public static Result<T> Success(T value)
    {
        return new(value, true);
    }

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static Result<T> NotFound()
    {
        return new(default, false);
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the entity was not found.
    /// </summary>
    public bool IsNotFound => !IsSuccess;

    /// <summary>
    /// Gets the value when successful. Throws <see cref="InvalidOperationException"/> when not found.
    /// </summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value when result is NotFound.");

    /// <summary>
    /// Gets the value or default when not found.
    /// </summary>
    public T? ValueOrNull => IsSuccess ? _value : default;

    /// <summary>
    /// Converts to nullable for backward compatibility during migration.
    /// </summary>
    public T? ToNullable()
    {
        return ValueOrNull;
    }

    /// <summary>
    /// Matches on the result and returns a value.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TResult> onNotFound)
    {
        return IsSuccess ? onSuccess(_value!) : onNotFound();
    }
}
