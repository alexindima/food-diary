using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class UsdaNutrientConfiguration : IEntityTypeConfiguration<UsdaNutrient> {
    public void Configure(EntityTypeBuilder<UsdaNutrient> builder) {
        builder.ToTable("UsdaNutrients");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.UnitName).HasMaxLength(32).IsRequired();
    }
}
