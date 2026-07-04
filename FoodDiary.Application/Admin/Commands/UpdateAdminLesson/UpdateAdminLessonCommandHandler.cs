using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminLesson;

public sealed class UpdateAdminLessonCommandHandler(INutritionLessonWriteRepository repository)
    : ICommandHandler<UpdateAdminLessonCommand, Result<AdminLessonModel>> {
    public async Task<Result<AdminLessonModel>> Handle(
        UpdateAdminLessonCommand command,
        CancellationToken cancellationToken) {
        if (!Enum.TryParse(command.Category, ignoreCase: true, out LessonCategory category)) {
            return Result.Failure<AdminLessonModel>(
                Errors.Validation.Invalid("category", "Invalid lesson category."));
        }

        if (!Enum.TryParse(command.Difficulty, ignoreCase: true, out LessonDifficulty difficulty)) {
            return Result.Failure<AdminLessonModel>(
                Errors.Validation.Invalid("difficulty", "Invalid lesson difficulty."));
        }

        var lessonId = new NutritionLessonId(command.Id);
        NutritionLesson? lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken).ConfigureAwait(false);

        if (lesson is null) {
            return Result.Failure<AdminLessonModel>(Errors.Lesson.NotFound(command.Id));
        }

        lesson.Update(
            command.Title,
            command.Content,
            command.Summary,
            command.Locale,
            category,
            difficulty,
            command.EstimatedReadMinutes,
            command.SortOrder);

        await repository.UpdateAsync(lesson, cancellationToken).ConfigureAwait(false);

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
