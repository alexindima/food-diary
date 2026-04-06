using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Social;

public sealed class RecipeLike : Entity<RecipeLikeId> {
    public UserId UserId { get; private set; }
    public RecipeId RecipeId { get; private set; }

    public User User { get; private set; } = null!;

    private RecipeLike() {
    }

    public static RecipeLike Create(UserId userId, RecipeId recipeId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (recipeId == RecipeId.Empty) {
            throw new ArgumentException("RecipeId is required.", nameof(recipeId));
        }

        var like = new RecipeLike {
            Id = RecipeLikeId.New(),
            UserId = userId,
            RecipeId = recipeId,
        };
        like.SetCreated();
        return like;
    }
}
