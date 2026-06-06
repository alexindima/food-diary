using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Commands.DeleteAdminLesson;

public sealed class DeleteAdminLessonCommandHandler(INutritionLessonRepository repository)
    : ICommandHandler<DeleteAdminLessonCommand, Result> {
    public async Task<Result> Handle(
        DeleteAdminLessonCommand command,
        CancellationToken cancellationToken) {
        var lessonId = new NutritionLessonId(command.Id);
        NutritionLesson? lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken).ConfigureAwait(false);

        if (lesson is null) {
            return Result.Failure(Errors.Lesson.NotFound(command.Id));
        }

        await repository.DeleteAsync(lesson, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
