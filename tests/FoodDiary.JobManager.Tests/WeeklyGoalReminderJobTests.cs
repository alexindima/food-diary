using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Services;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalReminderJobTests {
    [Fact]
    public async Task Execute_WhenDisabled_RecordsSuccessWithoutReadingCandidates() {
        var repository = new RecordingRepository();
        JobExecutionStateTracker tracker = new();
        WeeklyGoalReminderJob job = CreateJob(repository, enabled: false, tracker);

        await job.Execute(CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(0, repository.CallCount),
            () => Assert.Equal(0, tracker.GetSnapshot("weekly_goals.reminders")?.ConsecutiveFailures));
    }

    [Fact]
    public async Task Execute_WhenEnabled_ProcessesAndRecordsSuccess() {
        var repository = new RecordingRepository();
        JobExecutionStateTracker tracker = new();
        WeeklyGoalReminderJob job = CreateJob(repository, enabled: true, tracker);

        await job.Execute(CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(1, repository.CallCount),
            () => Assert.Equal(0, tracker.GetSnapshot("weekly_goals.reminders")?.ConsecutiveFailures));
    }

    [Fact]
    public async Task Execute_WhenCanceled_RecordsCanceledAndRethrows() {
        var repository = new RecordingRepository(exception: new OperationCanceledException());
        JobExecutionStateTracker tracker = new();
        WeeklyGoalReminderJob job = CreateJob(repository, enabled: true, tracker);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => job.Execute(cancellation.Token));

        Assert.Equal(0, tracker.GetSnapshot("weekly_goals.reminders")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task Execute_WhenProcessorFails_RecordsFailureAndRethrows() {
        var repository = new RecordingRepository(exception: new InvalidOperationException("failure"));
        JobExecutionStateTracker tracker = new();
        WeeklyGoalReminderJob job = CreateJob(repository, enabled: true, tracker);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(CancellationToken.None));

        Assert.Equal(1, tracker.GetSnapshot("weekly_goals.reminders")?.ConsecutiveFailures);
    }

    private static WeeklyGoalReminderJob CreateJob(
        IWeeklyGoalRepository repository,
        bool enabled,
        JobExecutionStateTracker tracker) {
        var processor = new WeeklyGoalReminderProcessor(repository, null!, null!, TimeProvider.System);
        return new WeeklyGoalReminderJob(
            processor,
            Options.Create(new WeeklyGoalReminderOptions { Enabled = enabled }),
            new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<WeeklyGoalReminderJob>.Instance);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingRepository(Exception? exception = null) : IWeeklyGoalRepository {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<WeeklyGoal>> GetReminderCandidatesAsync(
            DateTime earliestWeekStartUtc,
            DateTime latestWeekStartUtc,
            int limit,
            CancellationToken cancellationToken = default) {
            CallCount++;
            if (exception is not null) {
                throw exception;
            }

            return Task.FromResult<IReadOnlyList<WeeklyGoal>>([]);
        }

        public Task<WeeklyGoal?> GetAsync(UserId userId, DateTime weekStartUtc, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<WeeklyGoal?>(null);

        public Task AddAsync(WeeklyGoal goal, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
