using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Lessons.Commands.MarkLessonRead;

public sealed class MarkLessonReadCommandHandler(
    INutritionLessonReadRepository readRepository,
    INutritionLessonWriteRepository writeRepository,
    TimeProvider dateTimeProvider)
    : ICommandHandler<MarkLessonReadCommand, Result> {
    public async Task<Result> Handle(
        MarkLessonReadCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        Result<NutritionLessonId> lessonIdResult = RequiredIdParser.Parse(
            command.LessonId,
            nameof(command.LessonId),
            "Lesson id must not be empty.",
            value => new NutritionLessonId(value));
        if (lessonIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(lessonIdResult);
        }

        NutritionLessonId lessonId = lessonIdResult.Value;
        NutritionLesson? lesson = await readRepository.GetByIdAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure(Errors.Lesson.NotFound(command.LessonId));
        }

        UserLessonProgress? existing = await readRepository.GetUserProgressForLessonAsync(
            userIdResult.Value, lessonId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Success();
        }

        var progress = UserLessonProgress.Create(
            userIdResult.Value, lessonId, dateTimeProvider.GetUtcNow().UtcDateTime);
        await writeRepository.AddProgressAsync(progress, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
