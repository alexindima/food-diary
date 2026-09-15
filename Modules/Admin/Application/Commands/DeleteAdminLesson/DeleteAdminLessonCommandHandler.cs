using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Lessons.Contracts.Commands.DeleteLesson;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Internal.Validation;

namespace FoodDiary.Modules.Admin.Application.Commands.DeleteAdminLesson;

public sealed class DeleteAdminLessonCommandHandler(ISender lessonAdministrationService)
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

        return await lessonAdministrationService.Send(new DeleteLessonCommand(LessonId: lessonIdResult.Value), cancellationToken)
            .ConfigureAwait(false);
    }
}
