namespace FoodDiary.MailRelay.Application.Common.Results;

public abstract class Result {
    protected Result(bool isSuccess, MailRelayError? error) {
        if (isSuccess == (error is not null)) {
            throw new InvalidOperationException("Result state is invalid.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public MailRelayError? Error { get; }

    public static Result Success() => new NonGenericResult(isSuccess: true, error: null);

    public static Result Failure(MailRelayError error) => new NonGenericResult(isSuccess: false, error);

    public static Result<T> Success<T>(T value) => new(value, isSuccess: true, error: null);

    public static Result<T> Failure<T>(MailRelayError error) => new(default, isSuccess: false, error);

    private sealed class NonGenericResult(bool isSuccess, MailRelayError? error) : Result(isSuccess, error);
}

public sealed class Result<T> : Result {
    internal Result(T? value, bool isSuccess, MailRelayError? error) : base(isSuccess, error) {
        Value = value;
    }

    public T Value => IsSuccess
        ? field!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");
}
