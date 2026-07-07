using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Queries.GetLessonById;

public sealed class GetLessonByIdQueryHandler(ILessonReadService lessonReadService)
    : IQueryHandler<GetLessonByIdQuery, Result<LessonDetailModel>> {
    public async Task<Result<LessonDetailModel>> Handle(
        GetLessonByIdQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<LessonDetailModel>(userIdResult);
        }

        Result<NutritionLessonId> lessonIdResult = RequiredIdParser.Parse(
            query.LessonId,
            nameof(query.LessonId),
            "Lesson id must not be empty.",
            value => new NutritionLessonId(value));
        if (lessonIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<LessonDetailModel, NutritionLessonId>(lessonIdResult);
        }

        NutritionLessonId lessonId = lessonIdResult.Value;
        LessonDetailModel? lesson = await lessonReadService.GetByIdAsync(userIdResult.Value, lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure<LessonDetailModel>(Errors.Lesson.NotFound(query.LessonId));
        }

        return Result.Success(lesson);
    }
}
