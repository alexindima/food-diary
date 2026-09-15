using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using FoodDiary.Modules.Exercises.Application.Abstractions.Models;
using FoodDiary.Modules.Exercises.Contracts.Models;

namespace FoodDiary.Modules.Exercises.Application.Mappings;

public static class ExerciseMappings {
    public static ExerciseEntryModel ToModel(this ExerciseEntry entry) {
        return new ExerciseEntryModel(
            entry.Id.Value,
            entry.Date,
            entry.ExerciseType.ToString(),
            entry.Name,
            entry.DurationMinutes,
            entry.CaloriesBurned,
            entry.Notes);
    }

    public static ExerciseEntryModel ToModel(this ExerciseEntryReadModel entry) {
        return new ExerciseEntryModel(
            entry.Id,
            entry.Date,
            entry.ExerciseType,
            entry.Name,
            entry.DurationMinutes,
            entry.CaloriesBurned,
            entry.Notes);
    }
}
