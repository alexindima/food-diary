using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Queries.GetLessons;

public sealed class GetLessonsQueryHandler(ILessonReadService lessonReadService)
    : IQueryHandler<GetLessonsQuery, Result<IReadOnlyList<LessonSummaryModel>>> {
    public async Task<Result<IReadOnlyList<LessonSummaryModel>>> Handle(
        GetLessonsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<LessonSummaryModel>>(userIdResult);
        }

        LessonCategory? categoryFilter = EnumFilterParser.ParseOptional<LessonCategory>(query.Category);

        string locale = string.IsNullOrWhiteSpace(query.Locale) ? "en" : query.Locale.Trim().ToLowerInvariant();
        IReadOnlyList<LessonSummaryModel> models = await lessonReadService
            .GetByLocaleAsync(userIdResult.Value, locale, categoryFilter, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(models);
    }
}
