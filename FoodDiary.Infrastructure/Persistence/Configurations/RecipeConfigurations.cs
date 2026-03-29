using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe> {
    public void Configure(EntityTypeBuilder<Recipe> entity) {
        entity.Property<uint>("xmin").IsRowVersion();

        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        entity.Property(e => e.Visibility).HasDefaultValue(Visibility.Public);
        entity.Property(e => e.IsNutritionAutoCalculated).HasDefaultValue(true);
        entity.Ignore(e => e.UsageCount);

        entity.HasOne(e => e.User)
            .WithMany(u => u.Recipes)
            .HasForeignKey(e => e.UserId);

        entity.HasIndex(e => new { e.UserId, e.CreatedOnUtc });
        entity.HasIndex(e => new { e.Visibility, e.CreatedOnUtc });

        entity.HasMany(e => e.MealItems)
            .WithOne(mi => mi.Recipe)
            .HasForeignKey(mi => mi.RecipeId)
            .IsRequired(false);

        entity.Metadata.FindNavigation(nameof(Recipe.Steps))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        entity.Metadata.FindNavigation(nameof(Recipe.MealItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        entity.Metadata.FindNavigation(nameof(Recipe.NestedRecipeUsages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        entity.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep> {
    public void Configure(EntityTypeBuilder<RecipeStep> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeStepId(value));

        entity.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        entity.HasOne(e => e.Recipe)
            .WithMany(r => r.Steps)
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        entity.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient> {
    public void Configure(EntityTypeBuilder<RecipeIngredient> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeIngredientId(value));

        entity.Property(e => e.ProductId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ProductId(value.Value) : null);

        entity.Property(e => e.RecipeStepId).HasConversion(
            id => id.Value,
            value => new RecipeStepId(value));

        entity.Property(e => e.NestedRecipeId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new RecipeId(value.Value) : null);

        entity.HasOne(e => e.RecipeStep)
            .WithMany(s => s.Ingredients)
            .HasForeignKey(e => e.RecipeStepId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Product)
            .WithMany(p => p.RecipeIngredients)
            .HasForeignKey(e => e.ProductId)
            .IsRequired(false);

        entity.HasOne(e => e.NestedRecipe)
            .WithMany(r => r.NestedRecipeUsages)
            .HasForeignKey(e => e.NestedRecipeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
