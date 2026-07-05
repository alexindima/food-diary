using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryHandler(IAdminContentReadService adminContentReadService)
    : IQueryHandler<GetAdminLessonsQuery, Result<IReadOnlyList<AdminLessonModel>>> {
    public async Task<Result<IReadOnlyList<AdminLessonModel>>> Handle(
        GetAdminLessonsQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<AdminLessonModel> models = await adminContentReadService.GetLessonsAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(models);
    }
}
