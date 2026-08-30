using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Queries.GetLessonById;

public sealed class GetLessonByIdQueryHandler(
    ILessonReadService lessonReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetLessonByIdQuery, Result<LessonDetailModel>> {
    public async Task<Result<LessonDetailModel>> Handle(
        GetLessonByIdQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<LessonDetailModel>(userIdResult);
        }

        if (query.LessonId == Guid.Empty) {
            return Result.Failure<LessonDetailModel>(Errors.Validation.Invalid(
                nameof(query.LessonId),
                "Lesson id must not be empty."));
        }

        var lessonId = new NutritionLessonId(query.LessonId);
        LessonDetailModel? lesson = await lessonReadService.GetByIdAsync(userIdResult.Value, lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure<LessonDetailModel>(Errors.Lesson.NotFound(query.LessonId));
        }

        return Result.Success(lesson);
    }
}
