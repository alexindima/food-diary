using System.Runtime.CompilerServices;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Infrastructure.Integrations.MailInbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Admin.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class BugAcknowledgementWorkerTests {
    [Fact]
    public async Task Disabled_DoesNotCreateScope() {
        IServiceScopeFactory scopes = Substitute.For<IServiceScopeFactory>();
        using var worker = new BugAcknowledgementWorker(scopes, Options.Create(new BugAcknowledgementOptions()), NullLogger<BugAcknowledgementWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Empty(scopes.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Enabled_RunsWithConfiguredStartAndStopsOnCancellation(bool explicitStart) {
        var source = new WaitingSource();
        var services = new ServiceCollection();
        services.AddSingleton<IBugAcknowledgementSource>(source);
        services.AddSingleton(Substitute.For<IEmailTemplateAdministrationReadService>());
        services.AddSingleton(Substitute.For<IEmailTransport>());
        services.AddSingleton(Substitute.For<IBugAcknowledgementReceipts>());
        services.AddScoped<BugAcknowledgementService>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        var settings = new BugAcknowledgementOptions { Enabled = true, StartAtUtc = explicitStart ? DateTimeOffset.UnixEpoch.AddDays(1) : null };
        using var worker = new BugAcknowledgementWorker(provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(settings), NullLogger<BugAcknowledgementWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        DateTimeOffset since = await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(settings.StartAtUtc ?? DateTimeOffset.UnixEpoch, since);
    }

    [Fact]
    public async Task SuccessfulScans_RepeatUntilStopped() {
        var source = new CompletingSource();
        IEmailTemplateAdministrationReadService templates = Substitute.For<IEmailTemplateAdministrationReadService>();
        templates.GetTemplatesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<EmailTemplateReadModel>());
        var services = new ServiceCollection();
        services.AddSingleton<IBugAcknowledgementSource>(source);
        services.AddSingleton(templates);
        services.AddSingleton(Substitute.For<IEmailTransport>());
        services.AddSingleton(Substitute.For<IBugAcknowledgementReceipts>());
        services.AddScoped<BugAcknowledgementService>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        var settings = new BugAcknowledgementOptions { Enabled = true, PollInterval = TimeSpan.FromMilliseconds(10) };
        using var worker = new BugAcknowledgementWorker(provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(settings), NullLogger<BugAcknowledgementWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await source.Repeated.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(source.CompletedScans >= 2);
    }

    [Fact]
    public async Task ScanFailure_DoesNotTerminateWorker() {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IEmailTemplateAdministrationReadService templates = Substitute.For<IEmailTemplateAdministrationReadService>();
        templates.GetTemplatesAsync(Arg.Any<CancellationToken>()).Returns(_ => {
            signal.TrySetResult();
            return Task.FromException<IReadOnlyList<EmailTemplateReadModel>>(new InvalidOperationException("failure"));
        });
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IBugAcknowledgementSource>());
        services.AddSingleton(templates);
        services.AddSingleton(Substitute.For<IEmailTransport>());
        services.AddSingleton(Substitute.For<IBugAcknowledgementReceipts>());
        services.AddScoped<BugAcknowledgementService>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        using var worker = new BugAcknowledgementWorker(provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(new BugAcknowledgementOptions { Enabled = true }), NullLogger<BugAcknowledgementWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await signal.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(worker.ExecuteTask!.IsCompleted);
        await worker.StopAsync(CancellationToken.None);
    }

    [ExcludeFromCodeCoverage]
    private sealed class CompletingSource : IBugAcknowledgementSource {
        private int _completedScans;
        public int CompletedScans => _completedScans;
        public TaskCompletionSource Repeated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async IAsyncEnumerable<BugAcknowledgementCandidate> ReadAsync(DateTimeOffset since, [EnumeratorCancellation] CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            if (Interlocked.Increment(ref _completedScans) >= 2) {
                Repeated.TrySetResult();
            }
            yield break;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class WaitingSource : IBugAcknowledgementSource {
        public TaskCompletionSource<DateTimeOffset> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async IAsyncEnumerable<BugAcknowledgementCandidate> ReadAsync(DateTimeOffset since, [EnumeratorCancellation] CancellationToken cancellationToken) {
            Entered.TrySetResult(since);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            yield break;
        }
    }
}
