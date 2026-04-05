using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Lessons.Mappings;
using FoodDiary.Application.Lessons.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Queries.GetLessonById;

public class GetLessonByIdQueryHandler(INutritionLessonRepository repository)
    : IQueryHandler<GetLessonByIdQuery, Result<LessonDetailModel>> {
    public async Task<Result<LessonDetailModel>> Handle(
        GetLessonByIdQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<LessonDetailModel>(userIdResult.Error);
        }

        var lessonId = new NutritionLessonId(query.LessonId);
        var lesson = await repository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null) {
            return Result.Failure<LessonDetailModel>(Errors.Lesson.NotFound(query.LessonId));
        }

        var progress = await repository.GetUserProgressForLessonAsync(
            userIdResult.Value, lessonId, cancellationToken);

        return Result.Success(lesson.ToDetailModel(progress is not null));
    }
}
