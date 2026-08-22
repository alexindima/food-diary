using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Hydration;

internal sealed class HydrationEntryConfiguration : IEntityTypeConfiguration<HydrationEntry> {
    public void Configure(EntityTypeBuilder<HydrationEntry> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

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

        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_HydrationEntries_AmountMl",
                "\"AmountMl\" > 0 AND \"AmountMl\" <= 10000"));

        builder.HasIndex(e => new { e.UserId, e.Timestamp })
            .IsUnique()
            .HasDatabaseName("IX_HydrationEntries_User_Timestamp");

        builder.HasOne(e => e.User)
            .WithMany(u => u.HydrationEntries)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
