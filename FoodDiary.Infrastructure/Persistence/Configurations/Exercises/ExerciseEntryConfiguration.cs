using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Exercises;


internal sealed class ExerciseEntryConfiguration : IEntityTypeConfiguration<ExerciseEntry> {
    public void Configure(EntityTypeBuilder<ExerciseEntry> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new ExerciseEntryId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Date)
            .HasColumnType("date");

        builder.Property(e => e.ExerciseType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Name)
            .HasMaxLength(256);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasIndex(e => new { e.UserId, e.Date });

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
