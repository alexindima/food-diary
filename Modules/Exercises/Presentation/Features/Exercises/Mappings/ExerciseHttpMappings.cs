using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;
using FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;
using FoodDiary.Presentation.Api.Features.Exercises.Requests;
using FoodDiary.Presentation.Api.Features.Exercises.Responses;

namespace FoodDiary.Presentation.Api.Features.Exercises.Mappings;

public static class ExerciseHttpMappings {
    extension(Guid userId) {
        public GetExerciseEntriesQuery ToQuery(DateTime dateFrom, DateTime dateTo) =>
                new(userId, dateFrom, dateTo);

        public DeleteExerciseEntryCommand ToDeleteCommand(Guid entryId) =>
                new(userId, entryId);
    }

    extension(CreateExerciseEntryHttpRequest request) {
        public CreateExerciseEntryCommand ToCommand(Guid userId) =>
                new(userId, request.Date, request.ExerciseType, request.DurationMinutes,
                    request.CaloriesBurned, request.Name, request.Notes);
    }

    extension(UpdateExerciseEntryHttpRequest request) {
        public UpdateExerciseEntryCommand ToCommand(Guid userId, Guid entryId) =>
                new(userId, entryId, request.ExerciseType, request.DurationMinutes,
                    request.CaloriesBurned, request.Name, request.ClearName,
                    request.Notes, request.ClearNotes, request.Date);
    }

    extension(ExerciseEntryModel model) {
        public ExerciseEntryHttpResponse ToHttpResponse() =>
                new(model.Id, model.Date, model.ExerciseType, model.Name,
                    model.DurationMinutes, model.CaloriesBurned, model.Notes);
    }

    extension(IReadOnlyList<ExerciseEntryModel> models) {
        public IReadOnlyList<ExerciseEntryHttpResponse> ToHttpResponse(
        ) =>
                models.Select(m => m.ToHttpResponse()).ToList();
    }
}
