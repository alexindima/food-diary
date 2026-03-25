namespace FoodDiary.Application.Common.Abstractions.Result;

public sealed record Error {
    public static readonly Error None = new(string.Empty, string.Empty);

    public string Code { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string[]>? Details { get; }

    public Error(string code, string message, IReadOnlyDictionary<string, string[]>? details = null) {
        Code = code;
        Message = message;
        Details = details;
    }

    public static implicit operator string(Error error) => error.Code;
}
