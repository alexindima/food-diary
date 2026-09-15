using FoodDiary.Mediator;
using FoodDiary.Testing;
using FoodDiary.Modules.Fasting.Application.Commands.CleanupFastingTelemetry;
using FoodDiary.Modules.Fasting.Contracts.Commands.CleanupFastingTelemetry;
using FoodDiary.Modules.Fasting.Application.Abstractions.Common;

namespace FoodDiary.Modules.Fasting.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingTelemetryCleanupServiceTests {
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CleanupAsync_WhenBatchSizeIsNotPositive_RejectsBeforeDeleting(int batchSize) {
        IFastingTelemetryEventWriteRepository repository = Substitute.For<IFastingTelemetryEventWriteRepository>();
        var handler = new CleanupFastingTelemetryCommandHandler(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.Handle(
            new CleanupFastingTelemetryCommand(DateTime.UtcNow, batchSize), CancellationToken.None));

        await repository.DidNotReceiveWithAnyArgs().DeleteOlderThanAsync(default, default, default);
    }

    [Fact]
    public async Task CleanupAsync_DeletesFullBatchesUntilRepositoryReturnsPartialBatch() {
        IFastingTelemetryEventWriteRepository repository = Substitute.For<IFastingTelemetryEventWriteRepository>();
        var cutoffUtc = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc);
        repository
            .DeleteOlderThanAsync(cutoffUtc, 2, Arg.Any<CancellationToken>())
            .Returns(2, 2, 1);
        ISender service = RequestTestSender.Create(new CleanupFastingTelemetryCommandHandler(repository));

        int deletedCount = await service.Send(new CleanupFastingTelemetryCommand(cutoffUtc, 2), CancellationToken.None);

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
        ISender service = RequestTestSender.Create(new CleanupFastingTelemetryCommandHandler(repository));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.Send(new CleanupFastingTelemetryCommand(DateTime.UtcNow, 1), cancellationTokenSource.Token));

        await repository.Received(1).DeleteOlderThanAsync(
            Arg.Any<DateTime>(),
            1,
            cancellationTokenSource.Token);
    }
}
