using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class UsdaFoodConfiguration : IEntityTypeConfiguration<UsdaFood> {
    public void Configure(EntityTypeBuilder<UsdaFood> builder) {
        builder.ToTable("UsdaFoods");
        builder.HasKey(e => e.FdcId);
        builder.Property(e => e.FdcId).ValueGeneratedNever();

        builder.Property(e => e.Description).HasMaxLength(512).IsRequired();
        builder.Property(e => e.FoodCategory).HasMaxLength(256);

        builder.HasIndex(e => e.Description)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(e => e.FoodCategoryId);
    }
}
