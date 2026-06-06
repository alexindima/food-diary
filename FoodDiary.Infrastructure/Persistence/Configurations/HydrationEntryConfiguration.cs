using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class HydrationEntryConfiguration : IEntityTypeConfiguration<HydrationEntry> {
    public void Configure(EntityTypeBuilder<HydrationEntry> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new HydrationEntryId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Timestamp)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.AmountMl)
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.Timestamp })
            .HasDatabaseName("IX_HydrationEntries_User_Timestamp");

        builder.HasOne(e => e.User)
            .WithMany(u => u.HydrationEntries)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
