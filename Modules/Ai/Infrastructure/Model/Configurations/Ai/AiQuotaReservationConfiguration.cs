using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Ai;

internal sealed class AiQuotaReservationConfiguration : IEntityTypeConfiguration<AiQuotaReservation> {
    public void Configure(EntityTypeBuilder<AiQuotaReservation> builder) {
        builder.HasKey(x => x.RequestId);
        builder.Property(x => x.RequestId).HasMaxLength(64);
        builder.Property(x => x.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(x => x.Operation).HasMaxLength(32);
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(16);
        builder.HasOne<AiQuotaPeriod>()
            .WithMany()
            .HasForeignKey(x => new { x.UserId, x.PeriodStartUtc })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.PeriodStartUtc, x.State, x.ExpiresOnUtc });
        builder.ToTable(table => {
            table.HasCheckConstraint("CK_AiQuotaReservations_ReservedInputTokens", "\"ReservedInputTokens\" >= 0");
            table.HasCheckConstraint("CK_AiQuotaReservations_ReservedOutputTokens", "\"ReservedOutputTokens\" >= 0");
        });
    }
}
