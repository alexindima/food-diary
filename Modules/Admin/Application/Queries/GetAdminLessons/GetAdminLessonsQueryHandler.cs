using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryHandler(ILessonAdministrationReadService lessonReadService)
    : IQueryHandler<GetAdminLessonsQuery, Result<IReadOnlyList<AdminLessonModel>>> {
    public async Task<Result<IReadOnlyList<AdminLessonModel>>> Handle(GetAdminLessonsQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<LessonAdminReadModel> lessons = await lessonReadService
            .GetLessonsAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminLessonModel>>(lessons.Select(static lesson => lesson.ToAdminModel()).ToList());
    }
}
