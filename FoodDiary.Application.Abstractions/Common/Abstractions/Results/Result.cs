namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public abstract class Result {
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    private protected Result(bool isSuccess, Error error) {
        switch (isSuccess) {
            case true when error != Error.None:
                throw new InvalidOperationException("A successful result cannot contain an error.");
            case false when error == Error.None:
                throw new InvalidOperationException("A failed result must contain an error.");
            default:
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }

    public static Result Success() => new NonGenericResult(isSuccess: true, Error.None);
    public static Result Failure(Error error) => new NonGenericResult(isSuccess: false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, isSuccess: true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, isSuccess: false, error);

    private sealed class NonGenericResult(bool isSuccess, Error error) : Result(isSuccess, error);
}

public sealed class Result<TValue> : Result {
    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) {
        Value = value;
    }

    public TValue Value => IsSuccess
        ? field!
        : throw new InvalidOperationException("Value is unavailable for a failed result.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
