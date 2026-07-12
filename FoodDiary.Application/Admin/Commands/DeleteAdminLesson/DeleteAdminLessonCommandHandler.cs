using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.DeleteAdminLesson;

public sealed class DeleteAdminLessonCommandHandler(ILessonAdministrationService lessonAdministrationService)
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

        return await lessonAdministrationService
            .DeleteAsync(lessonIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}
