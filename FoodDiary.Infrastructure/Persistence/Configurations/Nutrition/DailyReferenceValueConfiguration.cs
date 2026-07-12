using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Nutrition;


internal sealed class DailyReferenceValueConfiguration : IEntityTypeConfiguration<DailyReferenceValue> {
    public void Configure(EntityTypeBuilder<DailyReferenceValue> builder) {
        builder.ToTable("DailyReferenceValues");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Unit).HasMaxLength(32).IsRequired();
        builder.Property(e => e.AgeGroup).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Gender).HasMaxLength(16).IsRequired();

        builder.HasIndex(e => new { e.NutrientId, e.AgeGroup, e.Gender }).IsUnique();

        builder.HasOne(e => e.Nutrient)
            .WithMany()
            .HasForeignKey(e => e.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
