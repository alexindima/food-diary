namespace FoodDiary.Infrastructure.Options;

public sealed class GoogleAuthOptions {
    public const string SectionName = "GoogleAuth";

    public string ClientId { get; init; } = string.Empty;

    public static bool HasValidClientId(GoogleAuthOptions options) =>
        string.IsNullOrWhiteSpace(options.ClientId) || !string.IsNullOrWhiteSpace(options.ClientId.Trim());
}
