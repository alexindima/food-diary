namespace FoodDiary.Application.Common.Abstractions.Result;

/// <summary>
/// Представляет ошибку
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static implicit operator string(Error error) => error.Code;
}
