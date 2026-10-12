using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.BugTriage.Infrastructure.Workers;

public sealed class AuditRetentionWorker(IAuditRunStore store, TimeProvider timeProvider, IOptions<BugTriageOptions> options,
    ILogger<AuditRetentionWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(options.Value.ImportTimeout);
                await store.PurgeAsync(timeProvider.GetUtcNow(), timeout.Token).ConfigureAwait(false);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception) {
                logger.LogWarning("Audit content cleanup failed; the next polling cycle will retry.");
            }
            await Task.Delay(options.Value.PollInterval, timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }
}
