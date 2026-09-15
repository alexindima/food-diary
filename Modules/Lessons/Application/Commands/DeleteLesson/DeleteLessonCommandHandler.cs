using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Contracts.Commands.DeleteLesson;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Lessons.Application.Commands.DeleteLesson;

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
