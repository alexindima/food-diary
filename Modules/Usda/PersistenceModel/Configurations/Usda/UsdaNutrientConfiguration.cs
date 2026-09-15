using FoodDiary.Modules.Usda.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Usda.PersistenceModel.Configurations.Usda;

internal sealed class UsdaNutrientConfiguration : IEntityTypeConfiguration<UsdaNutrient> {
    public void Configure(EntityTypeBuilder<UsdaNutrient> builder) {
        builder.ToTable("UsdaNutrients");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(e => e.Name).HasMaxLength(UsdaNutrient.NameMaxLength).IsRequired();
        builder.Property(e => e.UnitName).HasMaxLength(UsdaNutrient.UnitNameMaxLength).IsRequired();
    }
}
