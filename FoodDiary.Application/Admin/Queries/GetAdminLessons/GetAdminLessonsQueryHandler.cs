using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Lessons.Common;

namespace FoodDiary.Application.Admin.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryHandler(INutritionLessonRepository repository)
    : IQueryHandler<GetAdminLessonsQuery, Result<IReadOnlyList<AdminLessonModel>>> {
    public async Task<Result<IReadOnlyList<AdminLessonModel>>> Handle(
        GetAdminLessonsQuery query,
        CancellationToken cancellationToken) {
        var lessons = await repository.GetAllAsync(cancellationToken);

        var models = lessons.Select(l => new AdminLessonModel(
            l.Id.Value,
            l.Title,
            l.Content,
            l.Summary,
            l.Locale,
            l.Category.ToString(),
            l.Difficulty.ToString(),
            l.EstimatedReadMinutes,
            l.SortOrder,
            l.CreatedOnUtc,
            l.ModifiedOnUtc)).ToList();

        return Result.Success<IReadOnlyList<AdminLessonModel>>(models);
    }
}
