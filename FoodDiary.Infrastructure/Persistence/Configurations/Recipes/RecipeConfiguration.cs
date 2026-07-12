using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Recipes;


internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe> {
    public void Configure(EntityTypeBuilder<Recipe> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        builder.Property(e => e.Visibility).HasDefaultValue(Visibility.Public);
        builder.Property(e => e.IsNutritionAutoCalculated).HasDefaultValue(value: true);
        builder.Ignore(e => e.UsageCount);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Recipes)
            .HasForeignKey(e => e.UserId);

        builder.HasIndex(e => new { e.UserId, e.CreatedOnUtc });
        builder.HasIndex(e => new { e.Visibility, e.CreatedOnUtc });
        builder.HasIndex(e => e.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Category)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(e => e.Description)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasMany(e => e.MealItems)
            .WithOne(mi => mi.Recipe)
            .HasForeignKey(mi => mi.RecipeId)
            .IsRequired(false);

        builder.Metadata.FindNavigation(nameof(Recipe.Steps))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Recipe.MealItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(Recipe.NestedRecipeUsages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
