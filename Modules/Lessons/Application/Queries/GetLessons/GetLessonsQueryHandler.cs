using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Lessons.Queries.GetLessons;

public sealed class GetLessonsQueryHandler(
    ILessonReadService lessonReadService,
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
        LessonPageModel model = await lessonReadService
            .GetPageByLocaleAsync(
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
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            ? parsed
            : null;
}
