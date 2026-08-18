namespace FoodDiary.Results;

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// </summary>
public abstract class Result {
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the failure information, or <see cref="Error.None"/> for a successful result.
    /// </summary>
    public Error Error { get; }

    protected Result(bool isSuccess, Error error) {
        ArgumentNullException.ThrowIfNull(error);

        switch (isSuccess) {
            case true when error != Error.None:
                throw new InvalidOperationException("A successful result cannot contain an error.");
            case false when error == Error.None:
                throw new InvalidOperationException("A failed result must contain an error.");
            case false when string.IsNullOrWhiteSpace(error.Code):
                throw new InvalidOperationException("A failed result must contain a non-empty error code.");
            case false when string.IsNullOrWhiteSpace(error.Message):
                throw new InvalidOperationException("A failed result must contain a non-empty error message.");
            default:
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }

    /// <summary>
    /// Creates a successful result without a value.
    /// </summary>
    public static Result Success() => new NonGenericResult(isSuccess: true, Error.None);

    /// <summary>
    /// Creates a failed result without a value.
    /// </summary>
    /// <param name="error">The failure information.</param>
    public static Result Failure(Error error) => new NonGenericResult(isSuccess: false, error);

    /// <summary>
    /// Creates a successful result containing <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="value">The operation value.</param>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, isSuccess: true, Error.None);

    /// <summary>
    /// Creates a failed result for an operation returning <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="error">The failure information.</param>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, isSuccess: false, error);

    private sealed class NonGenericResult(bool isSuccess, Error error) : Result(isSuccess, error);
}

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
/// <typeparam name="TValue">The value type.</typeparam>
public sealed class Result<TValue> : Result {
    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) {
        Value = value;
    }

    /// <summary>
    /// Gets the operation value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The result represents a failure.</exception>
    public TValue Value => IsSuccess
        ? field!
        : throw new InvalidOperationException("Value is unavailable for a failed result.");

    /// <summary>
    /// Converts a value to a successful result.
    /// </summary>
    /// <param name="value">The operation value.</param>
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
