using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Wearables;


internal sealed class WearableConnectionConfiguration : IEntityTypeConfiguration<WearableConnection> {
    public void Configure(EntityTypeBuilder<WearableConnection> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new WearableConnectionId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Provider)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.ExternalUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.AccessToken).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.RefreshToken).HasMaxLength(2048);

        builder.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
