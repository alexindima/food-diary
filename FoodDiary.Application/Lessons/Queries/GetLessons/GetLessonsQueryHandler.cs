using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Mappings;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Lessons.Queries.GetLessons;

public class GetLessonsQueryHandler(INutritionLessonRepository repository)
    : IQueryHandler<GetLessonsQuery, Result<IReadOnlyList<LessonSummaryModel>>> {
    public async Task<Result<IReadOnlyList<LessonSummaryModel>>> Handle(
        GetLessonsQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<LessonSummaryModel>>(userIdResult.Error);
        }

        LessonCategory? categoryFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Category) && Enum.TryParse<LessonCategory>(query.Category, true, out var parsed)) {
            categoryFilter = parsed;
        }

        var locale = string.IsNullOrWhiteSpace(query.Locale) ? "en" : query.Locale.Trim().ToLowerInvariant();
        var lessons = await repository.GetByLocaleAsync(locale, categoryFilter, cancellationToken);

        if (lessons.Count == 0 && locale != "en") {
            lessons = await repository.GetByLocaleAsync("en", categoryFilter, cancellationToken);
        }

        var progress = await repository.GetUserProgressAsync(userIdResult.Value, cancellationToken);
        var readIds = progress.Select(p => p.LessonId).ToHashSet();

        var models = lessons
            .OrderBy(l => l.Category)
            .ThenBy(l => l.SortOrder)
            .Select(l => l.ToSummaryModel(readIds))
            .ToList();

        return Result.Success<IReadOnlyList<LessonSummaryModel>>(models);
    }
}
