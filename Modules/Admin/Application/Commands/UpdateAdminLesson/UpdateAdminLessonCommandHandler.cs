using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Mediator;
using FoodDiary.Modules.Lessons.Contracts.Commands.UpdateLesson;
using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Internal.Validation;
using FoodDiary.Modules.Lessons.Contracts.Models;

namespace FoodDiary.Modules.Admin.Application.Commands.UpdateAdminLesson;

public sealed class UpdateAdminLessonCommandHandler(ISender lessonAdministrationService)
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

        Result<LessonAdminReadModel> lessonResult = await lessonAdministrationService.Send(new UpdateLessonCommand(LessonId: lessonIdResult.Value, Title: command.Title, Content: command.Content, Summary: command.Summary, Locale: command.Locale, Category: categoryResult.Value, Difficulty: difficultyResult.Value, EstimatedReadMinutes: command.EstimatedReadMinutes, SortOrder: command.SortOrder, IsPublished: command.IsPublished), cancellationToken).ConfigureAwait(false);

        return lessonResult.IsSuccess
            ? Result.Success(lessonResult.Value.ToAdminModel())
            : Result.Failure<AdminLessonModel>(lessonResult.Error);
    }
}
