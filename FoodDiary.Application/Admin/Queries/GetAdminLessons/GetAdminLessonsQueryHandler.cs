using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryHandler(INutritionLessonReadRepository repository)
    : IQueryHandler<GetAdminLessonsQuery, Result<IReadOnlyList<AdminLessonModel>>> {
    public async Task<Result<IReadOnlyList<AdminLessonModel>>> Handle(
        GetAdminLessonsQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<NutritionLesson> lessons = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var models = lessons.Select(static lesson => lesson.ToAdminModel()).ToList();

        return Result.Success<IReadOnlyList<AdminLessonModel>>(models);
    }
}
