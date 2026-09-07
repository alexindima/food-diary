using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.BugTriage.Infrastructure.Workers;

public sealed class BugMailImportWorker(ImportBugReports importer, IOptions<BugTriageOptions> options,
    ILogger<BugMailImportWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(options.Value.ImportTimeout);
                await importer.RunAsync(options.Value.ContentRetention, timeout.Token).ConfigureAwait(false);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception) {
                // Provider exception messages may contain addresses, URLs, or credentials.
                logger.LogWarning("Bug mail import failed; the next polling cycle will retry.");
            }
            await Task.Delay(options.Value.PollInterval, stoppingToken).ConfigureAwait(false);
        }
    }
}
