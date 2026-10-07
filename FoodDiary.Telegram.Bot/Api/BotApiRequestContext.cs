namespace FoodDiary.Telegram.Bot.Api;

internal sealed record BotApiRequestContext(string? AccessToken = null, string? ApiSecret = null);
