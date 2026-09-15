using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Recipes.PersistenceModel.Configurations.Recipes;

internal sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient> {
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeIngredientId(value));

        builder.Property(e => e.ProductId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ProductId(value.Value) : null);

        builder.Property(e => e.RecipeStepId).HasConversion(
            id => id.Value,
            value => new RecipeStepId(value));

        builder.Property(e => e.NestedRecipeId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new RecipeId(value.Value) : null);

        builder.HasOne(e => e.RecipeStep)
            .WithMany(s => s.Ingredients)
            .HasForeignKey(e => e.RecipeStepId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.ProductSnapshot);

        builder.HasOne(e => e.NestedRecipe)
            .WithMany(r => r.NestedRecipeUsages)
            .HasForeignKey(e => e.NestedRecipeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
