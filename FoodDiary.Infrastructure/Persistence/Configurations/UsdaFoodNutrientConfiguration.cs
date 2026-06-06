using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class UsdaFoodNutrientConfiguration : IEntityTypeConfiguration<UsdaFoodNutrient> {
    public void Configure(EntityTypeBuilder<UsdaFoodNutrient> builder) {
        builder.ToTable("UsdaFoodNutrients");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasIndex(e => new { e.FdcId, e.NutrientId }).IsUnique();
        builder.HasIndex(e => e.NutrientId);

        builder.HasOne(e => e.Food)
            .WithMany(f => f.FoodNutrients)
            .HasForeignKey(e => e.FdcId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Nutrient)
            .WithMany()
            .HasForeignKey(e => e.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
