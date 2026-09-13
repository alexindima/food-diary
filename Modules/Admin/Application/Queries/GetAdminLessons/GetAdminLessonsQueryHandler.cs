using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryHandler(ILessonAdministrationReadService lessonReadService)
    : IQueryHandler<GetAdminLessonsQuery, Result<IReadOnlyList<AdminLessonModel>>> {
    public async Task<Result<IReadOnlyList<AdminLessonModel>>> Handle(GetAdminLessonsQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<LessonAdminReadModel> lessons = await lessonReadService
            .GetLessonsAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminLessonModel>>(lessons.Select(static lesson => lesson.ToAdminModel()).ToList());
    }
}
