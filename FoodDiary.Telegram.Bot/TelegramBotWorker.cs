using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot;

public sealed class TelegramBotWorker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramBotWorker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public TelegramBotWorker(
        ITelegramBotClient botClient,
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramBotWorker> logger,
        IHttpClientFactory httpClientFactory)
    {
        _botClient = botClient;
        _options = options.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogCritical("Telegram bot token is not configured.");
            return;
        }

        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation("Telegram bot started as {Username}", me.Username ?? me.Id.ToString());

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is not null)
        {
            await HandleCallbackAsync(update.CallbackQuery, cancellationToken);
            return;
        }

        if (update.Type != UpdateType.Message)
        {
            return;
        }

        var message = update.Message;
        if (message?.Text is null)
        {
            return;
        }

        var command = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        switch (command)
        {
            case "/start":
                await SendStartAsync(message.Chat.Id, message.From?.Id, cancellationToken);
                return;
            case "/help":
                await SendHelpAsync(message.Chat.Id, cancellationToken);
                return;
            default:
                await SendHelpAsync(message.Chat.Id, cancellationToken);
                return;
        }
    }

    private async Task SendStartAsync(long chatId, long? telegramUserId, CancellationToken cancellationToken)
    {
        var isLinked = await IsLinkedAsync(telegramUserId, cancellationToken);
        if (!isLinked)
        {
            var notLinkedText = "To use the bot, open the WebApp once and log in or register.";
            var markup = BuildWebAppMarkup();
            await _botClient.SendMessage(
                chatId,
                notLinkedText,
                replyMarkup: markup,
                cancellationToken: cancellationToken);
            return;
        }

        var text = "Quick actions:";
        var keyboard = BuildQuickActionsMarkup();
        await _botClient.SendMessage(
            chatId,
            text,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private Task SendHelpAsync(long chatId, CancellationToken cancellationToken)
    {
        const string text = "Available commands:\n/start - open diary\n/help - help";
        return _botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
    }

    private async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.Data is null || callbackQuery.From is null)
        {
            return;
        }

        if (callbackQuery.Data.StartsWith("water:", StringComparison.Ordinal))
        {
            var amountText = callbackQuery.Data.Replace("water:", string.Empty);
            if (!int.TryParse(amountText, out var amountMl) || amountMl <= 0)
            {
                await AnswerCallbackAsync(callbackQuery, "Invalid amount.", cancellationToken);
                return;
            }

            var accessToken = await TryGetAccessTokenAsync(callbackQuery.From.Id, cancellationToken);
            if (accessToken is null)
            {
                await AnswerCallbackAsync(callbackQuery, "Please open the WebApp and log in once.", cancellationToken);
                return;
            }

            var success = await CreateHydrationAsync(accessToken, amountMl, cancellationToken);
            await AnswerCallbackAsync(callbackQuery, success ? $"Added {amountMl} ml." : "Failed to add water.", cancellationToken);
        }
    }

    private Task AnswerCallbackAsync(CallbackQuery callbackQuery, string message, CancellationToken cancellationToken)
    {
        return _botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            message,
            cancellationToken: cancellationToken);
    }

    private InlineKeyboardMarkup BuildQuickActionsMarkup()
    {
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("+250 ml", "water:250"),
                InlineKeyboardButton.WithCallbackData("+500 ml", "water:500"),
            }
        };

        var webAppButtons = new List<InlineKeyboardButton>();
        var webAppUrl = _options.WebAppUrl?.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(webAppUrl))
        {
            webAppButtons.Add(InlineKeyboardButton.WithWebApp("Open diary", webAppUrl));
            webAppButtons.Add(InlineKeyboardButton.WithWebApp("Add meal", $"{webAppUrl}/consumptions/add"));
        }

        if (webAppButtons.Count > 0)
        {
            buttons.Add(webAppButtons);
        }

        return new InlineKeyboardMarkup(buttons);
    }

    private InlineKeyboardMarkup? BuildWebAppMarkup()
    {
        var webAppUrl = _options.WebAppUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(webAppUrl))
        {
            return null;
        }

        return new InlineKeyboardMarkup(
            InlineKeyboardButton.WithWebApp("Open diary", webAppUrl));
    }

    private async Task<bool> IsLinkedAsync(long? telegramUserId, CancellationToken cancellationToken)
    {
        if (!telegramUserId.HasValue || telegramUserId.Value <= 0)
        {
            return false;
        }

        var token = await TryGetAccessTokenAsync(telegramUserId.Value, cancellationToken);
        return token is not null;
    }

    private async Task<string?> TryGetAccessTokenAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiBaseUrl) || string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            _logger.LogWarning("Telegram bot API settings are missing.");
            return null;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.ApiBaseUrl);
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Secret", _options.ApiSecret);

        var response = await client.PostAsJsonAsync(
            "/api/auth/telegram/bot/auth",
            new TelegramBotAuthRequest(telegramUserId),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        return authResponse?.AccessToken;
    }

    private async Task<bool> CreateHydrationAsync(string accessToken, int amountMl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiBaseUrl))
        {
            return false;
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.ApiBaseUrl);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateHydrationRequest(DateTime.UtcNow, amountMl);
        var response = await client.PostAsJsonAsync("/api/hydrations", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ApiRequestException apiRequestException)
        {
            _logger.LogWarning(exception, "Telegram API error: {Code} {Message}", apiRequestException.ErrorCode, apiRequestException.Message);
            return Task.CompletedTask;
        }

        _logger.LogError(exception, "Telegram bot error");
        return Task.CompletedTask;
    }

    private sealed record TelegramBotAuthRequest(long TelegramUserId);
    private sealed record AuthResponse(string AccessToken, string RefreshToken);
    private sealed record CreateHydrationRequest(DateTime TimestampUtc, int AmountMl);
}
