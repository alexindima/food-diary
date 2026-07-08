using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Commands.DeleteAdminLesson;

public sealed class DeleteAdminLessonCommandHandler(INutritionLessonWriteRepository repository)
    : ICommandHandler<DeleteAdminLessonCommand, Result> {
    public async Task<Result> Handle(
        DeleteAdminLessonCommand command,
        CancellationToken cancellationToken) {
        Result<NutritionLessonId> lessonIdResult = RequiredIdParser.Parse(
            command.Id,
            nameof(command.Id),
            "Lesson id must not be empty.",
            value => new NutritionLessonId(value));
        if (lessonIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(lessonIdResult);
        }

        NutritionLessonId lessonId = lessonIdResult.Value;
        NutritionLesson? lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken).ConfigureAwait(false);

        if (lesson is null) {
            return Result.Failure(Errors.Lesson.NotFound(command.Id));
        }

        await repository.DeleteAsync(lesson, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
