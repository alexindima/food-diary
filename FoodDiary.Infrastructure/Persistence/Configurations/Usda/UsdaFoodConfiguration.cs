using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Usda;

internal sealed class UsdaFoodConfiguration : IEntityTypeConfiguration<UsdaFood> {
    public void Configure(EntityTypeBuilder<UsdaFood> builder) {
        builder.ToTable("UsdaFoods");
        builder.HasKey(e => e.FdcId);
        builder.Property(e => e.FdcId)
            .ValueGeneratedNever()
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        builder.Property(e => e.Description).HasMaxLength(UsdaFood.DescriptionMaxLength).IsRequired();
        builder.Property(e => e.FoodCategoryId)
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Property(e => e.FoodCategory).HasMaxLength(UsdaFood.FoodCategoryMaxLength);

        builder.HasIndex(e => e.Description)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(e => e.FoodCategoryId);

        builder.Navigation(e => e.FoodNutrients)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(e => e.FoodPortions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
