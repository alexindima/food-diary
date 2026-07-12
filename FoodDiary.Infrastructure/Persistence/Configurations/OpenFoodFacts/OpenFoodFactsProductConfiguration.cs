using FoodDiary.Domain.Entities.OpenFoodFacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.OpenFoodFacts;


internal sealed class OpenFoodFactsProductConfiguration : IEntityTypeConfiguration<OpenFoodFactsProduct> {
    public void Configure(EntityTypeBuilder<OpenFoodFactsProduct> builder) {
        builder.ToTable("OpenFoodFactsProducts");

        builder.HasKey(e => e.Barcode);

        builder.Property(e => e.Barcode)
            .HasMaxLength(64);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Brand)
            .HasMaxLength(512);

        builder.Property(e => e.Category)
            .HasMaxLength(1024);

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(e => e.LastSyncedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.LastSeenAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(e => e.Brand)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(e => e.Category)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(e => e.LastSeenAtUtc);
    }
}
