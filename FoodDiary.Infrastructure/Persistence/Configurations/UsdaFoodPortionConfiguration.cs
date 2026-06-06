using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class UsdaFoodPortionConfiguration : IEntityTypeConfiguration<UsdaFoodPortion> {
    public void Configure(EntityTypeBuilder<UsdaFoodPortion> builder) {
        builder.ToTable("UsdaFoodPortions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.MeasureUnitName).HasMaxLength(128);
        builder.Property(e => e.PortionDescription).HasMaxLength(256);
        builder.Property(e => e.Modifier).HasMaxLength(128);

        builder.HasIndex(e => e.FdcId);

        builder.HasOne(e => e.Food)
            .WithMany(f => f.FoodPortions)
            .HasForeignKey(e => e.FdcId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
