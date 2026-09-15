using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Application.Commands.CleanupDeletedUsers;
using FoodDiary.Modules.Users.Contracts.Commands.CleanupDeletedUsers;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CleanupDeletedUsersCommandHandlerTests {
    [Fact]
    public async Task Handle_AdvancesPastFailedBatch_AndStopsOnlyAfterEmptyPage() {
        IUserCleanupService cleanup = Substitute.For<IUserCleanupService>();
        var threshold = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var first = new UserCleanupCursor(threshold.AddDays(-1), new UserId(Guid.NewGuid()));
        var second = new UserCleanupCursor(threshold, new UserId(Guid.NewGuid()));
        var request = new CleanupDeletedUsersCommand(threshold, 10, Guid.NewGuid());
        using var cancellation = new CancellationTokenSource();
        cleanup.CleanupDeletedUsersAsync(threshold, 10, request.ReassignUserId, after: null, cancellation.Token)
            .Returns(new UserCleanupBatch(0, first));
        cleanup.CleanupDeletedUsersAsync(threshold, 10, request.ReassignUserId, first, cancellation.Token)
            .Returns(new UserCleanupBatch(1, second));
        cleanup.CleanupDeletedUsersAsync(threshold, 10, request.ReassignUserId, second, cancellation.Token)
            .Returns(new UserCleanupBatch(0, LastExamined: null));

        int removed = await new CleanupDeletedUsersCommandHandler(cleanup).Handle(request, cancellation.Token);

        Assert.Equal(1, removed);
        await cleanup.Received(3).CleanupDeletedUsersAsync(threshold, 10, request.ReassignUserId,
            Arg.Any<UserCleanupCursor?>(), cancellation.Token);
    }

    [Fact]
    public async Task Handle_WhenCancelledDuringBatch_DoesNotRequestAnotherPage() {
        IUserCleanupService cleanup = Substitute.For<IUserCleanupService>();
        using var cancellation = new CancellationTokenSource();
        cleanup.CleanupDeletedUsersAsync(Arg.Any<DateTime>(), 10, reassignUserId: null, after: null, cancellation.Token)
            .Returns(async _ => {
                await cancellation.CancelAsync();
                return new UserCleanupBatch(0, new UserCleanupCursor(DateTime.UtcNow, new UserId(Guid.NewGuid())));
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CleanupDeletedUsersCommandHandler(cleanup)
            .Handle(new CleanupDeletedUsersCommand(DateTime.UtcNow, 10, ReassignUserId: null), cancellation.Token));

        await cleanup.Received(1).CleanupDeletedUsersAsync(Arg.Any<DateTime>(), 10, reassignUserId: null,
            Arg.Any<UserCleanupCursor?>(), cancellation.Token);
    }
}
