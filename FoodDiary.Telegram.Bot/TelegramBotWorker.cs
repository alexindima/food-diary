using Microsoft.Extensions.Options;
using FoodDiary.Telegram.Bot.Images;
using FoodDiary.Telegram.Bot.Operations;
using System.Text.Json;
using System.Globalization;
using System.Net.Http.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class TelegramBotWorker(
    ITelegramBotClient botClient,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramBotWorker> logger,
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider)
    : BackgroundService {
    private static readonly TimeSpan StartupErrorInitialRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StartupErrorMaxRetryDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollingErrorRetryDelay = TimeSpan.FromSeconds(2);
    private readonly TelegramBotOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (string.IsNullOrWhiteSpace(_options.Token)) {
            logger.LogCritical("Telegram bot token is not configured.");
            return;
        }

        User me = await GetMeWithRetryAsync(stoppingToken).ConfigureAwait(false);
        logger.LogInformation("Telegram bot started as {Username}", me.Username ?? me.Id.ToString(CultureInfo.InvariantCulture));

        if (_options.OperationsEnabled) {
            var receiver = new DurableTelegramReceiver(botClient, PersistOrHandleUpdateAsync, timeProvider, logger);
            await receiver.RunAsync(stoppingToken).ConfigureAwait(false);
            return;
        }

        var receiverOptions = new ReceiverOptions {
            AllowedUpdates = [],
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken);

        await WaitForStopAsync(stoppingToken).ConfigureAwait(false);
        logger.LogInformation("Telegram bot stopping.");
    }

    private async Task<User> GetMeWithRetryAsync(CancellationToken stoppingToken) {
        TimeSpan retryDelay = StartupErrorInitialRetryDelay;
        while (true) {
            try {
                return await botClient.GetMe(stoppingToken).ConfigureAwait(false);
            } catch (RequestException exception) when (IsTransientStartupFailure(exception)) {
                object failureCode = exception is ApiRequestException apiRequestException
                    ? apiRequestException.ErrorCode
                    : "transport";
                logger.LogWarning(
                    "Telegram bot startup request failed ({FailureCode}); retrying in {RetryDelaySeconds} seconds.",
                    failureCode,
                    retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, timeProvider, stoppingToken).ConfigureAwait(false);
                retryDelay = TimeSpan.FromSeconds(Math.Min(
                    retryDelay.TotalSeconds * 2,
                    StartupErrorMaxRetryDelay.TotalSeconds));
            }
        }
    }

    private static bool IsTransientStartupFailure(RequestException exception) =>
        exception is not ApiRequestException apiRequestException ||
        apiRequestException.ErrorCode == 429 ||
        apiRequestException.ErrorCode >= 500;

    private static async Task WaitForStopAsync(CancellationToken stoppingToken) {
        if (stoppingToken.IsCancellationRequested) {
            return;
        }

        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = stoppingToken.Register(static state =>
            ((TaskCompletionSource)state!).TrySetResult(), stopped);
        await using (registration.ConfigureAwait(false)) {
            await stopped.Task.ConfigureAwait(false);
        }
    }

    private async Task PersistOrHandleUpdateAsync(Update update, CancellationToken cancellationToken) {
        BotIncomingOperation? operation = null;
        if (update.Message is { Chat.Type: ChatType.Private, From: { IsBot: false } sender } message && message.Chat.Id == sender.Id) {
            TelegramImageSelection? image = TelegramImageSelector.Select(message, out _);
            if (image is not null) {
                operation = new BotIncomingOperation("photo", message.Chat.Id, sender.Id, message.Id, message.Date,
                    sender.LanguageCode, image.FileId, image.ContentType, message.Caption);
            } else if (message.Text is "/today" or "/week") {
                operation = new BotIncomingOperation("statistics", message.Chat.Id, sender.Id, message.Id, message.Date,
                    sender.LanguageCode, PeriodDays: string.Equals(message.Text, "/today", StringComparison.Ordinal) ? 1 : 7);
            }
        } else if (update.CallbackQuery is { From: { IsBot: false } actor, Message.Chat.Type: ChatType.Private } callback &&
            callback.Message.Chat.Id == actor.Id) {
            if (callback.Data is "stats:today" or "stats:week") {
                operation = new BotIncomingOperation("statistics", callback.Message.Chat.Id, actor.Id, callback.Message.Id,
                    OccurredAtUtc: null, actor.LanguageCode, CallbackId: callback.Id,
                    PeriodDays: string.Equals(callback.Data, "stats:today", StringComparison.Ordinal) ? 1 : 7);
            } else if (BotInputParser.TryParseWaterAmount(callback.Data, out int amount)) {
                operation = new BotIncomingOperation("water", callback.Message.Chat.Id, actor.Id, callback.Message.Id,
                    OccurredAtUtc: null, actor.LanguageCode, CallbackId: callback.Id, AmountMl: amount);
            } else if (callback.Data?.StartsWith("meal:undo:", StringComparison.Ordinal) == true &&
                Guid.TryParseExact(callback.Data[10..], "N", out Guid mealOperationId) && mealOperationId != Guid.Empty) {
                operation = new BotIncomingOperation("meal-undo", callback.Message.Chat.Id, actor.Id, callback.Message.Id,
                    OccurredAtUtc: null, actor.LanguageCode, CallbackId: callback.Id, MealOperationId: mealOperationId);
            }
        }
        if (operation is null) {
            await HandleUpdateAsync(botClient, update, cancellationToken).ConfigureAwait(false);
            return;
        }
        using HttpClient http = httpClientFactory.CreateClient(BotOperationClient.ClientName);
        var client = new BotOperationClient(http, options);
        try {
            await client.RegisterAsync(update.Id, operation.TelegramUserId, JsonSerializer.Serialize(operation), cancellationToken).ConfigureAwait(false);
        } catch (BotOperationApiException error) when (error.ErrorCode is "Authentication.TelegramNotLinked" or
            "Authentication.InvalidToken" or "Authentication.AccountDeleted") {
            string instruction = operation.Language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true
                ? "Откройте Food Diary, чтобы войти в аккаунт или подключить Telegram."
                : "Open Food Diary to sign in or connect Telegram.";
            await botClient.SendMessage(operation.ChatId, instruction,
                replyMarkup: BotMenu.Open(_options, operation.Language, "/auth/telegram"), cancellationToken: cancellationToken).ConfigureAwait(false);
        } catch (BotOperationApiException error) when (string.Equals(error.ErrorCode, "Telegram.OperationConflict", StringComparison.Ordinal)) {
            // The durable update belongs to a different binding or payload; do not replay it under a new account.
        }
        if (operation.CallbackId is { } callbackId) {
            try {
                await botClient.AnswerCallbackQuery(callbackId, cancellationToken: cancellationToken).ConfigureAwait(false);
            } catch (ApiRequestException error) when (error.ErrorCode == 400) {
                // A durable replay may outlive Telegram's callback acknowledgement window.
            }
        }
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
        if (message?.Chat.Type != ChatType.Private) {
            return;
        }
        _ = TelegramImageSelector.Select(message, out string? imageError);
        if (imageError is not null) {
            bool russian = message.From?.LanguageCode?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;
            string explanation = (imageError, russian) switch {
                ("album", true) => "Отправьте одно фото отдельным сообщением. Альбомы пока не поддерживаются.",
                ("album", false) => "Send one photo in a separate message. Albums are not supported yet.",
                ("format", true) => "Отправьте фото или изображение JPEG, PNG либо WebP.",
                ("format", false) => "Send a photo or a JPEG, PNG, or WebP image.",
                (_, true) => "Изображение не подходит для загрузки. Отправьте файл размером до 20 МБ.",
                _ => "This image cannot be uploaded. Send a file up to 20 MB.",
            };
            await botClient.SendMessage(message.Chat.Id, explanation, cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }
        if (string.IsNullOrWhiteSpace(message.Text)) {
            return;
        }

        switch (message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]) {
            case "/start":
                await SendStartAsync(message.Chat.Id, message.From?.Id, message.From?.LanguageCode, cancellationToken).ConfigureAwait(false);
                return;
            case "/water":
                await SendWaterMenuAsync(message.Chat.Id, message.From?.LanguageCode, cancellationToken).ConfigureAwait(false);
                return;
            case "/settings":
                await botClient.SendMessage(message.Chat.Id,
                    BotMenu.IsRussian(message.From?.LanguageCode) ? "Настройки аккаунта:" : "Account settings:",
                    replyMarkup: BotMenu.Open(_options, message.From?.LanguageCode, "/profile"), cancellationToken: cancellationToken).ConfigureAwait(false);
                return;
            default:
                await SendHelpAsync(message.Chat.Id, message.From?.LanguageCode, cancellationToken).ConfigureAwait(false);
                return;
        }
    }

    private async Task SendStartAsync(long chatId, long? telegramUserId, string? language, CancellationToken cancellationToken) {
        bool isLinked = await IsLinkedAsync(telegramUserId, cancellationToken).ConfigureAwait(false);
        if (!isLinked) {
            string notLinkedText = BotMenu.IsRussian(language) ? "Откройте FoodDiary, чтобы войти или создать аккаунт через Telegram." : "To use the bot, open the WebApp once and log in or register.";
            InlineKeyboardMarkup? markup = BotMenu.Open(_options, language, "/auth/telegram");
            await botClient.SendMessage(
                chatId,
                notLinkedText,
                replyMarkup: markup,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

        string text = BotMenu.IsRussian(language) ? "Быстрые действия:" : "Quick actions:";
        InlineKeyboardMarkup keyboard = BotMenu.Actions(_options, language);
        await botClient.SendMessage(
            chatId,
            text,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Task SendHelpAsync(long chatId, string? language, CancellationToken cancellationToken) {
        return botClient.SendMessage(chatId, BotMenu.Help(language, _options.OperationsEnabled),
            replyMarkup: BotMenu.Actions(_options, language), cancellationToken: cancellationToken);
    }

    private Task SendWaterMenuAsync(long chatId, string? language, CancellationToken cancellationToken) =>
        botClient.SendMessage(chatId, BotMenu.IsRussian(language) ? "Сколько воды добавить?" : "How much water would you like to add?",
            replyMarkup: BotMenu.Water(language), cancellationToken: cancellationToken);

    private async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken) {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (callbackQuery.Data is null || callbackQuery.From is null) {
            return;
        }
        if (callbackQuery.Message?.Chat.Type != ChatType.Private ||
            callbackQuery.Message.Chat.Id != callbackQuery.From.Id || callbackQuery.From.IsBot) {
            return;
        }

        if (callbackQuery.Data is "menu:food" or "menu:water" or "menu:help") {
            await HandleMenuCallbackAsync(callbackQuery, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (callbackQuery.Data.StartsWith("water:", StringComparison.Ordinal)) {
            bool russian = BotMenu.IsRussian(callbackQuery.From.LanguageCode);
            if (!BotInputParser.TryParseWaterAmount(callbackQuery.Data, out int amountMl)) {
                await AnswerCallbackAsync(callbackQuery, russian ? "Некорректный объём." : "Invalid amount.", cancellationToken).ConfigureAwait(false);
                return;
            }

            string? accessToken = await TryGetAccessTokenAsync(callbackQuery.From.Id, cancellationToken).ConfigureAwait(false);
            if (accessToken is null) {
                await AnswerCallbackAsync(callbackQuery, russian ? "Откройте FoodDiary и войдите в аккаунт." : "Please open the WebApp and log in once.", cancellationToken).ConfigureAwait(false);
                return;
            }

            bool success = await CreateHydrationAsync(accessToken, amountMl, cancellationToken).ConfigureAwait(false);
            string notice = russian ? "Не удалось добавить воду." : "Failed to add water.";
            if (success) {
                notice = russian ? string.Create(CultureInfo.InvariantCulture, $"Добавлено {amountMl} мл.")
                    : string.Create(CultureInfo.InvariantCulture, $"Added {amountMl} ml.");
            }
            await AnswerCallbackAsync(callbackQuery, notice, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleMenuCallbackAsync(CallbackQuery callback, CancellationToken cancellationToken) {
        string? language = callback.From.LanguageCode;
        long chatId = callback.Message!.Chat.Id;
        if (string.Equals(callback.Data, "menu:water", StringComparison.Ordinal)) {
            await SendWaterMenuAsync(chatId, language, cancellationToken).ConfigureAwait(false);
        } else if (string.Equals(callback.Data, "menu:food", StringComparison.Ordinal) && _options.OperationsEnabled) {
            await botClient.SendMessage(chatId, BotMenu.IsRussian(language)
                ? "Отправьте одно фото еды. После распознавания запись сохранится автоматически. При необходимости нажмите «Отменить» в ответе."
                : "Send one food photo. The entry will be saved automatically after recognition. Use Undo in the reply if needed.",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        } else {
            await SendHelpAsync(chatId, language, cancellationToken).ConfigureAwait(false);
        }
        await botClient.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private Task AnswerCallbackAsync(CallbackQuery callbackQuery, string message, CancellationToken cancellationToken) {
        return botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            message,
            cancellationToken: cancellationToken);
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

        using HttpClient client = httpClientFactory.CreateClient();
        client.BaseAddress = baseUri;
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Secret", _options.ApiSecret);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/telegram/bot/auth",
            new { TelegramUserId = telegramUserId },
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

        using HttpClient client = httpClientFactory.CreateClient();
        client.BaseAddress = baseUri;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new {
            TimestampUtc = timeProvider.GetUtcNow().UtcDateTime,
            AmountMl = amountMl,
        };
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/hydrations", request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private async Task HandleErrorAsync(ITelegramBotClient tBotClient, Exception exception, CancellationToken cancellationToken) {
        if (exception is ApiRequestException apiRequestException) {
            logger.LogWarning("Telegram API error: {Code}", apiRequestException.ErrorCode);
        } else {
            logger.LogError("Telegram bot error: {ErrorType}", exception.GetType().Name);
        }

        await Task.Delay(PollingErrorRetryDelay, timeProvider, cancellationToken).ConfigureAwait(false);
    }

    private sealed record AuthResponse(string AccessToken);
}
