using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence.Configurations;

internal sealed class WaistEntryConfiguration : IEntityTypeConfiguration<WaistEntry> {
    public void Configure(EntityTypeBuilder<WaistEntry> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new WaistEntryId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Date)
            .HasColumnType("date");

        builder.HasIndex(e => new { e.UserId, e.Date }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
