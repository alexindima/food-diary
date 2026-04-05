using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan> {
    public void Configure(EntityTypeBuilder<MealPlan> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new MealPlanId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id!.Value.Value,
            value => new UserId(value));

        entity.Property(e => e.DietType)
            .HasConversion<string>();

        entity.Property(e => e.Name).HasMaxLength(256);
        entity.Property(e => e.Description).HasMaxLength(2048);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        entity.HasMany(e => e.Days)
            .WithOne(d => d.MealPlan)
            .HasForeignKey(d => d.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(e => e.Days)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        entity.HasIndex(e => e.IsCurated);
        entity.HasIndex(e => e.DietType);

        entity.Property<uint>("xmin").IsRowVersion();
    }
}

internal sealed class MealPlanDayConfiguration : IEntityTypeConfiguration<MealPlanDay> {
    public void Configure(EntityTypeBuilder<MealPlanDay> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new MealPlanDayId(value));

        entity.Property(e => e.MealPlanId).HasConversion(
            id => id.Value,
            value => new MealPlanId(value));

        entity.HasIndex(e => new { e.MealPlanId, e.DayNumber }).IsUnique();

        entity.HasMany(e => e.Meals)
            .WithOne(m => m.Day)
            .HasForeignKey(m => m.MealPlanDayId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(e => e.Meals)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MealPlanMealConfiguration : IEntityTypeConfiguration<MealPlanMeal> {
    public void Configure(EntityTypeBuilder<MealPlanMeal> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new MealPlanMealId(value));

        entity.Property(e => e.MealPlanDayId).HasConversion(
            id => id.Value,
            value => new MealPlanDayId(value));

        entity.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        entity.Property(e => e.MealType)
            .HasConversion<string>();

        entity.HasOne(e => e.Recipe)
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
