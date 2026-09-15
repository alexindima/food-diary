using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Application.Abstractions.Models;
using FoodDiary.Modules.Images.Application.Commands.CleanupOrphanImages;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Modules.Images.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CleanupOrphanImagesCommandHandlerTests {
    [Fact]
    public async Task Handle_FailedFullPageStillAdvancesAndDeletesLaterCandidates() {
        DateTime created = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var failed = new ImageCleanupCandidate(new ImageAssetId(Guid.Parse("00000000-0000-0000-0000-000000000001")), created);
        var successful = new ImageCleanupCandidate(new ImageAssetId(Guid.Parse("00000000-0000-0000-0000-000000000002")), created);
        IImageAssetUsageQuery query = Substitute.For<IImageAssetUsageQuery>();
        IImageAssetCleanupBatch batch = Substitute.For<IImageAssetCleanupBatch>();
        DateTime cutoff = created.AddDays(1);
        query.GetUnusedCandidatesOlderThanAsync(cutoff, 1, after: null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ImageCleanupCandidate>>([failed]));
        query.GetUnusedCandidatesOlderThanAsync(cutoff, 1, failed, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ImageCleanupCandidate>>([successful]));
        query.GetUnusedCandidatesOlderThanAsync(cutoff, 1, successful, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ImageCleanupCandidate>>([]));
        batch.DeleteUnusedAsync(failed.Id, Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new InvalidOperationException("Cannot delete")));
        batch.DeleteUnusedAsync(successful.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var handler = new CleanupOrphanImagesCommandHandler(query, batch, NullLogger<CleanupOrphanImagesCommandHandler>.Instance);

        int removed = await handler.Handle(new CleanupOrphanImagesCommand(cutoff, 1), CancellationToken.None);

        Assert.Equal(1, removed);
        await batch.Received(1).DeleteUnusedAsync(failed.Id, Arg.Any<CancellationToken>());
        await batch.Received(1).DeleteUnusedAsync(successful.Id, Arg.Any<CancellationToken>());
        Assert.Equal(3, query.ReceivedCalls().Count());
    }
}
