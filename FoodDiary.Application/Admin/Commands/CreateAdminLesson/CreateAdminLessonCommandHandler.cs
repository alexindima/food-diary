using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Lessons.Common;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Commands.CreateAdminLesson;

public sealed class CreateAdminLessonCommandHandler(INutritionLessonWriteRepository repository)
    : ICommandHandler<CreateAdminLessonCommand, Result<AdminLessonModel>> {
    public async Task<Result<AdminLessonModel>> Handle(
        CreateAdminLessonCommand command,
        CancellationToken cancellationToken) {
        Result<LessonCategory> categoryResult = AdminLessonValueParser.ParseCategory(command.Category, "category");
        if (categoryResult.IsFailure) {
            return Result.Failure<AdminLessonModel>(categoryResult.Error);
        }

        Result<LessonDifficulty> difficultyResult = AdminLessonValueParser.ParseDifficulty(command.Difficulty, "difficulty");
        if (difficultyResult.IsFailure) {
            return Result.Failure<AdminLessonModel>(difficultyResult.Error);
        }

        var lesson = NutritionLesson.Create(
            command.Title,
            command.Content,
            command.Summary,
            command.Locale,
            categoryResult.Value,
            difficultyResult.Value,
            command.EstimatedReadMinutes,
            command.SortOrder);

        await repository.AddAsync(lesson, cancellationToken).ConfigureAwait(false);

        return Result.Success(lesson.ToAdminModel());
    }
}
