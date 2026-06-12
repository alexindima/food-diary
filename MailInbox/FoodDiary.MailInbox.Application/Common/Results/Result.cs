namespace FoodDiary.MailInbox.Application.Common.Results;

public class Result {
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

    public static Result Success() => new(isSuccess: true, error: null);

    public static Result Failure(MailInboxError error) => new(isSuccess: false, error);

    public static Result<T> Success<T>(T value) => new(value, isSuccess: true, error: null);

    public static Result<T> Failure<T>(MailInboxError error) => new(default, isSuccess: false, error);
}

public sealed class Result<T> : Result {
    internal Result(T? value, bool isSuccess, MailInboxError? error) : base(isSuccess, error) {
        Value = value;
    }

    public T Value => IsSuccess
        ? field!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");
}
