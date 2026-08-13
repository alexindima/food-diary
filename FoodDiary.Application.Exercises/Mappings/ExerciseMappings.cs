using FoodDiary.Application.Abstractions.Exercises.Models;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Exercises.Mappings;

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
