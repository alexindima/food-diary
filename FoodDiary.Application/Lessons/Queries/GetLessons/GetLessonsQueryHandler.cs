using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Lessons.Models;

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

        LessonCategory? categoryFilter = EnumFilterParser.ParseOptional<LessonCategory>(query.Category);
        LessonDifficulty? difficultyFilter = EnumFilterParser.ParseOptional<LessonDifficulty>(query.Difficulty);
        LessonSortOption sort = EnumFilterParser.ParseOptional<LessonSortOption>(query.Sort)
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
                query.Page,
                query.PageSize,
                cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(model);
    }
}
