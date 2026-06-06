namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public class Result {
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error) {
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

    public static Result Success() => new(isSuccess: true, Error.None);
    public static Result Failure(Error error) => new(isSuccess: false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, isSuccess: true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, isSuccess: false, error);
}

public class Result<TValue> : Result {
    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) {
        Value = value;
    }

    public TValue Value => IsSuccess
        ? field!
        : throw new InvalidOperationException("Value is unavailable for a failed result.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
