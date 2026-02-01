using Microsoft.Extensions.Options;
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

    public TelegramBotWorker(
        ITelegramBotClient botClient,
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramBotWorker> logger)
    {
        _botClient = botClient;
        _options = options.Value;
        _logger = logger;
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
                await SendStartAsync(message.Chat.Id, cancellationToken);
                return;
            case "/help":
                await SendHelpAsync(message.Chat.Id, cancellationToken);
                return;
            default:
                await SendHelpAsync(message.Chat.Id, cancellationToken);
                return;
        }
    }

    private Task SendStartAsync(long chatId, CancellationToken cancellationToken)
    {
        var text = "Hello! Open the diary using the button below.";
        IReplyMarkup? replyMarkup = null;
        if (!string.IsNullOrWhiteSpace(_options.WebAppUrl))
        {
            replyMarkup = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithWebApp("Open diary", _options.WebAppUrl));
        }

        return _botClient.SendMessage(
            chatId,
            text,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    private Task SendHelpAsync(long chatId, CancellationToken cancellationToken)
    {
        const string text = "Available commands:\n/start - open diary\n/help - help";
        return _botClient.SendMessage(chatId, text, cancellationToken: cancellationToken);
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
}
