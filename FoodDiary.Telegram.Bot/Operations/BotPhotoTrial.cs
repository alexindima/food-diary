using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Operations;

internal static class BotPhotoTrial {
    private const string CallbackPrefix = "photo-trial:";
    internal const string Offered = "trial-offered";
    internal const string Accepted = "trial-accepted";
    internal const string OfferedCheckpoint = "{\"Stage\":\"trial-offered\"}";
    internal const string AcceptedCheckpoint = "{\"Stage\":\"trial-accepted\"}";
    internal static readonly TimeSpan OfferLifetime = TimeSpan.FromMinutes(15);

    internal static bool TryReadCallback(string? data, out BotOperationId operationId) {
        operationId = default;
        if (data?.StartsWith(CallbackPrefix, StringComparison.Ordinal) != true ||
            !Guid.TryParseExact(data.AsSpan(CallbackPrefix.Length), "N", out Guid id) || id == Guid.Empty) {
            return false;
        }
        operationId = new BotOperationId(id);
        return true;
    }

    internal static InlineKeyboardMarkup? Keyboard(TelegramBotOptions options, bool russian, BotOperationId? offer = null) {
        var rows = new List<InlineKeyboardButton[]>();
        if (offer is { } operationId) {
            rows.Add([InlineKeyboardButton.WithCallbackData(russian ? "Начать 7 дней бесплатно" : "Start 7 days free",
                $"{CallbackPrefix}{operationId.Value:N}")]);
        }
        if (BotUriHelper.NormalizeWebAppUrl(options.WebAppUrl) is { } url) {
            rows.Add([InlineKeyboardButton.WithWebApp(russian ? "Добавить вручную" : "Add manually", url + BotMenu.MealPath)]);
            if (offer is null) {
                rows.Add([InlineKeyboardButton.WithWebApp(russian ? "Посмотреть Premium" : "See Premium", url + "/premium")]);
            }
        }
        return rows.Count == 0 ? null : new InlineKeyboardMarkup(rows);
    }
}
