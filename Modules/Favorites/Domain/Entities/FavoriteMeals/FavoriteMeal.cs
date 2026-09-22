using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;

public sealed class FavoriteMeal : Entity<FavoriteMealId> {
    private const int NameMaxLength = 2048;
    public UserId UserId { get; private set; }
    public MealId MealId { get; private set; }
    public string? Name { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RemovedAtUtc { get; private set; }

    private FavoriteMeal() {
    }

    public static FavoriteMeal Create(UserId userId, MealId mealId, string? name = null) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (mealId == MealId.Empty) {
            throw new ArgumentException("MealId cannot be empty.", nameof(mealId));
        }

        var favorite = new FavoriteMeal {
            Id = FavoriteMealId.New(),
            UserId = userId,
            MealId = mealId,
            Name = NormalizeOptionalText(name),
            CreatedAtUtc = DomainTime.UtcNow,
        };

        favorite.SetCreated();
        return favorite;
    }

    public void Remove() {
        if (RemovedAtUtc is not null) {
            return;
        }
        RemovedAtUtc = DomainTime.UtcNow;
        SetModified();
    }

    public void Restore() {
        if (RemovedAtUtc is null) {
            return;
        }
        RemovedAtUtc = null;
        SetModified();
    }

    public void UpdateName(string? name) {
        string? normalized = NormalizeOptionalText(name);
        if (string.Equals(Name, normalized, StringComparison.Ordinal)) {
            return;
        }

        Name = normalized;
        SetModified();
    }

    private static string? NormalizeOptionalText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length > NameMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), "Name exceeds the maximum length.")
            : trimmed;
    }
}
