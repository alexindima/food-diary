using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Usda;

internal sealed class UsdaFoodPortionConfiguration : IEntityTypeConfiguration<UsdaFoodPortion> {
    public void Configure(EntityTypeBuilder<UsdaFoodPortion> builder) {
        builder.ToTable("UsdaFoodPortions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Property(e => e.FdcId)
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Property(e => e.Amount)
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Property(e => e.GramWeight)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(e => e.MeasureUnitName).HasMaxLength(UsdaFoodPortion.MeasureUnitNameMaxLength).IsRequired();
        builder.Property(e => e.PortionDescription).HasMaxLength(UsdaFoodPortion.PortionDescriptionMaxLength);
        builder.Property(e => e.Modifier).HasMaxLength(UsdaFoodPortion.ModifierMaxLength);

        builder.HasIndex(e => e.FdcId);

        builder.HasOne(e => e.Food)
            .WithMany(f => f.FoodPortions)
            .HasForeignKey(e => e.FdcId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
