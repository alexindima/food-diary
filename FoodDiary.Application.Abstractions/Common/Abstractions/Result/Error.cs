namespace FoodDiary.Application.Abstractions.Common.Abstractions.Result;

public sealed record Error(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null,
    ErrorKind? Kind = null) {
    public static readonly Error None = new(string.Empty, string.Empty);

    public static implicit operator string(Error error) => error.Code;
}
