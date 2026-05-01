using FoodDiary.Domain.Entities.OpenFoodFacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class OpenFoodFactsProductConfiguration : IEntityTypeConfiguration<OpenFoodFactsProduct> {
    public void Configure(EntityTypeBuilder<OpenFoodFactsProduct> entity) {
        entity.ToTable("OpenFoodFactsProducts");

        entity.HasKey(e => e.Barcode);

        entity.Property(e => e.Barcode)
            .HasMaxLength(64);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(e => e.Brand)
            .HasMaxLength(512);

        entity.Property(e => e.Category)
            .HasMaxLength(1024);

        entity.Property(e => e.ImageUrl)
            .HasMaxLength(2048);

        entity.Property(e => e.LastSyncedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.LastSeenAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.HasIndex(e => e.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        entity.HasIndex(e => e.Brand)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        entity.HasIndex(e => e.Category)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        entity.HasIndex(e => e.LastSeenAtUtc);
    }
}
