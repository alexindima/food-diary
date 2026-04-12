using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Tracking;

internal sealed class FastingCheckInConfiguration : IEntityTypeConfiguration<FastingCheckIn> {
    public void Configure(EntityTypeBuilder<FastingCheckIn> entity) {
        entity.ToTable("FastingCheckIns");

        entity.Property(checkIn => checkIn.Id)
            .HasConversion(StronglyTypedIdConverters.FastingCheckInIdConverter.Instance);

        entity.Property(checkIn => checkIn.OccurrenceId)
            .HasConversion(StronglyTypedIdConverters.FastingOccurrenceIdConverter.Instance);

        entity.Property(checkIn => checkIn.UserId)
            .HasConversion(StronglyTypedIdConverters.UserIdConverter.Instance);

        entity.Property(checkIn => checkIn.Symptoms)
            .HasMaxLength(200);

        entity.Property(checkIn => checkIn.Notes)
            .HasMaxLength(500);

        entity.HasIndex(checkIn => new { checkIn.OccurrenceId, checkIn.CheckedInAtUtc });
        entity.HasIndex(checkIn => new { checkIn.UserId, checkIn.CheckedInAtUtc });

        entity.HasOne(checkIn => checkIn.Occurrence)
            .WithMany()
            .HasForeignKey(checkIn => checkIn.OccurrenceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(checkIn => checkIn.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
