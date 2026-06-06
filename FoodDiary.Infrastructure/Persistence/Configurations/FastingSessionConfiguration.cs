using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class FastingSessionConfiguration : IEntityTypeConfiguration<FastingSession> {
    public void Configure(EntityTypeBuilder<FastingSession> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new FastingSessionId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Protocol)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.IsCompleted });
    }
}
