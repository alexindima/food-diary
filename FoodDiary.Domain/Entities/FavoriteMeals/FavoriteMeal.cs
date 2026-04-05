using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.FavoriteMeals;

public sealed class FavoriteMeal : Entity<FavoriteMealId> {
    public UserId UserId { get; private set; }
    public MealId MealId { get; private set; }
    public string? Name { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public User User { get; private set; } = null!;
    public Meal Meal { get; private set; } = null!;

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
            CreatedAtUtc = DomainTime.UtcNow
        };

        favorite.SetCreated();
        return favorite;
    }

    public void UpdateName(string? name) {
        var normalized = NormalizeOptionalText(name);
        if (Name != normalized) {
            Name = normalized;
            SetModified();
        }
    }

    private static string? NormalizeOptionalText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > DomainConstants.CommentMaxLength
            ? trimmed[..DomainConstants.CommentMaxLength]
            : trimmed;
    }
}
