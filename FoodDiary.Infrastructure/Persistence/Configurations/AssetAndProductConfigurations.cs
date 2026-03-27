using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class ImageAssetConfiguration : IEntityTypeConfiguration<ImageAsset> {
    public void Configure(EntityTypeBuilder<ImageAsset> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new ImageAssetId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.ObjectKey).IsRequired();
        entity.Property(e => e.Url).IsRequired();

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product> {
    public void Configure(EntityTypeBuilder<Product> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new ProductId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        entity.Property(e => e.Visibility).HasDefaultValue(Visibility.Public);
        entity.Property(e => e.ProductType).HasDefaultValue(ProductType.Unknown);
        entity.Ignore(e => e.UsageCount);

        entity.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasOne(e => e.User)
            .WithMany(u => u.Products)
            .HasForeignKey(e => e.UserId);

        entity.Metadata.FindNavigation(nameof(Product.MealItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        entity.Metadata.FindNavigation(nameof(Product.RecipeIngredients))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RecentItemConfiguration : IEntityTypeConfiguration<RecentItem> {
    public void Configure(EntityTypeBuilder<RecentItem> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecentItemId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.ItemType)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(e => e.LastUsedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.HasIndex(e => new { e.UserId, e.ItemType, e.ItemId })
            .IsUnique();

        entity.HasIndex(e => new { e.UserId, e.ItemType, e.LastUsedAtUtc });

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
