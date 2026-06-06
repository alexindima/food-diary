using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class MealPlanDayConfiguration : IEntityTypeConfiguration<MealPlanDay> {
    public void Configure(EntityTypeBuilder<MealPlanDay> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new MealPlanDayId(value));

        builder.Property(e => e.MealPlanId).HasConversion(
            id => id.Value,
            value => new MealPlanId(value));

        builder.HasIndex(e => new { e.MealPlanId, e.DayNumber }).IsUnique();

        builder.HasMany(e => e.Meals)
            .WithOne(m => m.Day)
            .HasForeignKey(m => m.MealPlanDayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Meals)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
