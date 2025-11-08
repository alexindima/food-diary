namespace FoodDiary.Application.Common.Abstractions.Result;

/// <summary>
/// Представляет результат операции
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Успешный результат не может содержать ошибку");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Неуспешный результат должен содержать ошибку");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default!, false, error);
}

/// <summary>
/// Представляет результат операции со значением
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Значение недоступно для неуспешного результата");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
