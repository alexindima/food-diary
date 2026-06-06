using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Lessons.Mappings;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Lessons.Queries.GetLessons;

public class GetLessonsQueryHandler(INutritionLessonRepository repository)
    : IQueryHandler<GetLessonsQuery, Result<IReadOnlyList<LessonSummaryModel>>> {
    public async Task<Result<IReadOnlyList<LessonSummaryModel>>> Handle(
        GetLessonsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<LessonSummaryModel>>(userIdResult.Error);
        }

        LessonCategory? categoryFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Category) && Enum.TryParse<LessonCategory>(query.Category, true, out LessonCategory parsed)) {
            categoryFilter = parsed;
        }

        string locale = string.IsNullOrWhiteSpace(query.Locale) ? "en" : query.Locale.Trim().ToLowerInvariant();
        IReadOnlyList<NutritionLesson> lessons = await repository.GetByLocaleAsync(locale, categoryFilter, cancellationToken).ConfigureAwait(false);

        if (lessons.Count == 0 && !string.Equals(locale, "en", StringComparison.Ordinal)) {
            lessons = await repository.GetByLocaleAsync("en", categoryFilter, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<UserLessonProgress> progress = await repository.GetUserProgressAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
        var readIds = progress.Select(p => p.LessonId).ToHashSet();

        var models = lessons
            .OrderBy(l => l.Category)
            .ThenBy(l => l.SortOrder)
            .Select(l => l.ToSummaryModel(readIds))
            .ToList();

        return Result.Success<IReadOnlyList<LessonSummaryModel>>(models);
    }
}
