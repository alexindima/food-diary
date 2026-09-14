using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;
using FoodDiary.Modules.Lessons.Contracts.Commands.CreateLesson;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Lessons.Commands.CreateLesson;

public sealed class CreateLessonCommandHandler(INutritionLessonWriteRepository repository) : IRequestHandler<CreateLessonCommand, Result<LessonAdminReadModel>> {
    public async Task<Result<LessonAdminReadModel>> Handle(CreateLessonCommand request, CancellationToken cancellationToken) {
        string title = request.Title;
        string content = request.Content;
        string? summary = request.Summary;
        string locale = request.Locale;
        LessonCategory category = request.Category;
        LessonDifficulty difficulty = request.Difficulty;
        int estimatedReadMinutes = request.EstimatedReadMinutes;
        int sortOrder = request.SortOrder;
        bool isPublished = request.IsPublished;
        var lesson = NutritionLesson.Create(
            title,
            content,
            summary,
            locale,
            category,
            difficulty,
            estimatedReadMinutes,
            sortOrder);

        lesson.SetPublication(isPublished);
        await repository.AddAsync(lesson, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToAdminReadModel(lesson));

    }

    private static LessonAdminReadModel ToAdminReadModel(NutritionLesson lesson) =>
        new(lesson.Id.Value, lesson.Title, lesson.Content, lesson.Summary, lesson.Locale,
            lesson.Category.ToString(), lesson.Difficulty.ToString(), lesson.EstimatedReadMinutes,
            lesson.SortOrder, lesson.CreatedOnUtc, lesson.ModifiedOnUtc, lesson.IsPublished);

}
