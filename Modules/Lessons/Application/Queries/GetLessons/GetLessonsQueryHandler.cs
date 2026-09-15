using FoodDiary.Modules.Lessons.Application.Mappings;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Application.Abstractions.Models;
using FoodDiary.Modules.Lessons.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Lessons.Application.Queries.GetLessons;

public sealed class GetLessonsQueryHandler(
    INutritionLessonReadModelRepository readModelRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetLessonsQuery, Result<LessonPageModel>> {
    public async Task<Result<LessonPageModel>> Handle(
        GetLessonsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<LessonPageModel>(userIdResult);
        }

        LessonCategory? categoryFilter = ParseOptional<LessonCategory>(query.Category);
        LessonDifficulty? difficultyFilter = ParseOptional<LessonDifficulty>(query.Difficulty);
        LessonSortOption sort = ParseOptional<LessonSortOption>(query.Sort)
            ?? LessonSortOption.Recommended;

        string locale = string.IsNullOrWhiteSpace(query.Locale) ? "en" : query.Locale.Trim().ToLowerInvariant();
        LessonPageModel model = await GetPageByLocaleAsync(
                userIdResult.Value,
                locale,
                categoryFilter,
                difficultyFilter,
                query.Search,
                sort,
                PaginationPolicy.NormalizePage(query.Page),
                PaginationPolicy.NormalizePageSize(query.PageSize),
                cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(model);
    }

    private static TEnum? ParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && SharedEnumValueParser.TryParse(value, out TEnum parsed)
            ? parsed
            : null;
    private async Task<LessonPageModel> GetPageByLocaleAsync(
        UserId userId,
        string locale,
        LessonCategory? categoryFilter,
        LessonDifficulty? difficultyFilter,
        string? search,
        LessonSortOption sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken) {
        int skip = (page - 1) * pageSize;
        LessonSummaryPageReadModel result = await readModelRepository
            .GetSummaryPageByLocaleAsync(locale, categoryFilter, difficultyFilter, search, sort, skip, pageSize, cancellationToken)
            .ConfigureAwait(false);

        string effectiveLocale = locale;
        if (result.TotalLessonCount == 0 && !string.Equals(locale, "en", StringComparison.Ordinal)) {
            effectiveLocale = "en";
            result = await readModelRepository
                .GetSummaryPageByLocaleAsync("en", categoryFilter, difficultyFilter, search, sort, skip, pageSize, cancellationToken)
                .ConfigureAwait(false);
        }

        IReadOnlyList<Guid> readLessonIds = await readModelRepository.GetReadLessonIdsAsync(userId, cancellationToken).ConfigureAwait(false);
        var readIds = new HashSet<Guid>(readLessonIds);
        int readLessonCount = await readModelRepository.CountReadLessonsByLocaleAsync(userId, effectiveLocale, cancellationToken).ConfigureAwait(false);
        int totalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize);

        return new LessonPageModel(
            result.Items.Select(lesson => lesson.ToSummaryModel(readIds)).ToList(),
            page,
            pageSize,
            result.TotalCount,
            totalPages,
            result.TotalLessonCount,
            readLessonCount,
            result.AvailableCategories);
    }
}
