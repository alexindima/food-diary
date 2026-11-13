using System.Net;
using System.Text.Json;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;

public sealed class TelegramBillingMessageSender(HttpClient client, IOptions<TelegramStarsOptions> options) : IBillingTelegramMessageSender {
    public async Task<TelegramMessageDeliveryResult> SendAsync(long telegramUserId, string text, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(telegramUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        TelegramStarsOptions settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BotToken)) {
            return TelegramMessageDeliveryResult.Retry;
        }
        string path = $"https://api.telegram.org/bot{settings.BotToken}/{(settings.UseTestEnvironment ? "test/" : "")}sendMessage";
        try {
            using HttpResponseMessage response = await client.PostAsJsonAsync(path, new { chat_id = telegramUserId, text }, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Forbidden) {
                return TelegramMessageDeliveryResult.RecipientUnavailable;
            }
            if (!response.IsSuccessStatusCode) {
                return TelegramMessageDeliveryResult.Retry;
            }
            using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            JsonElement root = document.RootElement;
            return root.ValueKind is JsonValueKind.Object && root.TryGetProperty("ok", out JsonElement ok) && ok.ValueKind is JsonValueKind.True &&
                root.TryGetProperty("result", out JsonElement result) && result.ValueKind is JsonValueKind.Object &&
                result.TryGetProperty("message_id", out JsonElement messageId) && messageId.ValueKind is JsonValueKind.Number && messageId.TryGetInt64(out long id) && id > 0
                ? TelegramMessageDeliveryResult.Sent : TelegramMessageDeliveryResult.Retry;
        } catch (Exception exception) when (exception is HttpRequestException or JsonException ||
            (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)) {
            // A transport exception can contain the bot token; never propagate its message or request URI.
            return TelegramMessageDeliveryResult.Retry;
        }
    }
}
