using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminLesson;

public sealed class UpdateAdminLessonCommandHandler(INutritionLessonWriteRepository repository)
    : ICommandHandler<UpdateAdminLessonCommand, Result<AdminLessonModel>> {
    public async Task<Result<AdminLessonModel>> Handle(
        UpdateAdminLessonCommand command,
        CancellationToken cancellationToken) {
        Result<LessonCategory> categoryResult = AdminLessonValueParser.ParseCategory(command.Category, "category");
        if (categoryResult.IsFailure) {
            return Result.Failure<AdminLessonModel>(categoryResult.Error);
        }

        Result<LessonDifficulty> difficultyResult = AdminLessonValueParser.ParseDifficulty(command.Difficulty, "difficulty");
        if (difficultyResult.IsFailure) {
            return Result.Failure<AdminLessonModel>(difficultyResult.Error);
        }

        Result<NutritionLessonId> lessonIdResult = RequiredIdParser.Parse(
            command.Id,
            nameof(command.Id),
            "Lesson id must not be empty.",
            value => new NutritionLessonId(value));
        if (lessonIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<AdminLessonModel, NutritionLessonId>(lessonIdResult);
        }

        NutritionLessonId lessonId = lessonIdResult.Value;
        NutritionLesson? lesson = await repository.GetByIdTrackingAsync(lessonId, cancellationToken).ConfigureAwait(false);

        if (lesson is null) {
            return Result.Failure<AdminLessonModel>(Errors.Lesson.NotFound(command.Id));
        }

        lesson.Update(
            command.Title,
            command.Content,
            command.Summary,
            command.Locale,
            categoryResult.Value,
            difficultyResult.Value,
            command.EstimatedReadMinutes,
            command.SortOrder);

        await repository.UpdateAsync(lesson, cancellationToken).ConfigureAwait(false);

        return Result.Success(lesson.ToAdminModel());
    }
}
