using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Favorites;


internal sealed class FavoriteMealConfiguration : IEntityTypeConfiguration<FavoriteMeal> {
    public void Configure(EntityTypeBuilder<FavoriteMeal> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new FavoriteMealId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.MealId).HasConversion(
            id => id.Value,
            value => new MealId(value));

        builder.Property(e => e.Name)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Meal)
            .WithMany()
            .HasForeignKey(e => e.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.MealId })
            .IsUnique();

        builder.HasIndex(e => e.UserId);
    }
}
