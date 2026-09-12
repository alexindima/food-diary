using System.Net;
using System.Text.Json;
using FoodDiary.Telegram.Bot.Images;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class TelegramOperationWorker(IHttpClientFactory clients, IOptions<TelegramBotOptions> options,
    ITelegramBotClient bot, TimeProvider timeProvider, ILogger<TelegramOperationWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!options.Value.OperationsEnabled) {
            return;
        }
        while (!stoppingToken.IsCancellationRequested) {
            try {
                using HttpClient http = clients.CreateClient(BotOperationClient.ClientName);
                var operations = new BotOperationClient(http, options);
                IReadOnlyList<Guid> ready = await operations.ListReadyAsync(stoppingToken).ConfigureAwait(false);
                foreach (Guid id in ready) {
                    try {
                        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        deadline.CancelAfter(TimeSpan.FromSeconds(90));
                        await ProcessAsync(operations, id, deadline.Token).ConfigureAwait(false);
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
            await Task.Delay(TimeSpan.FromSeconds(5), timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    internal async Task ProcessAsync(BotOperationClient operations, Guid id, CancellationToken cancellationToken) {
        BotOperationLease? lease = await operations.AcquireAsync(id, cancellationToken).ConfigureAwait(false);
        if (lease is null) {
            return;
        }
        BotIncomingOperation incoming;
        try {
            incoming = BotOperationStateReader.ReadIncoming(lease.Payload);
        } catch (Exception error) when (error is JsonException or InvalidDataException) {
            logger.LogWarning("Telegram operation {OperationId} rejected due to invalid input state.", id);
            await operations.CheckpointAsync(lease, "{\"ErrorCode\":\"OperationInputInvalid\"}", completed: true,
                timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
            return;
        }
        BotPhotoCheckpoint checkpoint;
        try {
            checkpoint = BotOperationStateReader.ReadCheckpoint(lease.Checkpoint, incoming.Kind);
        } catch (Exception error) when (error is JsonException or InvalidDataException) {
            checkpoint = new BotPhotoCheckpoint(Stage: "failed", ErrorCode: "OperationStateInvalid");
        }
        if (string.Equals(checkpoint.Stage, "failed", StringComparison.Ordinal)) {
            await SendFailureAsync(incoming, checkpoint.ErrorCode, cancellationToken).ConfigureAwait(false);
            await operations.CheckpointAsync(lease, JsonSerializer.Serialize(checkpoint), completed: true,
                timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (checkpoint.Stage is "meal-saved" or "undo-complete" or "water-saved" or "statistics-ready") {
            await SendCompletionAsync(incoming, checkpoint, cancellationToken).ConfigureAwait(false);
            await operations.CheckpointAsync(lease, JsonSerializer.Serialize(checkpoint), completed: true,
                timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
            return;
        }
        using HttpClient http = clients.CreateClient(BotDiaryClient.ClientName);
        var diary = new BotDiaryClient(http, options);
        try {
            string token = await diary.AuthenticateAsync(incoming.TelegramUserId, lease, cancellationToken).ConfigureAwait(false);
            if (string.Equals(incoming.Kind, "water", StringComparison.Ordinal)) {
                if (lease.CreatedAtUtc == default || lease.CreatedAtUtc.Kind != DateTimeKind.Utc) {
                    throw new InvalidDataException("A durable water operation timestamp is required.");
                }
                incoming = incoming with { OccurredAtUtc = lease.CreatedAtUtc };
            }
            checkpoint = await AdvanceOperationAsync(diary, token, lease.OperationId, incoming, checkpoint, cancellationToken).ConfigureAwait(false);
        } catch (BotRecognitionAccessException error) {
            checkpoint = checkpoint with { Stage = "failed", Upload = null, ErrorCode = error.Code };
        } catch (HttpRequestException error) when (error.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict) {
            checkpoint = checkpoint with { Stage = "failed", Upload = null, ErrorCode = error.StatusCode.Value.ToString() };
        } catch (InvalidDataException) {
            checkpoint = checkpoint with { Stage = "failed", Upload = null, ErrorCode = "InvalidImageOrIdentity" };
        }
        await operations.CheckpointAsync(lease, JsonSerializer.Serialize(checkpoint), completed: false,
            timeProvider.GetUtcNow().UtcDateTime.AddSeconds(5), cancellationToken).ConfigureAwait(false);
    }

    private async Task SendFailureAsync(BotIncomingOperation incoming, string? errorCode, CancellationToken cancellationToken) {
        string message = incoming.Language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true
            ? "Не удалось добавить еду по фото. Откройте дневник, чтобы проверить доступ к распознаванию или добавить еду вручную."
            : "Could not add food from this photo. Open your diary to check recognition access or add food manually.";
        if (errorCode is "Ai.ConsentRequired" or "Ai.QuotaExceeded") {
            message = BotRecognitionFailure.Format(errorCode, BotMenu.IsRussian(incoming.Language));
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
        try {
            await bot.SendMessage(incoming.ChatId, message, cancellationToken: cancellationToken).ConfigureAwait(false);
        } catch (ApiRequestException error) when (error.ErrorCode == 403) {
            // A blocked bot cannot deliver the terminal notice; the operation must still stop.
        }
    }

    private async Task<BotPhotoCheckpoint> AdvanceOperationAsync(BotDiaryClient diary, string token, Guid operationId,
        BotIncomingOperation incoming, BotPhotoCheckpoint checkpoint, CancellationToken cancellationToken) {
        if (string.Equals(incoming.Kind, "statistics", StringComparison.Ordinal)) {
            BotDiaryStatistics statistics = await diary.GetStatisticsAsync(token,
                incoming.PeriodDays ?? throw new InvalidDataException("Missing statistics period."), cancellationToken).ConfigureAwait(false);
            return checkpoint with { Stage = "statistics-ready", Statistics = statistics };
        }
        if (string.Equals(incoming.Kind, "water", StringComparison.Ordinal)) {
            BotHydrationReceipt receipt = await diary.SaveWaterAsync(token, operationId,
                incoming.OccurredAtUtc ?? throw new InvalidDataException("Missing water timestamp."),
                incoming.AmountMl ?? throw new InvalidDataException("Missing water amount."), cancellationToken).ConfigureAwait(false);
            return checkpoint with { Stage = "water-saved", WaterEntryId = receipt.EntryId };
        }
        if (string.Equals(incoming.Kind, "meal-undo", StringComparison.Ordinal)) {
            string result = await diary.UndoRecognizedMealAsync(token,
                incoming.MealOperationId ?? throw new InvalidDataException("Missing meal operation ID."), cancellationToken).ConfigureAwait(false);
            return checkpoint with { Stage = "undo-complete", ErrorCode = result };
        }
        return await AdvanceImageAsync(diary, token, operationId, incoming, checkpoint, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BotPhotoCheckpoint> AdvanceImageAsync(BotDiaryClient diary, string token, Guid operationId,
        BotIncomingOperation incoming, BotPhotoCheckpoint checkpoint, CancellationToken cancellationToken) {
        var downloader = new TelegramImageDownloader(bot);
        switch (checkpoint.Stage) {
            case "recognition-ready": {
                BotRecognizedMeal result = await diary.SaveRecognizedMealAsync(token,
                    checkpoint.RecognitionId ?? throw new InvalidDataException("Missing recognition ID."),
                    incoming.OccurredAtUtc ?? throw new InvalidDataException("Missing original message time."), cancellationToken).ConfigureAwait(false);
                return checkpoint with { Stage = "meal-saved", SavedMeal = result };
            }
            case "received": {
                byte[] content = await downloader.DownloadAsync(Image(incoming), cancellationToken).ConfigureAwait(false);
                BotImageUpload upload = await diary.RequestUploadAsync(token, operationId, checkpoint.UploadAttempt,
                    incoming.ContentType!, content.Length, cancellationToken).ConfigureAwait(false);
                return checkpoint with { Stage = "upload", Upload = upload, ImageAssetId = upload.AssetId };
            }
            case "upload": {
                BotImageUpload upload = checkpoint.Upload ?? throw new InvalidDataException("Missing upload checkpoint.");
                if (upload.ExpiresAtUtc <= timeProvider.GetUtcNow().UtcDateTime) {
                    return new BotPhotoCheckpoint(UploadAttempt: checked(checkpoint.UploadAttempt + 1));
                }
                byte[] content = await downloader.DownloadAsync(Image(incoming), cancellationToken).ConfigureAwait(false);
                await diary.UploadAsync(upload, incoming.ContentType!, content, cancellationToken).ConfigureAwait(false);
                await diary.ConfirmUploadAsync(token, upload.AssetId, cancellationToken).ConfigureAwait(false);
                return checkpoint with { Stage = "image-ready", Upload = null };
            }
            case "image-ready": {
                Guid imageId = checkpoint.ImageAssetId ?? throw new InvalidDataException("Missing image checkpoint.");
                BotRecognitionJob job = await diary.StartRecognitionAsync(token, operationId, imageId, incoming.Caption, cancellationToken).ConfigureAwait(false);
                if (job.Id != operationId || job.ImageAssetId != imageId) {
                    throw new InvalidDataException("Recognition identity mismatch.");
                }
                return checkpoint with { Stage = "recognizing", RecognitionId = job.Id };
            }
            case "recognizing": {
                Guid jobId = checkpoint.RecognitionId ?? throw new InvalidDataException("Missing recognition checkpoint.");
                BotRecognitionJob job = await diary.GetRecognitionAsync(token, jobId, cancellationToken).ConfigureAwait(false);
                if (job.Id != jobId || job.ImageAssetId != checkpoint.ImageAssetId) {
                    throw new InvalidDataException("Recognition identity mismatch.");
                }
                return job.Status switch {
                    "Succeeded" when job.NutritionErrorCode is null => checkpoint with { Stage = "recognition-ready", Nutrition = job.Nutrition },
                    "Succeeded" or "Failed" => checkpoint with { Stage = "failed", ErrorCode = job.ErrorCode ?? job.NutritionErrorCode ?? "RecognitionFailed" },
                    _ => checkpoint,
                };
            }
            default:
                return checkpoint;
        }
    }

    private async Task SendCompletionAsync(BotIncomingOperation incoming, BotPhotoCheckpoint checkpoint, CancellationToken cancellationToken) {
        bool russian = incoming.Language?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;
        InlineKeyboardMarkup? keyboard = null;
        string message;
        if (string.Equals(checkpoint.Stage, "statistics-ready", StringComparison.Ordinal)) {
            message = BotStatisticsFormatter.Format(checkpoint.Statistics ?? throw new InvalidDataException("Missing statistics checkpoint."), russian);
        } else if (string.Equals(checkpoint.Stage, "water-saved", StringComparison.Ordinal)) {
            message = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"{(russian ? "Вода добавлена" : "Water added")}: {incoming.AmountMl} {(russian ? "мл" : "ml")}.");
        } else if (string.Equals(checkpoint.Stage, "meal-saved", StringComparison.Ordinal)) {
            BotRecognizedMeal meal = checkpoint.SavedMeal ?? throw new InvalidDataException("Missing saved meal receipt.");
            message = russian ? "Еда добавлена в дневник." : "Food added to your diary.";
            if (!meal.Undone && checkpoint.Nutrition is not null) {
                message += "\n\n" + checkpoint.Nutrition.Format(russian);
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
        try {
            await bot.SendMessage(incoming.ChatId, message, replyMarkup: keyboard, cancellationToken: cancellationToken).ConfigureAwait(false);
        } catch (ApiRequestException error) when (error.ErrorCode == 403) {
            // Business completion remains durable even if Telegram delivery is blocked.
        }
    }

    private static TelegramImageSelection Image(BotIncomingOperation incoming) =>
        new(incoming.FileId ?? throw new InvalidDataException("Missing Telegram file ID."),
            incoming.ContentType ?? throw new InvalidDataException("Missing Telegram content type."), DeclaredSizeBytes: null);
}
