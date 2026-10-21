using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DefaultTemplatePaginationTests {
    [Theory]
    [InlineData(null, false, 2, 2, 2)]
    [InlineData("  soup  ", true, 2, 2, 2)]
    [InlineData("query", true, 4, 2, 0)]
    [InlineData("query", false, 1, 0, 0)]
    public async Task CompatibilitySearch_PagesResultsAndPreservesSearchArchiveScopeAndCancellation(string? search, bool includeArchived, int page, int limit, int expectedCount) {
        var user = UserId.New();
        RecommendationTemplateReadModel[] rows = [.. Enumerable.Range(0, 5).Select(index => new RecommendationTemplateReadModel(
            Guid.NewGuid(), "Template", "Recommendation", IsArchived: false, new DateTime(2026, 10, 21, 12, index, 0, DateTimeKind.Utc), ModifiedAtUtc: null))];
        var reader = new LegacyTemplateReader(rows);
        IRecommendationTemplateReadModelRepository repository = reader;
        using var cancellation = new CancellationTokenSource();

        IReadOnlyList<RecommendationTemplateReadModel> result = await repository.SearchAsync(user, search, includeArchived, page, limit, cancellation.Token);

        (UserId, string?, bool, CancellationToken) read = Assert.Single(reader.Reads);
        Assert.Equal((user, search, includeArchived, cancellation.Token), read);
        Assert.Equal(expectedCount, result.Count);
        if (expectedCount > 0) { Assert.Same(rows[2], result[0]); Assert.Same(rows[3], result[1]); }
    }

    [Fact]
    public async Task MissingImplementation_ReportsUnsupportedSearchInsteadOfAnEmptyList() {
        IRecommendationTemplateReadModelRepository repository = new MissingTemplateReader();
        var user = UserId.New();

        Action search = () => _ = repository.SearchAsync(user, search: null, includeArchived: false, CancellationToken.None);
        Assert.Throws<NotSupportedException>(search);
        await Assert.ThrowsAsync<NotSupportedException>(() => repository.SearchAsync(user, search: null, includeArchived: false, 1, 10, CancellationToken.None));
    }

    [ExcludeFromCodeCoverage]
    private sealed class LegacyTemplateReader(IReadOnlyList<RecommendationTemplateReadModel> rows) : IRecommendationTemplateReadModelRepository {
        public List<(UserId, string?, bool, CancellationToken)> Reads { get; } = [];
        public Task<IReadOnlyList<RecommendationTemplateReadModel>> SearchAsync(UserId dietologistUserId, string? search, bool includeArchived, CancellationToken cancellationToken = default) {
            Reads.Add((dietologistUserId, search, includeArchived, cancellationToken));
            return Task.FromResult(rows);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class MissingTemplateReader : IRecommendationTemplateReadModelRepository;
}
