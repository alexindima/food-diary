using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Fasting.Infrastructure.Persistence.Configurations;

internal sealed class FastingCheckInConfiguration : IEntityTypeConfiguration<FastingCheckIn> {
    public void Configure(EntityTypeBuilder<FastingCheckIn> builder) {
        builder.ToTable("FastingCheckIns");

        builder.Property(checkIn => checkIn.Id)
            .HasConversion(id => id.Value, value => new FastingCheckInId(value));

        builder.Property(checkIn => checkIn.OccurrenceId)
            .HasConversion(id => id.Value, value => new FastingOccurrenceId(value));

        builder.Property(checkIn => checkIn.UserId)
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(checkIn => checkIn.Symptoms)
            .HasMaxLength(200);

        builder.Property(checkIn => checkIn.Notes)
            .HasMaxLength(500);

        builder.HasIndex(checkIn => new { checkIn.OccurrenceId, checkIn.CheckedInAtUtc });
        builder.HasIndex(checkIn => new { checkIn.UserId, checkIn.CheckedInAtUtc });

        builder.HasOne(checkIn => checkIn.Occurrence)
            .WithMany()
            .HasForeignKey(checkIn => checkIn.OccurrenceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(checkIn => checkIn.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
