using FoodDiary.Modules.Exercises.Application.Commands.CreateExerciseEntry;
using FoodDiary.Modules.Exercises.Application.Commands.DeleteExerciseEntry;
using FoodDiary.Modules.Exercises.Application.Commands.UpdateExerciseEntry;
using FoodDiary.Modules.Exercises.Contracts.Models;
using FoodDiary.Modules.Exercises.Application.Queries.GetExerciseEntries;
using FoodDiary.Modules.Exercises.Presentation.Requests;
using FoodDiary.Modules.Exercises.Presentation.Responses;

namespace FoodDiary.Modules.Exercises.Presentation.Mappings;

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
