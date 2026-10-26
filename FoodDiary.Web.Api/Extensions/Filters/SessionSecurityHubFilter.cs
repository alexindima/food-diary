using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Web.Api.Extensions.Filters;

/// <summary>Rechecks server-push connections even when the client invokes no hub methods.</summary>
public sealed class SessionSecurityHubFilter(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<SessionSecurityHubFilter> logger) : IHubFilter {
    private static readonly object MonitorKey = new();
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next) {
        await next(context).ConfigureAwait(false);
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.Context.ConnectionAborted);
        context.Context.Items[MonitorKey] = new ConnectionMonitor(cancellation, MonitorAsync(context.Context, cancellation.Token));
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next) {
        if (context.Context.Items.Remove(MonitorKey, out object? value) && value is ConnectionMonitor monitor) {
            await monitor.Cancellation.CancelAsync().ConfigureAwait(false);
            await monitor.Completion.ConfigureAwait(false);
            monitor.Cancellation.Dispose();
        }
        await next(context, exception).ConfigureAwait(false);
    }

    private async Task MonitorAsync(HubCallerContext caller, CancellationToken cancellationToken) {
        try {
            using var timer = new PeriodicTimer(CheckInterval, clock);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false)) {
                using var timeout = new CancellationTokenSource(CheckTimeout, clock);
                using var check = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                AsyncServiceScope scope = scopes.CreateAsyncScope();
                await using (scope.ConfigureAwait(false)) {
                    bool valid = await AccessTokenSecurityStateValidator.IsCurrentAsync(caller.User,
                        scope.ServiceProvider.GetRequiredService<IUserAccessTokenSecurityReader>(),
                        scope.ServiceProvider.GetRequiredService<IUserAccessTokenSessionReader>(), check.Token).ConfigureAwait(false);
                    if (!valid) {
                        caller.Abort();
                        return;
                    }
                }
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Disconnect ends the monitor and releases its timer and independent persistence scope.
        } catch (Exception) {
            logger.LogWarning("Closing a real-time connection because its current security state could not be verified.");
            caller.Abort();
        }
    }

    private sealed record ConnectionMonitor(CancellationTokenSource Cancellation, Task Completion);
}
