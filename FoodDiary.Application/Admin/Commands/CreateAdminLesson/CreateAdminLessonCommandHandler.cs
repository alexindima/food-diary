using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Commands.CreateAdminLesson;

public sealed class CreateAdminLessonCommandHandler(INutritionLessonRepository repository)
    : ICommandHandler<CreateAdminLessonCommand, Result<AdminLessonModel>> {
    public async Task<Result<AdminLessonModel>> Handle(
        CreateAdminLessonCommand command,
        CancellationToken cancellationToken) {
        if (!Enum.TryParse<LessonCategory>(command.Category, true, out var category)) {
            return Result.Failure<AdminLessonModel>(
                Errors.Validation.Invalid("category", "Invalid lesson category."));
        }

        if (!Enum.TryParse<LessonDifficulty>(command.Difficulty, true, out var difficulty)) {
            return Result.Failure<AdminLessonModel>(
                Errors.Validation.Invalid("difficulty", "Invalid lesson difficulty."));
        }

        var lesson = NutritionLesson.Create(
            command.Title,
            command.Content,
            command.Summary,
            command.Locale,
            category,
            difficulty,
            command.EstimatedReadMinutes,
            command.SortOrder);

        await repository.AddAsync(lesson, cancellationToken);

        return Result.Success(new AdminLessonModel(
            lesson.Id.Value,
            lesson.Title,
            lesson.Content,
            lesson.Summary,
            lesson.Locale,
            lesson.Category.ToString(),
            lesson.Difficulty.ToString(),
            lesson.EstimatedReadMinutes,
            lesson.SortOrder,
            lesson.CreatedOnUtc,
            lesson.ModifiedOnUtc));
    }
}
