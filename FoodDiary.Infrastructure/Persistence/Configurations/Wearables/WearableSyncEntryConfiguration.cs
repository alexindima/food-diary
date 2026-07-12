using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Wearables;


internal sealed class WearableSyncEntryConfiguration : IEntityTypeConfiguration<WearableSyncEntry> {
    public void Configure(EntityTypeBuilder<WearableSyncEntry> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new WearableSyncEntryId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Provider)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.DataType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Date).HasColumnType("date");

        builder.HasIndex(e => new { e.UserId, e.Provider, e.DataType, e.Date }).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.Date });

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
