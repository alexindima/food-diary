using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Modules.Identity.Infrastructure.Providers.Options;

public sealed class TelegramClientOptions : ITelegramIdentityPolicy, ITelegramOperationPolicy {
    public const string SectionName = "TelegramClient";
    public bool LoginEnabled { get; init; }
    public bool RegistrationEnabled { get; init; }
    public bool OperationsEnabled { get; init; }
    public long BotId { get; init; }

    public static bool HasCompatibleBot(TelegramClientOptions options, TelegramAuthOptions auth) {
        if (!options.LoginEnabled && !options.RegistrationEnabled && !options.OperationsEnabled) {
            return true;
        }
        if (options.RegistrationEnabled && !options.LoginEnabled) {
            return false;
        }
        string[] tokenParts = auth.BotToken.Split(':', 2);
        return tokenParts.Length == 2 && !string.IsNullOrWhiteSpace(tokenParts[1]) &&
               long.TryParse(tokenParts[0], System.Globalization.NumberStyles.None,
                   System.Globalization.CultureInfo.InvariantCulture, out long tokenBotId) && tokenBotId > 0 &&
               (!options.OperationsEnabled || options.BotId == tokenBotId);
    }
}
