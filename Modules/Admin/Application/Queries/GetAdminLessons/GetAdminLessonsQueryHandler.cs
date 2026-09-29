using FoodDiary.Mediator;
using FoodDiary.Modules.Lessons.Contracts.Queries.GetLessonsForAdministration;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryHandler(ISender lessonReadService)
    : IQueryHandler<GetAdminLessonsQuery, Result<IReadOnlyList<AdminLessonModel>>> {
    public async Task<Result<IReadOnlyList<AdminLessonModel>>> Handle(GetAdminLessonsQuery query, CancellationToken cancellationToken) {
        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        IReadOnlyList<LessonAdminReadModel> lessons = await lessonReadService.Send(new GetLessonsForAdministrationQuery(page, limit), cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminLessonModel>>(lessons.Select(static lesson => lesson.ToAdminModel()).ToList());
    }
}
