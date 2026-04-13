using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.FavoriteProducts;

public sealed class FavoriteProduct : Entity<FavoriteProductId> {
    public UserId UserId { get; private set; }
    public ProductId ProductId { get; private set; }
    public string? Name { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public User User { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private FavoriteProduct() {
    }

    public static FavoriteProduct Create(UserId userId, ProductId productId, string? name = null) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (productId == ProductId.Empty) {
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));
        }

        var favorite = new FavoriteProduct {
            Id = FavoriteProductId.New(),
            UserId = userId,
            ProductId = productId,
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
