namespace FoodDiary.Integrations.Options;

public sealed class TelegramOidcOptions {
    public const string SectionName = "TelegramOidc";

    public bool Enabled { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;

    public static bool HasCompatibleBot(TelegramOidcOptions options, TelegramAuthOptions auth) {
        if (!options.Enabled) {
            return true;
        }
        string[] tokenParts = auth.BotToken.Split(':', 2);
        return tokenParts.Length == 2 && !string.IsNullOrWhiteSpace(tokenParts[1]) &&
               long.TryParse(tokenParts[0], System.Globalization.NumberStyles.None,
                   System.Globalization.CultureInfo.InvariantCulture, out long botId) && botId > 0 &&
               long.TryParse(options.ClientId, System.Globalization.NumberStyles.None,
                   System.Globalization.CultureInfo.InvariantCulture, out long clientId) && clientId == botId;
    }

    public static bool IsValid(TelegramOidcOptions options) => !options.Enabled ||
        (long.TryParse(options.ClientId, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out long clientId) && clientId > 0 &&
         !string.IsNullOrWhiteSpace(options.ClientSecret) &&
         Uri.TryCreate(options.RedirectUri, UriKind.Absolute, out Uri? redirect) &&
         string.Equals(redirect.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) &&
         string.IsNullOrEmpty(redirect.Fragment) && string.IsNullOrEmpty(redirect.UserInfo));
}
