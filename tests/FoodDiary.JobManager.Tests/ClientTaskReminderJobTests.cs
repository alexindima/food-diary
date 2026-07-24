using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class ClientTaskReminderJobTests {
    [Fact]
    public async Task Execute_WhenDisabled_RecordsSuccessWithoutProcessing() {
        var repository = new StubClientTaskRepository();
        JobExecutionStateTracker tracker = new();
        ClientTaskReminderJob job = CreateJob(repository, tracker, enabled: false);

        await job.Execute();

        Assert.False(repository.WasCalled);
        Assert.NotNull(tracker.GetSnapshot("dietologist.client_task_reminders")?.LastSucceededAtUtc);
    }

    [Fact]
    public async Task Execute_WhenEnabled_ProcessesAndRecordsSuccess() {
        var repository = new StubClientTaskRepository();
        JobExecutionStateTracker tracker = new();
        ClientTaskReminderJob job = CreateJob(repository, tracker, enabled: true);

        await job.Execute();

        Assert.NotNull(tracker.GetSnapshot("dietologist.client_task_reminders")?.LastSucceededAtUtc);
    }

    [Fact]
    public async Task Execute_WhenCanceled_RecordsCancellationAndRethrows() {
        var repository = new StubClientTaskRepository(new OperationCanceledException());
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        ClientTaskReminderJob job = CreateJob(repository, new JobExecutionStateTracker(), enabled: true);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => job.Execute(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task Execute_WhenProcessorFails_RecordsFailureAndRethrows() {
        var repository = new StubClientTaskRepository(new InvalidOperationException("failed"));
        JobExecutionStateTracker tracker = new();
        ClientTaskReminderJob job = CreateJob(repository, tracker, enabled: true);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => job.Execute());

        Assert.Equal("failed", exception.Message);
        Assert.NotNull(tracker.GetSnapshot("dietologist.client_task_reminders")?.LastFailedAtUtc);
    }

    private static ClientTaskReminderJob CreateJob(
        IClientTaskRepository repository,
        JobExecutionStateTracker tracker,
        bool enabled) {
        var processor = new ClientTaskDueReminderProcessor(
            repository,
            new NullNotificationWriter(),
            TimeProvider.System);
        return new ClientTaskReminderJob(
            processor,
            Options.Create(new ClientTaskReminderOptions { Enabled = enabled }),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<ClientTaskReminderJob>.Instance);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubClientTaskRepository(Exception? exception = null) : IClientTaskRepository {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<ClientTask>> GetDueForReminderAsync(
            DateTime utcNow,
            DateTime dueBeforeUtc,
            int limit,
            CancellationToken cancellationToken = default) {
            WasCalled = true;
            return exception is null
                ? Task.FromResult<IReadOnlyList<ClientTask>>([])
                : Task.FromException<IReadOnlyList<ClientTask>>(exception);
        }

        public Task<ClientTask> AddAsync(ClientTask task, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ClientTask?> GetByIdAsync(
            ClientTaskId id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ClientTaskReadModel>> GetByClientAsync(
            UserId clientUserId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ClientTaskReadModel>> GetByDietologistAndClientAsync(
            UserId dietologistUserId,
            UserId clientUserId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullNotificationWriter : INotificationWriter {
        public Task AddAsync(
            Notification notification,
            bool sendWebPush = false,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
