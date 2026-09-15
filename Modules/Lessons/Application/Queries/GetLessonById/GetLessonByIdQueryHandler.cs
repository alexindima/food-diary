using FoodDiary.Modules.Lessons.Application.Mappings;
using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Application.Abstractions.Common;
using FoodDiary.Modules.Lessons.Application.Abstractions.Models;
using FoodDiary.Modules.Lessons.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Lessons.Application.Queries.GetLessonById;

public sealed class GetLessonByIdQueryHandler(
    INutritionLessonReadModelRepository readModelRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetLessonByIdQuery, Result<LessonDetailModel>> {
    public async Task<Result<LessonDetailModel>> Handle(
        GetLessonByIdQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<LessonDetailModel>(userIdResult);
        }

        if (query.LessonId == Guid.Empty) {
            return Result.Failure<LessonDetailModel>(Errors.Validation.Invalid(
                nameof(query.LessonId),
                "Lesson id must not be empty."));
        }

        var lessonId = (NutritionLessonId)query.LessonId;
        LessonDetailModel? lesson = await GetByIdAsync(userIdResult.Value, lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure<LessonDetailModel>(LessonErrors.NotFound(query.LessonId));
        }

        return Result.Success(lesson);
    }
    private async Task<LessonDetailModel?> GetByIdAsync(
        UserId userId,
        NutritionLessonId lessonId,
        CancellationToken cancellationToken) {
        LessonDetailReadModel? lesson = await readModelRepository.GetDetailReadModelByIdAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return null;
        }

        bool isRead = await readModelRepository
            .IsLessonReadAsync(userId, lessonId, cancellationToken)
            .ConfigureAwait(false);

        return lesson.ToDetailModel(isRead);
    }
}
