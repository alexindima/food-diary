using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Lessons.Mappings;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Lessons.Queries.GetLessonById;

public sealed class GetLessonByIdQueryHandler(INutritionLessonReadRepository repository)
    : IQueryHandler<GetLessonByIdQuery, Result<LessonDetailModel>> {
    public async Task<Result<LessonDetailModel>> Handle(
        GetLessonByIdQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<LessonDetailModel>(userIdResult.Error);
        }

        var lessonId = new NutritionLessonId(query.LessonId);
        NutritionLesson? lesson = await repository.GetByIdAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure<LessonDetailModel>(Errors.Lesson.NotFound(query.LessonId));
        }

        UserLessonProgress? progress = await repository.GetUserProgressForLessonAsync(
            userIdResult.Value, lessonId, cancellationToken).ConfigureAwait(false);

        return Result.Success(lesson.ToDetailModel(progress is not null));
    }
}
