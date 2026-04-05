using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;
using FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;
using FoodDiary.Presentation.Api.Features.Exercises.Requests;
using FoodDiary.Presentation.Api.Features.Exercises.Responses;

namespace FoodDiary.Presentation.Api.Features.Exercises.Mappings;

public static class ExerciseHttpMappings {
    public static GetExerciseEntriesQuery ToQuery(this Guid userId, DateTime dateFrom, DateTime dateTo) =>
        new(userId, dateFrom, dateTo);

    public static CreateExerciseEntryCommand ToCommand(this CreateExerciseEntryHttpRequest request, Guid userId) =>
        new(userId, request.Date, request.ExerciseType, request.DurationMinutes,
            request.CaloriesBurned, request.Name, request.Notes);

    public static UpdateExerciseEntryCommand ToCommand(this UpdateExerciseEntryHttpRequest request, Guid userId, Guid entryId) =>
        new(userId, entryId, request.ExerciseType, request.DurationMinutes,
            request.CaloriesBurned, request.Name, request.ClearName,
            request.Notes, request.ClearNotes, request.Date);

    public static DeleteExerciseEntryCommand ToDeleteCommand(this Guid userId, Guid entryId) =>
        new(userId, entryId);

    public static ExerciseEntryHttpResponse ToHttpResponse(this ExerciseEntryModel model) =>
        new(model.Id, model.Date, model.ExerciseType, model.Name,
            model.DurationMinutes, model.CaloriesBurned, model.Notes);

    public static IReadOnlyList<ExerciseEntryHttpResponse> ToHttpResponse(
        this IReadOnlyList<ExerciseEntryModel> models) =>
        models.Select(m => m.ToHttpResponse()).ToList();
}
