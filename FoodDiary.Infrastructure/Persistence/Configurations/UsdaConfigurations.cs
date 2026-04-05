using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class UsdaFoodConfiguration : IEntityTypeConfiguration<UsdaFood> {
    public void Configure(EntityTypeBuilder<UsdaFood> entity) {
        entity.ToTable("UsdaFoods");
        entity.HasKey(e => e.FdcId);
        entity.Property(e => e.FdcId).ValueGeneratedNever();

        entity.Property(e => e.Description).HasMaxLength(512).IsRequired();
        entity.Property(e => e.FoodCategory).HasMaxLength(256);

        entity.HasIndex(e => e.Description)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        entity.HasIndex(e => e.FoodCategoryId);
    }
}

internal sealed class UsdaNutrientConfiguration : IEntityTypeConfiguration<UsdaNutrient> {
    public void Configure(EntityTypeBuilder<UsdaNutrient> entity) {
        entity.ToTable("UsdaNutrients");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
        entity.Property(e => e.UnitName).HasMaxLength(32).IsRequired();
    }
}

internal sealed class UsdaFoodNutrientConfiguration : IEntityTypeConfiguration<UsdaFoodNutrient> {
    public void Configure(EntityTypeBuilder<UsdaFoodNutrient> entity) {
        entity.ToTable("UsdaFoodNutrients");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.HasIndex(e => new { e.FdcId, e.NutrientId }).IsUnique();
        entity.HasIndex(e => e.NutrientId);

        entity.HasOne(e => e.Food)
            .WithMany(f => f.FoodNutrients)
            .HasForeignKey(e => e.FdcId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Nutrient)
            .WithMany()
            .HasForeignKey(e => e.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UsdaFoodPortionConfiguration : IEntityTypeConfiguration<UsdaFoodPortion> {
    public void Configure(EntityTypeBuilder<UsdaFoodPortion> entity) {
        entity.ToTable("UsdaFoodPortions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();

        entity.Property(e => e.MeasureUnitName).HasMaxLength(128);
        entity.Property(e => e.PortionDescription).HasMaxLength(256);
        entity.Property(e => e.Modifier).HasMaxLength(128);

        entity.HasIndex(e => e.FdcId);

        entity.HasOne(e => e.Food)
            .WithMany(f => f.FoodPortions)
            .HasForeignKey(e => e.FdcId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DailyReferenceValueConfiguration : IEntityTypeConfiguration<DailyReferenceValue> {
    public void Configure(EntityTypeBuilder<DailyReferenceValue> entity) {
        entity.ToTable("DailyReferenceValues");
        entity.HasKey(e => e.Id);

        entity.Property(e => e.Unit).HasMaxLength(32).IsRequired();
        entity.Property(e => e.AgeGroup).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Gender).HasMaxLength(16).IsRequired();

        entity.HasIndex(e => new { e.NutrientId, e.AgeGroup, e.Gender }).IsUnique();

        entity.HasOne(e => e.Nutrient)
            .WithMany()
            .HasForeignKey(e => e.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
