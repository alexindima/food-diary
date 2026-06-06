using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class RecentItemConfiguration : IEntityTypeConfiguration<RecentItem> {
    public void Configure(EntityTypeBuilder<RecentItem> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecentItemId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ItemType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.LastUsedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => new { e.UserId, e.ItemType, e.ItemId })
            .IsUnique();

        builder.HasIndex(e => new { e.UserId, e.ItemType, e.LastUsedAtUtc });

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
