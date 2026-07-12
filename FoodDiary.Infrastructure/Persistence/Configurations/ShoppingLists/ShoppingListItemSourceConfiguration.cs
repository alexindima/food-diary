using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.ShoppingLists;

internal sealed class ShoppingListItemSourceConfiguration : IEntityTypeConfiguration<ShoppingListItemSource> {
    public void Configure(EntityTypeBuilder<ShoppingListItemSource> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new ShoppingListItemSourceId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.ShoppingListItemId).HasConversion(
            id => id.Value,
            value => new ShoppingListItemId(value));

        builder.Property(e => e.MealPlanId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new MealPlanId(value.Value) : null);

        builder.Property(e => e.MealPlanMealId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new MealPlanMealId(value.Value) : null);

        builder.Property(e => e.RecipeId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new RecipeId(value.Value) : null);

        builder.Property(e => e.SourceType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Label)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.MealType)
            .HasMaxLength(256);

        builder.HasIndex(e => e.ShoppingListItemId);
        builder.HasIndex(e => e.MealPlanId);
        builder.HasIndex(e => e.RecipeId);
    }
}
