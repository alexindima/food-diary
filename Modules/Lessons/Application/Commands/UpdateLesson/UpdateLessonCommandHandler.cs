using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Contracts.Commands.UpdateLesson;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Lessons.Commands.UpdateLesson;

public sealed class UpdateLessonCommandHandler(INutritionLessonWriteRepository repository) : IRequestHandler<UpdateLessonCommand, Result<LessonAdminReadModel>> {
    public async Task<Result<LessonAdminReadModel>> Handle(UpdateLessonCommand request, CancellationToken cancellationToken) {
        NutritionLessonId lessonId = request.LessonId;
        string title = request.Title;
        string content = request.Content;
        string? summary = request.Summary;
        string locale = request.Locale;
        LessonCategory category = request.Category;
        LessonDifficulty difficulty = request.Difficulty;
        int estimatedReadMinutes = request.EstimatedReadMinutes;
        int sortOrder = request.SortOrder;
        bool isPublished = request.IsPublished;
        NutritionLesson? lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken).ConfigureAwait(false);
        if (lesson is null) {
            return Result.Failure<LessonAdminReadModel>(LessonErrors.NotFound(lessonId.Value));
        }

        lesson.Update(title, content, summary, locale, category, difficulty, estimatedReadMinutes, sortOrder);
        lesson.SetPublication(isPublished);
        await repository.UpdateAsync(lesson, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToAdminReadModel(lesson));

    }

    private static LessonAdminReadModel ToAdminReadModel(NutritionLesson lesson) =>
        new(lesson.Id.Value, lesson.Title, lesson.Content, lesson.Summary, lesson.Locale,
            lesson.Category.ToString(), lesson.Difficulty.ToString(), lesson.EstimatedReadMinutes,
            lesson.SortOrder, lesson.CreatedOnUtc, lesson.ModifiedOnUtc, lesson.IsPublished);

}
