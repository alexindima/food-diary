using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class WearableConnectionConfiguration : IEntityTypeConfiguration<WearableConnection> {
    public void Configure(EntityTypeBuilder<WearableConnection> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new WearableConnectionId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Provider)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(e => e.ExternalUserId).HasMaxLength(256).IsRequired();
        entity.Property(e => e.AccessToken).HasMaxLength(2048).IsRequired();
        entity.Property(e => e.RefreshToken).HasMaxLength(2048);

        entity.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WearableSyncEntryConfiguration : IEntityTypeConfiguration<WearableSyncEntry> {
    public void Configure(EntityTypeBuilder<WearableSyncEntry> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new WearableSyncEntryId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Provider)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(e => e.DataType)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(e => e.Date).HasColumnType("date");

        entity.HasIndex(e => new { e.UserId, e.Provider, e.DataType, e.Date }).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.Date });

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
