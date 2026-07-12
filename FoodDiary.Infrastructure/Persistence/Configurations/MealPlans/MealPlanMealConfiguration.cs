using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.MealPlans;


internal sealed class MealPlanMealConfiguration : IEntityTypeConfiguration<MealPlanMeal> {
    public void Configure(EntityTypeBuilder<MealPlanMeal> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new MealPlanMealId(value));

        builder.Property(e => e.MealPlanDayId).HasConversion(
            id => id.Value,
            value => new MealPlanDayId(value));

        builder.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        builder.Property(e => e.MealType)
            .HasConversion<string>();

        builder.HasOne(e => e.Recipe)
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
