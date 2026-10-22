using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Application.Abstractions.Models;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Lessons.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DefaultAdminLessonReadModelTests {
    [Fact]
    public void UnsupportedUnpagedAdminRead_ThrowsBeforeAnyUnrelatedRead() {
        var reader = new RequiredReadsOnlyRepository();
        INutritionLessonReadModelRepository repository = reader;
        using var cancellation = new CancellationTokenSource();

        Action read = () => _ = repository.GetAdminReadModelsAsync(cancellation.Token);
        Assert.Throws<NotSupportedException>(read);

        Assert.Equal(0, reader.UnexpectedReads);
    }

    [Fact]
    public async Task DefaultAdminPager_PropagatesAnUnsupportedUnderlyingReadAsync() {
        var reader = new RequiredReadsOnlyRepository();
        INutritionLessonReadModelRepository repository = reader;
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.GetAdminReadModelsAsync(1, 10, cancellation.Token));

        Assert.Equal(0, reader.UnexpectedReads);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RequiredReadsOnlyRepository : INutritionLessonReadModelRepository {
        public int UnexpectedReads { get; private set; }

        public Task<IReadOnlyList<LessonSummaryReadModel>> GetSummaryReadModelsByLocaleAsync(
            string locale, LessonCategory? category = null, CancellationToken cancellationToken = default) => throw UnexpectedRead();

        public Task<LessonSummaryPageReadModel> GetSummaryPageByLocaleAsync(
            string locale, LessonCategory? category, LessonDifficulty? difficulty, string? search,
            LessonSortOption sort, int skip, int take, CancellationToken cancellationToken = default) => throw UnexpectedRead();

        public Task<int> CountReadLessonsByLocaleAsync(UserId userId, string locale, CancellationToken cancellationToken = default) => throw UnexpectedRead();

        public Task<LessonDetailReadModel?> GetDetailReadModelByIdAsync(NutritionLessonId id, CancellationToken cancellationToken = default) => throw UnexpectedRead();

        public Task<IReadOnlyList<Guid>> GetReadLessonIdsAsync(UserId userId, CancellationToken cancellationToken = default) => throw UnexpectedRead();

        public Task<bool> IsLessonReadAsync(UserId userId, NutritionLessonId lessonId, CancellationToken cancellationToken = default) => throw UnexpectedRead();

        private InvalidOperationException UnexpectedRead() {
            UnexpectedReads++;
            return new InvalidOperationException("Only the optional admin default should be called.");
        }
    }
}
