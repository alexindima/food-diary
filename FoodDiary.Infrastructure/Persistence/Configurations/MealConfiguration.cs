using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class MealConfiguration : IEntityTypeConfiguration<Meal> {
    public void Configure(EntityTypeBuilder<Meal> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MealId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Meals)
            .HasForeignKey(e => e.UserId);
        builder.Property(e => e.IsNutritionAutoCalculated).HasDefaultValue(value: true);

        builder.HasIndex(e => new { e.UserId, e.Date, e.CreatedOnUtc });

        builder.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.AiSessions)
            .WithOne(s => s.Meal)
            .HasForeignKey(s => s.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.AiSessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
