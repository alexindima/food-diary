namespace FoodDiary.Results;

public sealed record Error(
    string Code,
    string Message,
    ErrorKind? Kind = null,
    IReadOnlyDictionary<string, string[]>? Details = null) {
    public static readonly Error None = new(string.Empty, string.Empty);

    public static implicit operator string(Error error) => error.Code;
}
