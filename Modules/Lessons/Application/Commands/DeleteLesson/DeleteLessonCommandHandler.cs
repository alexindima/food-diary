using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Contracts.Commands.DeleteLesson;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Lessons.Commands.DeleteLesson;

public sealed class DeleteLessonCommandHandler(INutritionLessonWriteRepository repository) : IRequestHandler<DeleteLessonCommand, Result> {
    public async Task<Result> Handle(DeleteLessonCommand request, CancellationToken cancellationToken) {
        NutritionLessonId lessonId = request.LessonId;
        NutritionLesson? lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure(LessonErrors.NotFound(lessonId.Value));
        }

        await repository.DeleteAsync(lesson, cancellationToken).ConfigureAwait(false);
        return Result.Success();

    }

}
