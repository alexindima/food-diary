namespace FoodDiary.MailRelay.Application.Common.Result;

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

    public static Result Success() => new(true, null);

    public static Result Failure(MailRelayError error) => new(false, error);
}

public sealed class Result<T> : Result {
    private readonly T? _value;

    private Result(T value) : base(true, null) {
        _value = value;
    }

    private Result(MailRelayError error) : base(false, error) {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(MailRelayError error) => new(error);
}
