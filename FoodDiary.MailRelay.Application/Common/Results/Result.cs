namespace FoodDiary.MailRelay.Application.Common.Results;

public class Result {
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

    public static Result Success() => new(isSuccess: true, error: null);

    public static Result Failure(MailRelayError error) => new(isSuccess: false, error);
}

public sealed class Result<T> : Result {
    private Result(T value) : base(isSuccess: true, error: null) {
        Value = value;
    }

    private Result(MailRelayError error) : base(isSuccess: false, error) {
    }

    public T Value => IsSuccess
        ? field!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(MailRelayError error) => new(error);
}
