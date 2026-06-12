using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class MealItemConfiguration : IEntityTypeConfiguration<MealItem> {
    public void Configure(EntityTypeBuilder<MealItem> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MealItemId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.MealId).HasConversion(
            id => id.Value,
            value => new MealId(value));

        builder.Property(e => e.ProductId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ProductId(value.Value) : null);

        builder.Property(e => e.RecipeId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new RecipeId(value.Value) : null);

        builder.Property(e => e.SourceAiItemId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new MealAiItemId(value.Value) : null);

        builder.Property(e => e.Origin)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(FoodDiary.Domain.Enums.MealItemOrigin.Manual);

        builder.Property(e => e.SnapshotName)
            .HasMaxLength(256);

        builder.Property(e => e.SnapshotImageUrl)
            .HasMaxLength(2048);

        builder.Property(e => e.SnapshotUnit)
            .HasMaxLength(32);

        builder.HasOne(e => e.Meal)
            .WithMany(m => m.Items)
            .HasForeignKey(e => e.MealId);

        builder.HasOne(e => e.Product)
            .WithMany(p => p.MealItems)
            .HasForeignKey(e => e.ProductId)
            .IsRequired(false);

        builder.HasOne(e => e.Recipe)
            .WithMany(r => r.MealItems)
            .HasForeignKey(e => e.RecipeId)
            .IsRequired(false);
    }
}
