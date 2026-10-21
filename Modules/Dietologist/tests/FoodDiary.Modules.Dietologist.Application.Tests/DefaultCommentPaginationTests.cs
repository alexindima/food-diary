using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DefaultCommentPaginationTests {
    [Theory]
    [InlineData(2, 2, 2)]
    [InlineData(4, 2, 0)]
    [InlineData(1, 0, 0)]
    public async Task DefaultPager_PreservesTheTotalAndCommentOrderAndForwardsIdentityAndCancellation(int page, int limit, int expectedCount) {
        var recommendation = RecommendationId.New();
        RecommendationCommentReadModel[] rows = [.. Enumerable.Range(0, 5).Select(index => new RecommendationCommentReadModel(
            Guid.NewGuid(), recommendation.Value, Guid.NewGuid(), "Anna", "Example", "author@example.test",
            "Comment " + index, new DateTime(2026, 10, 21, 12, index, 0, DateTimeKind.Utc)))];
        var reader = new LegacyCommentReader(rows);
        IRecommendationCommentReadModelRepository repository = reader;
        using var cancellation = new CancellationTokenSource();

        (IReadOnlyList<RecommendationCommentReadModel> items, int total) =
            await repository.GetPageByRecommendationAsync(recommendation, page, limit, cancellation.Token);
        (RecommendationId, CancellationToken) read = Assert.Single(reader.Reads);

        Assert.Multiple(
            () => Assert.Equal(5, total),
            () => Assert.Equal(expectedCount, items.Count),
            () => Assert.Equal((recommendation, cancellation.Token), read));
        if (expectedCount > 0) { Assert.Same(rows[2], items[0]); Assert.Same(rows[3], items[1]); }
    }

    [ExcludeFromCodeCoverage]
    private sealed class LegacyCommentReader(IReadOnlyList<RecommendationCommentReadModel> rows) : IRecommendationCommentReadModelRepository {
        public List<(RecommendationId, CancellationToken)> Reads { get; } = [];
        public Task<IReadOnlyList<RecommendationCommentReadModel>> GetByRecommendationAsync(RecommendationId recommendationId, CancellationToken cancellationToken = default) {
            Reads.Add((recommendationId, cancellationToken));
            return Task.FromResult(rows);
        }
        public Task<RecommendationParticipantsReadModel?> GetParticipantsAsync(RecommendationId recommendationId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Only comment reads are expected.");
        public Task<bool> IsParticipantAsync(RecommendationId recommendationId, UserId userId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Only comment reads are expected.");
    }
}
