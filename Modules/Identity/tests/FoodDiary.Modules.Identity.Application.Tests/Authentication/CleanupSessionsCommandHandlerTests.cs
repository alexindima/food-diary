using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CleanupSessions;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupSessions;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class CleanupSessionsCommandHandlerTests {
    [Fact]
    public async Task Handle_LimitsWorkEvenWhenEveryBatchIsFull() {
        IRefreshTokenSessionWriteRepository repository = Substitute.For<IRefreshTokenSessionWriteRepository>();
        DateTime cutoff = DateTime.UtcNow.AddDays(-30);
        repository.DeleteInactiveBeforeAsync(cutoff, 2, Arg.Any<CancellationToken>()).Returns(2);
        var handler = new CleanupSessionsCommandHandler(repository);
        Assert.Equal(6, await handler.Handle(new CleanupSessionsCommand(cutoff, 2, 3), CancellationToken.None));
        await repository.Received(3).DeleteInactiveBeforeAsync(cutoff, 2, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_StopsAfterPartialBatchAndPropagatesCancellation() {
        IRefreshTokenSessionWriteRepository repository = Substitute.For<IRefreshTokenSessionWriteRepository>();
        DateTime cutoff = DateTime.UtcNow.AddDays(-30);
        repository.DeleteInactiveBeforeAsync(cutoff, 2, Arg.Any<CancellationToken>()).Returns(2, 1);
        var handler = new CleanupSessionsCommandHandler(repository);
        Assert.Equal(3, await handler.Handle(new CleanupSessionsCommand(cutoff, 2), CancellationToken.None));
        await repository.Received(2).DeleteInactiveBeforeAsync(cutoff, 2, CancellationToken.None);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(new CleanupSessionsCommand(cutoff, 2), new CancellationToken(canceled: true)));
        Assert.Equal(2, repository.ReceivedCalls().Count());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1001, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Handle_RejectsUnboundedConfiguration(int batchSize, int maxBatches) {
        IRefreshTokenSessionWriteRepository repository = Substitute.For<IRefreshTokenSessionWriteRepository>();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new CleanupSessionsCommandHandler(repository)
            .Handle(new CleanupSessionsCommand(DateTime.UtcNow, batchSize, maxBatches), CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }
}
