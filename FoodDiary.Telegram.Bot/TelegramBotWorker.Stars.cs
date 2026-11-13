using FoodDiary.Telegram.Bot.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Model;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FoodDiary.Telegram.Bot;

public sealed partial class TelegramBotWorker {
    private async Task<bool> TryHandleStarsUpdateAsync(Update update, CancellationToken cancellationToken) {
        TelegramStarsUpdateHttpRequest? request = MapStarsUpdate(update);
        if (request is null) {
            return false;
        }
        if (update.PreCheckoutQuery is { } query) {
            bool approved = false;
            try {
                TelegramStarsUpdateHttpResponse response = await SendStarsUpdateAsync(request, cancellationToken);
                approved = response.CheckoutApproved;
            } catch (Exception error) when (error is HttpRequestException or InvalidDataException ||
                (error is OperationCanceledException && !cancellationToken.IsCancellationRequested)) {
                logger.LogWarning("Telegram pre-checkout could not be validated; payment was not approved.");
            }
            string? message = null;
            if (!approved) {
                message = BotMenu.IsRussian(query.From.LanguageCode)
                    ? "Счёт недоступен. Откройте FoodDiary и попробуйте оформить подписку заново."
                    : "This invoice is unavailable. Open FoodDiary and start a new checkout.";
            }
            try {
                await botClient.AnswerPreCheckoutQuery(query.Id, message, cancellationToken);
            } catch (ApiRequestException error) when (error.ErrorCode == 400) {
                // Expired/already-answered queries cannot be paid again; never wedge the financial intake behind one.
                logger.LogInformation("Telegram pre-checkout query is no longer answerable; continuing intake.");
            }
        } else {
            // Returning only after the API commit keeps the polling offset behind unpersisted payments.
            await SendStarsUpdateAsync(request, cancellationToken);
        }
        return true;
    }

    internal static TelegramStarsUpdateHttpRequest? MapStarsUpdate(Update update) {
        if (update.PreCheckoutQuery is { } query) {
            return new TelegramStarsUpdateHttpRequest {
                UpdateId = update.Id,
                TelegramUserId = query.From.Id,
                Kind = "pre_checkout",
                InvoicePayload = query.InvoicePayload,
                Currency = query.Currency,
                TotalAmount = StarsAmount(query.TotalAmount),
            };
        }
        if (update.Subscription is { } subscription) {
            return new TelegramStarsUpdateHttpRequest {
                UpdateId = update.Id,
                TelegramUserId = subscription.User.Id,
                Kind = "subscription",
                InvoicePayload = subscription.InvoicePayload,
                SubscriptionState = subscription.State,
            };
        }
        if (update.Message is not { Chat.Type: ChatType.Private } message) {
            return null;
        }
        if (message.SuccessfulPayment is { } payment) {
            return new TelegramStarsUpdateHttpRequest {
                UpdateId = update.Id,
                TelegramUserId = message.Chat.Id,
                Kind = "payment",
                InvoicePayload = payment.InvoicePayload,
                Currency = payment.Currency,
                TotalAmount = StarsAmount(payment.TotalAmount),
                PaymentChargeId = payment.TelegramPaymentChargeId,
                SubscriptionExpirationUtc = payment.SubscriptionExpirationDate,
                PaymentOccurredAtUtc = message.Date,
                IsRecurring = payment.IsRecurring,
                IsFirstRecurring = payment.IsFirstRecurring,
            };
        }
        return message.RefundedPayment is { } refund
            ? new TelegramStarsUpdateHttpRequest {
                UpdateId = update.Id,
                TelegramUserId = message.Chat.Id,
                Kind = "refund",
                InvoicePayload = refund.InvoicePayload,
                Currency = refund.Currency,
                TotalAmount = StarsAmount(refund.TotalAmount),
                PaymentChargeId = refund.TelegramPaymentChargeId,
                PaymentOccurredAtUtc = message.Date,
            }
            : null;
    }

    private static int? StarsAmount(long amount) => amount is > 0 and <= 10000 ? (int)amount : null;

    private async Task<TelegramStarsUpdateHttpResponse> SendStarsUpdateAsync(TelegramStarsUpdateHttpRequest request, CancellationToken token) {
        if (!BotUriHelper.TryCreateApiBaseUri(_options.ApiBaseUrl, out Uri? baseUri) || string.IsNullOrWhiteSpace(_options.ApiSecret)) {
            throw new InvalidDataException("Telegram billing transport is not configured.");
        }
        var sdk = new BotStarsApi(new BotApiTransport(httpClientFactory.CreateClient("TelegramBilling"), baseUri!));
        using HttpResponseMessage response = await sdk.ProcessUpdateAsync("1", request, new BotApiRequestContext(ApiSecret: _options.ApiSecret), token);
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(8192, token);
        return await response.Content.ReadFromJsonAsync<TelegramStarsUpdateHttpResponse>(token)
            ?? throw new InvalidDataException("Telegram billing response is missing.");
    }
}
