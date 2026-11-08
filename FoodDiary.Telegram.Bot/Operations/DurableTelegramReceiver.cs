using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class DurableTelegramReceiver(ITelegramBotClient botClient,
    Func<Update, CancellationToken, Task> persist, TimeProvider timeProvider, ILogger logger) {
    internal int? NextOffset { get; private set; }

    internal async Task RunAsync(CancellationToken cancellationToken) {
        int retrySeconds = 2;
        var telemetry = new TelegramLoopTelemetry(TelegramLoopKind.Intake, timeProvider, logger);
        while (!cancellationToken.IsCancellationRequested) {
            long started = timeProvider.GetTimestamp();
            try {
                Update[] updates = await botClient.GetUpdates(offset: NextOffset, timeout: 30,
                    allowedUpdates: [UpdateType.Message, UpdateType.CallbackQuery, UpdateType.PreCheckoutQuery, UpdateType.Subscription], cancellationToken: cancellationToken);
                await ProcessBatchAsync(updates, cancellationToken);
                telemetry.Observe(success: true, timeProvider.GetElapsedTime(started));
                retrySeconds = 2;
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                return;
            } catch (Exception error) {
                telemetry.Observe(success: false, timeProvider.GetElapsedTime(started));
                logger.LogWarning("Telegram intake paused after {ErrorType}; the unpersisted update remains unacknowledged.", error.GetType().Name);
                try {
                    await Task.Delay(TimeSpan.FromSeconds(retrySeconds), timeProvider, cancellationToken);
                } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return;
                }
                retrySeconds = Math.Min(retrySeconds * 2, 30);
            }
        }
    }

    internal async Task ProcessBatchAsync(IEnumerable<Update> updates, CancellationToken cancellationToken) {
        foreach (Update update in updates.OrderBy(item => item.Id)) {
            if (NextOffset.HasValue && update.Id < NextOffset.Value) {
                continue;
            }
            await persist(update, cancellationToken);
            NextOffset = checked(update.Id + 1);
        }
    }
}
