using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Products;


internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product> {
    public void Configure(EntityTypeBuilder<Product> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new ProductId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        builder.Property(e => e.Visibility).HasDefaultValue(Visibility.Public);
        builder.Property(e => e.ProductType).HasDefaultValue(ProductType.Unknown);
        builder.Ignore(e => e.UsageCount);

        builder.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Products)
            .HasForeignKey(e => e.UserId);

        builder.HasOne(e => e.UsdaFood)
            .WithMany()
            .HasForeignKey(e => e.UsdaFdcId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.UserId, e.CreatedOnUtc });
        builder.HasIndex(e => new { e.Visibility, e.CreatedOnUtc });
        builder.HasIndex(e => e.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Brand)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Category)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Barcode)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.Metadata.FindNavigation(nameof(Product.MealItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Product.RecipeIngredients))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
