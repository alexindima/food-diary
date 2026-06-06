using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Tracking;

internal sealed class FastingCheckInConfiguration : IEntityTypeConfiguration<FastingCheckIn> {
    public void Configure(EntityTypeBuilder<FastingCheckIn> builder) {
        builder.ToTable("FastingCheckIns");

        builder.Property(checkIn => checkIn.Id)
            .HasConversion(StronglyTypedIdConverters.FastingCheckInIdConverter.Instance);

        builder.Property(checkIn => checkIn.OccurrenceId)
            .HasConversion(StronglyTypedIdConverters.FastingOccurrenceIdConverter.Instance);

        builder.Property(checkIn => checkIn.UserId)
            .HasConversion(StronglyTypedIdConverters.UserIdConverter.Instance);

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
