using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Commands.MarkLessonRead;

public class MarkLessonReadCommandHandler(
    INutritionLessonRepository repository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<MarkLessonReadCommand, Result> {
    public async Task<Result> Handle(
        MarkLessonReadCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var lessonId = new NutritionLessonId(command.LessonId);
        var lesson = await repository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null) {
            return Result.Failure(Errors.Lesson.NotFound(command.LessonId));
        }

        var existing = await repository.GetUserProgressForLessonAsync(
            userIdResult.Value, lessonId, cancellationToken);
        if (existing is not null) {
            return Result.Success();
        }

        var progress = UserLessonProgress.Create(
            userIdResult.Value, lessonId, dateTimeProvider.UtcNow);
        await repository.AddProgressAsync(progress, cancellationToken);

        return Result.Success();
    }
}
