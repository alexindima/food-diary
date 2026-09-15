using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Wearables.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Wearables.PersistenceModel.Configurations.Wearables;

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
    }
}
