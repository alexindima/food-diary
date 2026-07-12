using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.MealPlans;


internal sealed class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan> {
    public void Configure(EntityTypeBuilder<MealPlan> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new MealPlanId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id!.Value.Value,
            value => new UserId(value));

        builder.Property(e => e.DietType)
            .HasConversion<string>();

        builder.Property(e => e.Name).HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(2048);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasMany(e => e.Days)
            .WithOne(d => d.MealPlan)
            .HasForeignKey(d => d.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Days)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.IsCurated);
        builder.HasIndex(e => e.DietType);

        builder.Property<uint>("xmin").IsRowVersion();
    }
}
