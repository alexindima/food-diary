using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.RecentItems;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class PostCommitRecentItemUsageRecorderTests {
    [Fact]
    public async Task RegisterUsageAsync_WithItems_DefersWriteUntilPostCommitActionRuns() {
        IRecentItemWriteRepository repository = Substitute.For<IRecentItemWriteRepository>();
        IPostCommitActionQueue queue = Substitute.For<IPostCommitActionQueue>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        Func<CancellationToken, Task>? capturedAction = null;
        queue.When(candidate => candidate.Enqueue(
                "recent-items.register-usage",
                Arg.Any<Func<CancellationToken, Task>>()))
            .Do(call => capturedAction = call.ArgAt<Func<CancellationToken, Task>>(1));
        var recorder = new PostCommitRecentItemUsageRecorder(repository, queue, unitOfWork);
        var userId = UserId.New();
        var productId = ProductId.New();
        var recipeId = RecipeId.New();
        ProductId[] expectedProductIds = [productId];
        RecipeId[] expectedRecipeIds = [recipeId];

        await recorder.RegisterUsageAsync(userId, [productId], [recipeId], CancellationToken.None);

        await repository.DidNotReceiveWithAnyArgs().RegisterUsageAsync(default, [], [], default);
        Assert.NotNull(capturedAction);

        using var cancellationTokenSource = new CancellationTokenSource();
        await capturedAction(cancellationTokenSource.Token);

        await repository.Received(1).RegisterUsageAsync(
            userId,
            Arg.Is<IReadOnlyCollection<ProductId>>(ids => ids.SequenceEqual(expectedProductIds)),
            Arg.Is<IReadOnlyCollection<RecipeId>>(ids => ids.SequenceEqual(expectedRecipeIds)),
            cancellationTokenSource.Token);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task RegisterUsageAsync_WhenPostCommitWriteTracksChanges_SavesThem() {
        IRecentItemWriteRepository repository = Substitute.For<IRecentItemWriteRepository>();
        IPostCommitActionQueue queue = Substitute.For<IPostCommitActionQueue>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true, returnThese: []);
        Func<CancellationToken, Task>? capturedAction = null;
        queue.When(candidate => candidate.Enqueue(
                "recent-items.register-usage",
                Arg.Any<Func<CancellationToken, Task>>()))
            .Do(call => capturedAction = call.ArgAt<Func<CancellationToken, Task>>(1));
        var recorder = new PostCommitRecentItemUsageRecorder(repository, queue, unitOfWork);

        await recorder.RegisterUsageAsync(
            userId: UserId.New(),
            productIds: [ProductId.New()],
            recipeIds: [],
            cancellationToken: CancellationToken.None);

        Assert.NotNull(capturedAction);
        await capturedAction(CancellationToken.None);

        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }
}
