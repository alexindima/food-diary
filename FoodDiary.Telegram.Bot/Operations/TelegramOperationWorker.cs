using System.Globalization;
using System.Net;
using System.Text.Json;
using FoodDiary.Telegram.Bot.Images;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class TelegramOperationWorker(IHttpClientFactory clients, IOptions<TelegramBotOptions> options,
    ITelegramBotClient bot, TimeProvider timeProvider, ILogger<TelegramOperationWorker> logger) : BackgroundService {
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!options.Value.OperationsEnabled) {
            return;
        }
        while (!stoppingToken.IsCancellationRequested) {
            try {
                using HttpClient http = clients.CreateClient(BotOperationClient.ClientName);
                var operations = new BotOperationClient(http, options);
                IReadOnlyList<BotOperationId> ready = await operations.ListReadyAsync(stoppingToken);
                foreach (BotOperationId id in ready) {
                    try {
                        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        deadline.CancelAfter(TimeSpan.FromSeconds(90));
                        while (await ProcessAsync(operations, id, deadline.Token)) {
                            // Reacquire the committed checkpoint while the next step is ready.
                        }
                    } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                        return;
                    } catch (Exception error) {
                        logger.LogWarning("Telegram operation {OperationId} will resume after {ErrorType}.", id, error.GetType().Name);
                    }
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception error) {
                logger.LogWarning("Telegram work discovery failed with {ErrorType}.", error.GetType().Name);
            }
            await Task.Delay(PollInterval, timeProvider, stoppingToken);
        }
    }

    internal async Task<bool> ProcessAsync(BotOperationClient operations, BotOperationId id, CancellationToken cancellationToken) {
        BotOperationLease? lease = await operations.AcquireAsync(id, cancellationToken);
        if (lease is null) {
            return false;
        }
        BotIncomingOperation incoming;
        try {
            incoming = BotOperationStateReader.ReadIncoming(lease.Payload);
        } catch (Exception error) when (error is JsonException or InvalidDataException) {
            logger.LogWarning("Telegram operation {OperationId} rejected due to invalid input state.", id);
            await operations.CheckpointAsync(lease, "{\"ErrorCode\":\"OperationInputInvalid\"}", completed: true,
                timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
            return false;
        }
        BotPhotoCheckpoint checkpoint;
        try {
            checkpoint = BotOperationStateReader.ReadCheckpoint(lease.Checkpoint, incoming.Kind);
        } catch (Exception error) when (error is JsonException or InvalidDataException) {
            checkpoint = new BotPhotoCheckpoint(Stage: "failed", ErrorCode: "OperationStateInvalid");
        }
        if (checkpoint.Stage is "failed" or "meal-saved" or "undo-complete" or "water-saved" or "statistics-ready" or "photo-notified") {
            return await FinishOperationAsync(operations, lease, incoming, checkpoint, cancellationToken);
        }
        bool firstPhotoAttempt = incoming.Kind is "photo" && checkpoint is { Stage: "received", UploadAttempt: 0 };
        using HttpClient http = clients.CreateClient(BotDiaryClient.ClientName);
        var diary = new BotDiaryClient(http, options);
        try {
            string token = await diary.AuthenticateAsync(incoming.TelegramUserId, lease, cancellationToken);
            if (string.Equals(incoming.Kind, "water", StringComparison.Ordinal)) {
                if (lease.CreatedAtUtc == default || lease.CreatedAtUtc.Kind != DateTimeKind.Utc) {
                    throw new InvalidDataException("A durable water operation timestamp is required.");
                }
                incoming = incoming with { OccurredAtUtc = lease.CreatedAtUtc };
            }
            checkpoint = await AdvanceOperationAsync(diary, token, lease.OperationId, incoming, checkpoint, cancellationToken);
        } catch (BotRecognitionAccessException error) {
            checkpoint = checkpoint with { Stage = "failed", Upload = null, ErrorCode = error.Code };
        } catch (HttpRequestException error) when (error.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict) {
            checkpoint = checkpoint with { Stage = "failed", Upload = null, ErrorCode = error.StatusCode.Value.ToString() };
        } catch (InvalidDataException) {
            checkpoint = checkpoint with { Stage = "failed", Upload = null, ErrorCode = "InvalidImageOrIdentity" };
        }
        bool ready = checkpoint.Stage is not ("recognizing" or "received");
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        string state = JsonSerializer.Serialize(checkpoint);
        if (!await operations.CheckpointAsync(lease, state, completed: false, ready ? now : now.Add(PollInterval), cancellationToken)) {
            return false;
        }
        if (firstPhotoAttempt && checkpoint.Stage is "upload") {
            return await SavePhotoProgressAsync(operations, id, incoming, checkpoint, state, cancellationToken);
        }
        return ready;
    }

    private async Task<bool> FinishOperationAsync(BotOperationClient operations, BotOperationLease lease, BotIncomingOperation incoming,
        BotPhotoCheckpoint checkpoint, CancellationToken cancellationToken) {
        bool cleanup = false;
        if (checkpoint.Stage is "photo-notified") {
            try {
                await bot.DeleteMessage(incoming.ChatId, checkpoint.ProgressMessageId.GetValueOrDefault(), cancellationToken);
            } catch (ApiRequestException error) when (error.ErrorCode is 400 or 403) {
                // Telegram cannot delete a message that is already gone or inaccessible.
            }
        } else {
            bool delivered = checkpoint.Stage is "failed"
                ? await SendFailureAsync(incoming, checkpoint.ErrorCode, cancellationToken)
                : await SendCompletionAsync(incoming, checkpoint, cancellationToken);
            cleanup = delivered && incoming.Kind is "photo" && checkpoint.ProgressMessageId is > 0;
            if (cleanup) {
                checkpoint = new BotPhotoCheckpoint(Stage: "photo-notified", ProgressMessageId: checkpoint.ProgressMessageId);
            }
        }
        return await operations.CheckpointAsync(lease, JsonSerializer.Serialize(checkpoint), completed: !cleanup,
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken) && cleanup;
    }

    private async Task<bool> SavePhotoProgressAsync(BotOperationClient operations, BotOperationId id, BotIncomingOperation incoming,
        BotPhotoCheckpoint checkpoint, string state, CancellationToken cancellationToken) {
        // Only the worker that committed the first upload step attempts to send this notice.
        BotOperationLease? lease = await operations.AcquireAsync(id, cancellationToken);
        if (lease?.Checkpoint is not { } currentState) {
            return false;
        }
        if (string.Equals(currentState, state, StringComparison.Ordinal)) {
            int? messageId = await SendPhotoProgressAsync(incoming, id, cancellationToken);
            currentState = JsonSerializer.Serialize(checkpoint with { ProgressMessageId = messageId });
        }
        return await operations.CheckpointAsync(lease, currentState, completed: false,
            timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }

    private async Task<int?> SendPhotoProgressAsync(BotIncomingOperation incoming, BotOperationId id, CancellationToken cancellationToken) {
        string message = BotMenu.IsRussian(incoming.Language)
            ? "Фото получено. Начинаю обработку."
            : "Photo received. I’ll start processing it now.";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        try {
            Message sent = await bot.SendMessage(incoming.ChatId, message, disableNotification: true, cancellationToken: timeout.Token);
            return sent.Id;
        } catch (Exception error) when (error is RequestException ||
            (error is OperationCanceledException && !cancellationToken.IsCancellationRequested)) {
            logger.LogWarning("Telegram photo progress notice for {OperationId} could not be sent due to {ErrorType}.", id, error.GetType().Name);
            return null;
        }
    }

    private Task<bool> SendFailureAsync(BotIncomingOperation incoming, string? errorCode, CancellationToken cancellationToken) {
        bool russian = BotMenu.IsRussian(incoming.Language);
        string message = russian
            ? "Не удалось добавить еду по фото. Откройте дневник, чтобы проверить доступ к распознаванию или добавить еду вручную."
            : "Could not add food from this photo. Open your diary to check recognition access or add food manually.";
        if (errorCode is "Ai.ConsentRequired" or "Ai.QuotaExceeded" or BotRecognitionFailure.PremiumRequired) {
            message = BotRecognitionFailure.Format(errorCode, russian);
        }
        if (string.Equals(incoming.Kind, "meal-undo", StringComparison.Ordinal)) {
            message = incoming.Language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true
                ? "Не удалось отменить запись. Откройте дневник, чтобы проверить её состояние."
                : "Could not undo this entry. Open your diary to check its state.";
        }
        if (string.Equals(incoming.Kind, "water", StringComparison.Ordinal)) {
            message = incoming.Language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true
                ? "Не удалось добавить воду. Откройте дневник, чтобы проверить запись."
                : "Could not add water. Open your diary to check the entry.";
        }
        if (string.Equals(incoming.Kind, "statistics", StringComparison.Ordinal)) {
            message = incoming.Language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true
                ? "Не удалось получить статистику. Попробуйте ещё раз или откройте дневник."
                : "Could not load statistics. Try again or open your diary.";
        }
        if (string.Equals(errorCode, "OperationStateInvalid", StringComparison.Ordinal)) {
            message = BotMenu.IsRussian(incoming.Language)
                ? "Не удалось продолжить обработку. Проверьте дневник перед повторной отправкой: запись могла уже сохраниться."
                : "Processing could not continue. Check your diary before sending again: the entry may already have been saved.";
        }
        InlineKeyboardMarkup? markup = errorCode is BotRecognitionFailure.PremiumRequired
            ? BotMenu.Open(options.Value, incoming.Language, BotMenu.MealPath, russian ? "Добавить вручную" : "Add manually")
            : null;
        return SendTerminalNoticeAsync(incoming.ChatId, message, markup, preview: null, cancellationToken);
    }

    private async Task<BotPhotoCheckpoint> AdvanceOperationAsync(BotDiaryClient diary, string token, BotOperationId operationId,
        BotIncomingOperation incoming, BotPhotoCheckpoint checkpoint, CancellationToken cancellationToken) {
        if (string.Equals(incoming.Kind, "statistics", StringComparison.Ordinal)) {
            BotDiaryStatistics statistics = await diary.GetStatisticsAsync(token,
                incoming.PeriodDays ?? throw new InvalidDataException("Missing statistics period."), cancellationToken);
            return checkpoint with { Stage = "statistics-ready", Statistics = statistics };
        }
        if (string.Equals(incoming.Kind, "water", StringComparison.Ordinal)) {
            BotHydrationReceipt receipt = await diary.SaveWaterAsync(token, operationId,
                incoming.OccurredAtUtc ?? throw new InvalidDataException("Missing water timestamp."),
                incoming.AmountMl ?? throw new InvalidDataException("Missing water amount."), cancellationToken);
            return checkpoint with { Stage = "water-saved", WaterEntryId = receipt.EntryId.Value };
        }
        if (string.Equals(incoming.Kind, "meal-undo", StringComparison.Ordinal)) {
            string result = await diary.UndoRecognizedMealAsync(token,
                new BotOperationId(incoming.MealOperationId ?? throw new InvalidDataException("Missing meal operation ID.")), cancellationToken);
            return checkpoint with { Stage = "undo-complete", ErrorCode = result };
        }
        return await AdvanceImageAsync(diary, token, operationId, incoming, checkpoint, cancellationToken);
    }

    private async Task<BotPhotoCheckpoint> AdvanceImageAsync(BotDiaryClient diary, string token, BotOperationId operationId,
        BotIncomingOperation incoming, BotPhotoCheckpoint checkpoint, CancellationToken cancellationToken) {
        if (!checkpoint.PhotoPremiumApproved && checkpoint.Stage is "received" or "upload" or "image-ready") {
            if (!await diary.HasPremiumAsync(token, cancellationToken)) {
                return checkpoint with { Stage = "failed", Upload = null, ErrorCode = BotRecognitionFailure.PremiumRequired };
            }
            checkpoint = checkpoint with { PhotoPremiumApproved = true };
            if (checkpoint.Stage is "image-ready") {
                // Older operations can resume here. Persist access before submitting an AI job that may outlive this attempt.
                return checkpoint;
            }
        }
        var downloader = new TelegramImageDownloader(bot);
        switch (checkpoint.Stage) {
            case "recognition-ready": {
                    BotRecognizedMeal result = await diary.SaveRecognizedMealAsync(token,
                        new BotRecognitionId(checkpoint.RecognitionId ?? throw new InvalidDataException("Missing recognition ID.")),
                        incoming.OccurredAtUtc ?? throw new InvalidDataException("Missing original message time."), cancellationToken);
                    return checkpoint with { Stage = "meal-saved", SavedMeal = result };
                }
            case "received": {
                    byte[] content = await downloader.DownloadAsync(Image(incoming), cancellationToken);
                    BotImageUpload upload = await diary.RequestUploadAsync(token, operationId, checkpoint.UploadAttempt,
                        incoming.ContentType!, content.Length, cancellationToken);
                    return checkpoint with { Stage = "upload", Upload = upload, ImageAssetId = upload.AssetId.Value };
                }
            case "upload": {
                    BotImageUpload upload = checkpoint.Upload ?? throw new InvalidDataException("Missing upload checkpoint.");
                    if (upload.ExpiresAtUtc <= timeProvider.GetUtcNow().UtcDateTime) {
                        return new BotPhotoCheckpoint(UploadAttempt: checked(checkpoint.UploadAttempt + 1),
                            PhotoPremiumApproved: checkpoint.PhotoPremiumApproved, ProgressMessageId: checkpoint.ProgressMessageId);
                    }
                    byte[] content = await downloader.DownloadAsync(Image(incoming), cancellationToken);
                    await diary.UploadAsync(upload, incoming.ContentType!, content, cancellationToken);
                    await diary.ConfirmUploadAsync(token, upload.AssetId, cancellationToken);
                    return checkpoint with { Stage = "image-ready", Upload = null };
                }
            case "image-ready": {
                    var imageId = new BotImageAssetId(checkpoint.ImageAssetId ?? throw new InvalidDataException("Missing image checkpoint."));
                    BotRecognitionJob job = await diary.StartRecognitionAsync(token, operationId, imageId, incoming.Caption, cancellationToken);
                    if (job.Id.Value != operationId.Value || job.ImageAssetId != imageId) {
                        throw new InvalidDataException("Recognition identity mismatch.");
                    }
                    return checkpoint with { Stage = "recognizing", RecognitionId = job.Id.Value };
                }
        }
        // The state reader rejects unknown stages before processing; the remaining active photo stage is recognizing.
        var jobId = new BotRecognitionId(checkpoint.RecognitionId ?? throw new InvalidDataException("Missing recognition checkpoint."));
        BotRecognitionJob recognition = await diary.GetRecognitionAsync(token, jobId, cancellationToken);
        if (recognition.Id != jobId || recognition.ImageAssetId.Value != checkpoint.ImageAssetId) {
            throw new InvalidDataException("Recognition identity mismatch.");
        }
        return recognition.Status switch {
            "Succeeded" when recognition.NutritionErrorCode is null => checkpoint with { Stage = "recognition-ready", Nutrition = recognition.Nutrition },
            "Succeeded" or "Failed" => checkpoint with { Stage = "failed", ErrorCode = recognition.ErrorCode ?? recognition.NutritionErrorCode ?? "RecognitionFailed" },
            _ => checkpoint,
        };
    }

    private Task<bool> SendCompletionAsync(BotIncomingOperation incoming, BotPhotoCheckpoint checkpoint, CancellationToken cancellationToken) {
        bool russian = incoming.Language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;
        InlineKeyboardMarkup? keyboard = null;
        ParseMode parseMode = default;
        string message;
        if (string.Equals(checkpoint.Stage, "statistics-ready", StringComparison.Ordinal)) {
            message = BotStatisticsFormatter.Format(checkpoint.Statistics ?? throw new InvalidDataException("Missing statistics checkpoint."), russian);
        } else if (string.Equals(checkpoint.Stage, "water-saved", StringComparison.Ordinal)) {
            message = string.Create(CultureInfo.InvariantCulture,
                $"{(russian ? "Вода добавлена" : "Water added")}: {incoming.AmountMl} {(russian ? "мл" : "ml")}.");
        } else if (string.Equals(checkpoint.Stage, "meal-saved", StringComparison.Ordinal)) {
            BotRecognizedMeal meal = checkpoint.SavedMeal ?? throw new InvalidDataException("Missing saved meal receipt.");
            message = russian ? "Приём пищи сохранён в дневнике." : "Meal saved to your diary.";
            if (!meal.Undone && checkpoint.Nutrition is not null) {
                message += "\n\n" + checkpoint.Nutrition.Format(russian);
                parseMode = ParseMode.Html;
            }
            if (meal.Undone) {
                message = russian ? "Эта запись уже отменена или удалена из дневника." : "This entry has already been undone or deleted from your diary.";
            }
            keyboard = BotMealActions.Create(meal, options.Value.WebAppUrl, russian, timeProvider.GetUtcNow().UtcDateTime);
        } else {
            message = checkpoint.ErrorCode switch {
                "Undone" or "AlreadyUndone" or "AlreadyDeleted" => russian ? "Запись отменена." : "Entry undone.",
                "Meal.RecognitionUndoChanged" => russian ? "Еда уже отредактирована. Удалить её можно в дневнике." : "This meal was edited. You can delete it in your diary.",
                "Meal.RecognitionUndoExpired" => russian ? "Срок отмены истёк. Удалить еду можно в дневнике." : "The undo period expired. You can delete the meal in your diary.",
                _ => russian ? "Запись недоступна для отмены." : "This entry is not available to undo.",
            };
        }
        LinkPreviewOptions? preview = checkpoint.Stage is "meal-saved" ? new LinkPreviewOptions { IsDisabled = true } : null;
        return SendTerminalNoticeAsync(incoming.ChatId, message, keyboard, preview, cancellationToken, parseMode);
    }

    private async Task<bool> SendTerminalNoticeAsync(long chatId, string message, InlineKeyboardMarkup? keyboard, LinkPreviewOptions? preview,
        CancellationToken cancellationToken, ParseMode parseMode = default) {
        try {
            await bot.SendMessage(chatId, message, parseMode: parseMode, replyMarkup: keyboard, linkPreviewOptions: preview, cancellationToken: cancellationToken);
            return true;
        } catch (ApiRequestException error) when (error.ErrorCode == 403) {
            // Business completion remains durable even if Telegram delivery is blocked.
            return false;
        }
    }

    private static TelegramImageSelection Image(BotIncomingOperation incoming) =>
        new(incoming.FileId ?? throw new InvalidDataException("Missing Telegram file ID."),
            incoming.ContentType ?? throw new InvalidDataException("Missing Telegram content type."), DeclaredSizeBytes: null);
}
