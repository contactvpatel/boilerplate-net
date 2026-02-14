namespace WebShop.Business.Models;

/// <summary>
/// Represents the result of an operation that may succeed with a value, fail with not-found, or fail with a business rule violation.
/// Replaces null returns for explicit success/not-found handling.
/// </summary>
/// <typeparam name="T">The type of the value when successful.</typeparam>
public readonly struct Result<T>
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, string? errorMessage)
    {
        _value = value;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful result with the given value.
    /// </summary>
    public static Result<T> Success(T value)
    {
        return new(value, true, null);
    }

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static Result<T> NotFound()
    {
        return new(default, false, null);
    }

    /// <summary>
    /// Creates a failure result with a business rule or validation error message.
    /// Use for cases like duplicate email, invalid state, etc.
    /// </summary>
    public static Result<T> Failure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(default, false, message);
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the entity was not found.
    /// </summary>
    public bool IsNotFound => !IsSuccess && string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets a value indicating whether the operation failed due to a business rule violation.
    /// </summary>
    public bool IsFailure => !IsSuccess && !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets the error message when <see cref="IsFailure"/> is true.
    /// </summary>
    public string? ErrorMessage { get; }

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

    /// <summary>
    /// Matches on the result with three outcomes: success, not-found, or failure.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TResult> onNotFound, Func<string, TResult> onFailure)
    {
        if (IsSuccess)
        {
            return onSuccess(_value!);
        }

        if (IsFailure)
        {
            return onFailure(ErrorMessage!);
        }

        return onNotFound();
    }
}
