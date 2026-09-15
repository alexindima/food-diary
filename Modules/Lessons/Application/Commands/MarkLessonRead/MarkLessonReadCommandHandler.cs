using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Gamification.Contracts.Achievements.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Lessons.Application.Commands.MarkLessonRead;

public sealed class MarkLessonReadCommandHandler(
    INutritionLessonReadRepository readRepository,
    INutritionLessonWriteRepository writeRepository,
    TimeProvider dateTimeProvider,
    ICurrentUserAccessService currentUserAccessService,
    IAchievementEvaluationOutbox achievementEvaluationOutbox,
    ILessonProgressTransactionRunner transactionRunner)
    : ICommandHandler<MarkLessonReadCommand, Result> {
    public async Task<Result> Handle(
        MarkLessonReadCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        if (command.LessonId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(command.LessonId),
                "Lesson id must not be empty."));
        }

        var lessonId = (NutritionLessonId)command.LessonId;
        return await transactionRunner.ExecuteSerializedAsync(userIdResult.Value, lessonId,
            token => MarkReadAsync(userIdResult.Value, lessonId, token), cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> MarkReadAsync(UserId userId, NutritionLessonId lessonId, CancellationToken cancellationToken) {
        NutritionLesson? lesson = await readRepository.GetByIdAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure(LessonErrors.NotFound(lessonId.Value));
        }

        UserLessonProgress? existing = await readRepository.GetUserProgressForLessonAsync(
            userId, lessonId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Success();
        }

        DateTime readAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        var progress = UserLessonProgress.Create(userId, lessonId, readAtUtc);
        await writeRepository.AddProgressAsync(progress, cancellationToken).ConfigureAwait(false);
        await achievementEvaluationOutbox.EnqueueAsync(userId, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
