using FoodDiary.Application.Admin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Integrations.MailInbox;

internal sealed class BugAcknowledgementWorker(IServiceScopeFactory scopes, IOptions<BugAcknowledgementOptions> options,
    ILogger<BugAcknowledgementWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        BugAcknowledgementOptions settings = options.Value;
        if (!settings.Enabled) {
            return;
        }
        using var timer = new PeriodicTimer(settings.PollInterval);
        do {
            try {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(TimeSpan.FromMinutes(10));
                AsyncServiceScope scope = scopes.CreateAsyncScope();
                await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable scopeLease = scope.ConfigureAwait(false);
                await scope.ServiceProvider.GetRequiredService<BugAcknowledgementService>()
                    .RunAsync(settings.StartAtUtc ?? DateTimeOffset.UnixEpoch, timeout.Token).ConfigureAwait(false);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception ex) {
                logger.LogWarning("Bug acknowledgement scan failed. ErrorType={ErrorType}", ex.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
