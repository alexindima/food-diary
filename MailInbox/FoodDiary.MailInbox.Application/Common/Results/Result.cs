namespace FoodDiary.MailInbox.Application.Common.Results;

public abstract class Result {
    protected Result(bool isSuccess, MailInboxError? error) {
        if (isSuccess == (error is not null)) {
            throw new InvalidOperationException("Result state is invalid.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public MailInboxError? Error { get; }

    public static Result Success() => new NonGenericResult(isSuccess: true, error: null);

    public static Result Failure(MailInboxError error) => new NonGenericResult(isSuccess: false, error);

    public static Result<T> Success<T>(T value) => new(value, isSuccess: true, error: null);

    public static Result<T> Failure<T>(MailInboxError error) => new(default, isSuccess: false, error);

    private sealed class NonGenericResult(bool isSuccess, MailInboxError? error) : Result(isSuccess, error);
}

public sealed class Result<T> : Result {
    internal Result(T? value, bool isSuccess, MailInboxError? error) : base(isSuccess, error) {
        Value = value;
    }

    public T Value => IsSuccess
        ? field!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");
}
