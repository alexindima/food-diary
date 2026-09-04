namespace FoodDiary.Integrations.Options;

public sealed class GoogleAuthOptions {
    public const string SectionName = "GoogleAuth";

    public string ClientId { get; init; } = string.Empty;

    public static bool HasValidClientId(GoogleAuthOptions options) {
        if (string.IsNullOrWhiteSpace(options.ClientId)) {
            return true;
        }

        string clientId = options.ClientId.Trim();
        return clientId.Length <= 512 &&
               !clientId.Any(static character => char.IsWhiteSpace(character) || char.IsControl(character));
    }
}
