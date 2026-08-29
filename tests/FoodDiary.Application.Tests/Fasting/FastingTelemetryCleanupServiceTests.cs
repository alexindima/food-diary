using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Modules.Fasting.Application.Services;

namespace FoodDiary.Application.Tests.Fasting;

[ExcludeFromCodeCoverage]
public sealed class FastingTelemetryCleanupServiceTests {
    [Fact]
    public async Task CleanupAsync_DeletesFullBatchesUntilRepositoryReturnsPartialBatch() {
        IFastingTelemetryEventWriteRepository repository = Substitute.For<IFastingTelemetryEventWriteRepository>();
        var cutoffUtc = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc);
        repository
            .DeleteOlderThanAsync(cutoffUtc, 2, Arg.Any<CancellationToken>())
            .Returns(2, 2, 1);
        var service = new FastingTelemetryCleanupService(repository);

        int deletedCount = await service.CleanupAsync(cutoffUtc, 2, CancellationToken.None);

        Assert.Equal(5, deletedCount);
        await repository.Received(3).DeleteOlderThanAsync(cutoffUtc, 2, CancellationToken.None);
    }

    [Fact]
    public async Task CleanupAsync_WhenCanceledBetweenBatches_StopsBeforeNextDelete() {
        using var cancellationTokenSource = new CancellationTokenSource();
        IFastingTelemetryEventWriteRepository repository = Substitute.For<IFastingTelemetryEventWriteRepository>();
        repository
            .DeleteOlderThanAsync(Arg.Any<DateTime>(), 1, cancellationTokenSource.Token)
            .Returns(_ => {
                cancellationTokenSource.Cancel();
                return 1;
            });
        var service = new FastingTelemetryCleanupService(repository);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.CleanupAsync(DateTime.UtcNow, 1, cancellationTokenSource.Token));

        await repository.Received(1).DeleteOlderThanAsync(
            Arg.Any<DateTime>(),
            1,
            cancellationTokenSource.Token);
    }
}
