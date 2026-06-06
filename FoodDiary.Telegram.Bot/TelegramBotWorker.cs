using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot;

public sealed class TelegramBotWorker(
    ITelegramBotClient botClient,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramBotWorker> logger,
    IHttpClientFactory httpClientFactory)
    : BackgroundService {
    private readonly TelegramBotOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (string.IsNullOrWhiteSpace(_options.Token)) {
            logger.LogCritical("Telegram bot token is not configured.");
            return;
        }

        User me = await botClient.GetMe(stoppingToken).ConfigureAwait(false);
        logger.LogInformation("Telegram bot started as {Username}", me.Username ?? me.Id.ToString(CultureInfo.InvariantCulture));

        var receiverOptions = new ReceiverOptions {
            AllowedUpdates = Array.Empty<UpdateType>(),
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken);

        await WaitForStopAsync(stoppingToken).ConfigureAwait(false);
        logger.LogInformation("Telegram bot stopping.");
    }

    private static async Task WaitForStopAsync(CancellationToken stoppingToken) {
        if (stoppingToken.IsCancellationRequested) {
            return;
        }

        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = stoppingToken.Register(static state =>
            ((TaskCompletionSource)state!).TrySetResult(), stopped);
        await stopped.Task.ConfigureAwait(false);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient tBotClient, Update update, CancellationToken cancellationToken) {
        if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: not null }) {
            await HandleCallbackAsync(update.CallbackQuery, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (update.Type != UpdateType.Message) {
            return;
        }

        Message? message = update.Message;
        if (message?.Text is null) {
            return;
        }

        string command = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        switch (command) {
            case "/start":
                await SendStartAsync(message.Chat.Id, message.From?.Id, cancellationToken).ConfigureAwait(false);
                return;
            case "/help":
                await SendHelpAsync(message.Chat.Id, cancellationToken).ConfigureAwait(false);
                return;
            default:
                await SendHelpAsync(message.Chat.Id, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    private async Task SendStartAsync(long chatId, long? telegramUserId, CancellationToken cancellationToken) {
        bool isLinked = await IsLinkedAsync(telegramUserId, cancellationToken).ConfigureAwait(false);
        if (!isLinked) {
            string notLinkedText = "To use the bot, open the WebApp once and log in or register.";
            InlineKeyboardMarkup? markup = BuildWebAppMarkup();
            await botClient.SendMessage(
                chatId,
                notLinkedText,
                replyMarkup: markup,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

        string text = "Quick actions:";
        InlineKeyboardMarkup keyboard = BuildQuickActionsMarkup();
        await botClient.SendMessage(
            chatId,
            text,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Task SendHelpAsync(long chatId, CancellationToken cancellationToken) {
        const string text = "Available commands:\n/start - open diary\n/help - help";
        return botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
    }

    private async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken) {
        if (callbackQuery.Data is null || callbackQuery.From is null) {
            return;
        }

        if (callbackQuery.Data.StartsWith("water:", StringComparison.Ordinal)) {
            if (!BotInputParser.TryParseWaterAmount(callbackQuery.Data, out int amountMl)) {
                await AnswerCallbackAsync(callbackQuery, "Invalid amount.", cancellationToken).ConfigureAwait(false);
                return;
            }

            string? accessToken = await TryGetAccessTokenAsync(callbackQuery.From.Id, cancellationToken).ConfigureAwait(false);
            if (accessToken is null) {
                await AnswerCallbackAsync(callbackQuery, "Please open the WebApp and log in once.", cancellationToken).ConfigureAwait(false);
                return;
            }

            bool success = await CreateHydrationAsync(accessToken, amountMl, cancellationToken).ConfigureAwait(false);
            await AnswerCallbackAsync(callbackQuery, success ? string.Create(CultureInfo.InvariantCulture, $"Added {amountMl} ml.") : "Failed to add water.", cancellationToken).ConfigureAwait(false);
        }
    }

    private Task AnswerCallbackAsync(CallbackQuery callbackQuery, string message, CancellationToken cancellationToken) {
        return botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            message,
            cancellationToken: cancellationToken);
    }

    private InlineKeyboardMarkup BuildQuickActionsMarkup() {
        var buttons = new List<List<InlineKeyboardButton>> {
            new() {
                InlineKeyboardButton.WithCallbackData("+250 ml", "water:250"),
                InlineKeyboardButton.WithCallbackData("+500 ml", "water:500"),
            },
        };

        var webAppButtons = new List<InlineKeyboardButton>();
        string? webAppUrl = BotUriHelper.NormalizeWebAppUrl(_options.WebAppUrl);
        if (!string.IsNullOrWhiteSpace(webAppUrl)) {
            webAppButtons.Add(InlineKeyboardButton.WithWebApp("Open diary", webAppUrl));
            webAppButtons.Add(InlineKeyboardButton.WithWebApp("Add meal", $"{webAppUrl}/consumptions/add"));
        }

        if (webAppButtons.Count > 0) {
            buttons.Add(webAppButtons);
        }

        return new InlineKeyboardMarkup(buttons);
    }

    private InlineKeyboardMarkup? BuildWebAppMarkup() {
        string? webAppUrl = BotUriHelper.NormalizeWebAppUrl(_options.WebAppUrl);
        if (string.IsNullOrWhiteSpace(webAppUrl)) {
            return null;
        }

        return new InlineKeyboardMarkup(
            InlineKeyboardButton.WithWebApp("Open diary", webAppUrl));
    }

    private async Task<bool> IsLinkedAsync(long? telegramUserId, CancellationToken cancellationToken) {
        if (telegramUserId is not > 0) {
            return false;
        }

        string? token = await TryGetAccessTokenAsync(telegramUserId.Value, cancellationToken).ConfigureAwait(false);
        return token is not null;
    }

    private async Task<string?> TryGetAccessTokenAsync(long telegramUserId, CancellationToken cancellationToken) {
        if (!BotUriHelper.TryCreateApiBaseUri(_options.ApiBaseUrl, out Uri? baseUri) ||
            string.IsNullOrWhiteSpace(_options.ApiSecret)) {
            logger.LogWarning("Telegram bot API settings are missing.");
            return null;
        }

        HttpClient client = httpClientFactory.CreateClient();
        client.BaseAddress = baseUri;
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Secret", _options.ApiSecret);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/telegram/bot/auth",
            new TelegramBotAuthRequest(telegramUserId),
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode) {
            return null;
        }

        AuthResponse? authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return authResponse?.AccessToken;
    }

    private async Task<bool> CreateHydrationAsync(string accessToken, int amountMl, CancellationToken cancellationToken) {
        if (!BotUriHelper.TryCreateApiBaseUri(_options.ApiBaseUrl, out Uri? baseUri)) {
            return false;
        }

        HttpClient client = httpClientFactory.CreateClient();
        client.BaseAddress = baseUri;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateHydrationRequest(DateTime.UtcNow, amountMl);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/hydrations", request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private Task HandleErrorAsync(ITelegramBotClient tBotClient, Exception exception, CancellationToken cancellationToken) {
        if (exception is ApiRequestException apiRequestException) {
            logger.LogWarning(exception, "Telegram API error: {Code} {Message}", apiRequestException.ErrorCode, apiRequestException.Message);
            return Task.CompletedTask;
        }

        logger.LogError(exception, "Telegram bot error");
        return Task.CompletedTask;
    }

    private sealed record TelegramBotAuthRequest(long TelegramUserId);

    private sealed record AuthResponse(string AccessToken, string RefreshToken);

    private sealed record CreateHydrationRequest(DateTime TimestampUtc, int AmountMl);
}
