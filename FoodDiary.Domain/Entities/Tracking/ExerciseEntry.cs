using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class ExerciseEntry : AggregateRoot<ExerciseEntryId> {
    private const int NameMaxLength = 256;
    private const int NotesMaxLength = 500;
    private const double MaxCalories = 10_000;
    private const int MaxDurationMinutes = 1440;

    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; }
    public ExerciseType ExerciseType { get; private set; }
    public string? Name { get; private set; }
    public int DurationMinutes { get; private set; }
    public double CaloriesBurned { get; private set; }
    public string? Notes { get; private set; }

    public User User { get; private set; } = null!;

    private ExerciseEntry() {
    }

    public static ExerciseEntry Create(
        UserId userId,
        DateTime date,
        ExerciseType exerciseType,
        int durationMinutes,
        double caloriesBurned,
        string? name = null,
        string? notes = null) {
        EnsureUserId(userId);
        EnsureDuration(durationMinutes);
        EnsureCalories(caloriesBurned);

        var entry = new ExerciseEntry {
            Id = ExerciseEntryId.New(),
            UserId = userId,
            Date = NormalizeDate(date),
            ExerciseType = exerciseType,
            DurationMinutes = durationMinutes,
            CaloriesBurned = Math.Round(caloriesBurned, 1),
            Name = NormalizeOptionalText(name, NameMaxLength, nameof(name)),
            Notes = NormalizeOptionalText(notes, NotesMaxLength, nameof(notes)),
        };
        entry.SetCreated();
        return entry;
    }

    public void Update(
        ExerciseType? exerciseType = null,
        int? durationMinutes = null,
        double? caloriesBurned = null,
        string? name = null,
        bool clearName = false,
        string? notes = null,
        bool clearNotes = false,
        DateTime? date = null) {
        var changed = false;

        if (exerciseType.HasValue && exerciseType.Value != ExerciseType) {
            ExerciseType = exerciseType.Value;
            changed = true;
        }

        if (durationMinutes.HasValue) {
            EnsureDuration(durationMinutes.Value);
            if (durationMinutes.Value != DurationMinutes) {
                DurationMinutes = durationMinutes.Value;
                changed = true;
            }
        }

        if (caloriesBurned.HasValue) {
            EnsureCalories(caloriesBurned.Value);
            var rounded = Math.Round(caloriesBurned.Value, 1);
            if (Math.Abs(rounded - CaloriesBurned) > 0.01) {
                CaloriesBurned = rounded;
                changed = true;
            }
        }

        if (clearName) {
            if (Name is not null) { Name = null; changed = true; }
        } else if (name is not null) {
            var normalized = NormalizeOptionalText(name, NameMaxLength, nameof(name));
            if (Name != normalized) { Name = normalized; changed = true; }
        }

        if (clearNotes) {
            if (Notes is not null) { Notes = null; changed = true; }
        } else if (notes is not null) {
            var normalized = NormalizeOptionalText(notes, NotesMaxLength, nameof(notes));
            if (Notes != normalized) { Notes = normalized; changed = true; }
        }

        if (date.HasValue) {
            var normalizedDate = NormalizeDate(date.Value);
            if (normalizedDate != Date) { Date = normalizedDate; changed = true; }
        }

        if (changed) {
            SetModified();
        }
    }

    private static DateTime NormalizeDate(DateTime date) {
        return date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)
            : date.ToUniversalTime().Date;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : trimmed;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureDuration(int minutes) {
        if (minutes is <= 0 or > MaxDurationMinutes) {
            throw new ArgumentOutOfRangeException(nameof(minutes), $"Duration must be between 1 and {MaxDurationMinutes} minutes.");
        }
    }

    private static void EnsureCalories(double calories) {
        if (double.IsNaN(calories) || double.IsInfinity(calories)) {
            throw new ArgumentOutOfRangeException(nameof(calories), "Calories must be a finite number.");
        }

        if (calories < 0 || calories > MaxCalories) {
            throw new ArgumentOutOfRangeException(nameof(calories), $"Calories must be between 0 and {MaxCalories}.");
        }
    }
}
