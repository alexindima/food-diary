using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Exercises.Common;

internal static class ExerciseEntryInputValidation {
    internal const int MaxDurationMinutes = 1440;
    internal const double MaxCaloriesBurned = 10_000;
    internal const int MaxNameLength = 256;
    internal const int MaxNotesLength = 500;
    internal const string DurationErrorMessage = "Duration must be between 1 and 1440 minutes.";
    internal const string CaloriesErrorMessage = "Calories burned must be a finite number between 0 and 10000.";
    internal const string NameErrorMessage = "Name must be at most 256 characters.";
    internal const string NotesErrorMessage = "Notes must be at most 500 characters.";

    internal static bool IsValidDuration(int durationMinutes) =>
        durationMinutes is >= 1 and <= MaxDurationMinutes;

    internal static bool IsValidCalories(double caloriesBurned) =>
        double.IsFinite(caloriesBurned) && caloriesBurned is >= 0 and <= MaxCaloriesBurned;

    internal static bool IsValidOptionalText(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maxLength;

    internal static Error? GetError(
        int? durationMinutes,
        double? caloriesBurned,
        string? name,
        bool clearName,
        string? notes,
        bool clearNotes) {
        if (durationMinutes is { } duration && !IsValidDuration(duration)) {
            return Errors.Validation.Invalid(
                nameof(durationMinutes),
                DurationErrorMessage);
        }

        if (caloriesBurned is { } calories && !IsValidCalories(calories)) {
            return Errors.Validation.Invalid(
                nameof(caloriesBurned),
                CaloriesErrorMessage);
        }

        if (!clearName && !IsValidOptionalText(name, MaxNameLength)) {
            return Errors.Validation.Invalid(nameof(name), NameErrorMessage);
        }

        return !clearNotes && !IsValidOptionalText(notes, MaxNotesLength)
            ? Errors.Validation.Invalid(nameof(notes), NotesErrorMessage)
            : null;
    }
}
