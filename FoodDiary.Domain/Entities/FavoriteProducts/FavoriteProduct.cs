using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.FavoriteProducts;

public sealed class FavoriteProduct : Entity<FavoriteProductId> {
    public UserId UserId { get; private set; }
    public ProductId ProductId { get; private set; }
    public string? Name { get; private set; }
    public double? PreferredPortionAmount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public User User { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    private FavoriteProduct() {
    }

    public static FavoriteProduct Create(UserId userId, ProductId productId, string? name = null, double? preferredPortionAmount = null) {
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
            PreferredPortionAmount = NormalizePreferredPortionAmount(preferredPortionAmount),
            CreatedAtUtc = DomainTime.UtcNow,
        };

        favorite.SetCreated();
        return favorite;
    }

    public void UpdateName(string? name) {
        string? normalized = NormalizeOptionalText(name);
        if (!string.Equals(Name, normalized, StringComparison.Ordinal)) {
            Name = normalized;
            SetModified();
        }
    }

    public void UpdatePreferredPortionAmount(double? preferredPortionAmount) {
        double? normalized = NormalizePreferredPortionAmount(preferredPortionAmount);
        if (PreferredPortionAmount != normalized) {
            PreferredPortionAmount = normalized;
            SetModified();
        }
    }

    private static string? NormalizeOptionalText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length > DomainConstants.CommentMaxLength
            ? trimmed[..DomainConstants.CommentMaxLength]
            : trimmed;
    }

    private static double? NormalizePreferredPortionAmount(double? value) {
        if (!value.HasValue) {
            return null;
        }

        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value <= 0) {
            throw new ArgumentOutOfRangeException(nameof(value), "Preferred portion amount must be a positive finite number.");
        }

        return value.Value;
    }
}
