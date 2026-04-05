using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using UserIdType = FoodDiary.Domain.ValueObjects.Ids.UserId;

namespace FoodDiary.Domain.Entities.MealPlans;

public sealed class MealPlan : AggregateRoot<MealPlanId> {
    private const int NameMaxLength = 256;
    private const int DescriptionMaxLength = 2048;
    private const int MaxDurationDays = 31;

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DietType DietType { get; private set; }
    public int DurationDays { get; private set; }
    public double? TargetCaloriesPerDay { get; private set; }
    public bool IsCurated { get; private set; }
    public UserId? UserId { get; private set; }
    public User? User { get; private set; }

    private readonly List<MealPlanDay> _days = [];
    public IReadOnlyCollection<MealPlanDay> Days => _days.AsReadOnly();

    private MealPlan() {
    }

    public static MealPlan CreateCurated(
        string name,
        string? description,
        DietType dietType,
        int durationDays,
        double? targetCaloriesPerDay) {
        var plan = new MealPlan {
            Id = MealPlanId.New(),
            Name = NormalizeName(name),
            Description = NormalizeDescription(description),
            DietType = dietType,
            DurationDays = NormalizeDuration(durationDays),
            TargetCaloriesPerDay = targetCaloriesPerDay,
            IsCurated = true,
            UserId = null
        };
        plan.SetCreated();
        return plan;
    }

    public static MealPlan CreateForUser(
        UserIdType userId,
        string name,
        string? description,
        DietType dietType,
        int durationDays,
        double? targetCaloriesPerDay) {
        if (userId == UserIdType.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var plan = new MealPlan {
            Id = MealPlanId.New(),
            Name = NormalizeName(name),
            Description = NormalizeDescription(description),
            DietType = dietType,
            DurationDays = NormalizeDuration(durationDays),
            TargetCaloriesPerDay = targetCaloriesPerDay,
            IsCurated = false,
            UserId = userId
        };
        plan.SetCreated();
        return plan;
    }

    public MealPlanDay AddDay(int dayNumber) {
        if (_days.Any(d => d.DayNumber == dayNumber)) {
            throw new InvalidOperationException($"Day {dayNumber} already exists in this plan.");
        }

        var day = MealPlanDay.Create(Id, dayNumber);
        _days.Add(day);
        return day;
    }

    public MealPlan Adopt(UserIdType userId) {
        if (userId == UserIdType.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var adopted = new MealPlan {
            Id = MealPlanId.New(),
            Name = Name,
            Description = Description,
            DietType = DietType,
            DurationDays = DurationDays,
            TargetCaloriesPerDay = TargetCaloriesPerDay,
            IsCurated = false,
            UserId = userId
        };
        adopted.SetCreated();

        foreach (var sourceDay in _days.OrderBy(d => d.DayNumber)) {
            var newDay = adopted.AddDay(sourceDay.DayNumber);
            foreach (var sourceMeal in sourceDay.Meals) {
                newDay.AddMeal(sourceMeal.MealType, sourceMeal.RecipeId, sourceMeal.Servings);
            }
        }

        return adopted;
    }

    private static string NormalizeName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Name is required.", nameof(value));
        }

        var normalized = value.Trim();
        return normalized.Length > NameMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Name must be at most {NameMaxLength} characters.")
            : normalized;
    }

    private static string? NormalizeDescription(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > DescriptionMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Description must be at most {DescriptionMaxLength} characters.")
            : normalized;
    }

    private static int NormalizeDuration(int value) {
        return value is <= 0 or > MaxDurationDays
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Duration must be between 1 and {MaxDurationDays} days.")
            : value;
    }
}
