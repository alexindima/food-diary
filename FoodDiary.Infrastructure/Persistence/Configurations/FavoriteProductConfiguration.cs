using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class FavoriteProductConfiguration : IEntityTypeConfiguration<FavoriteProduct> {
    public void Configure(EntityTypeBuilder<FavoriteProduct> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new FavoriteProductId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ProductId).HasConversion(
            id => id.Value,
            value => new ProductId(value));

        builder.Property(e => e.Name)
            .HasMaxLength(500);

        builder.Property(e => e.PreferredPortionAmount);

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.ProductId })
            .IsUnique();

        builder.HasIndex(e => e.UserId);
    }
}
