using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.DeleteAdminLesson;

public sealed class DeleteAdminLessonCommandHandler(INutritionLessonRepository repository)
    : ICommandHandler<DeleteAdminLessonCommand, Result> {
    public async Task<Result> Handle(
        DeleteAdminLessonCommand command,
        CancellationToken cancellationToken) {
        var lessonId = new NutritionLessonId(command.Id);
        var lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken);

        if (lesson is null) {
            return Result.Failure(Errors.Lesson.NotFound(command.Id));
        }

        await repository.DeleteAsync(lesson, cancellationToken);

        return Result.Success();
    }
}
