using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Ai;

internal sealed class AiQuotaPeriodConfiguration : IEntityTypeConfiguration<AiQuotaPeriod> {
    public void Configure(EntityTypeBuilder<AiQuotaPeriod> builder) {
        builder.HasKey(x => new { x.UserId, x.PeriodStartUtc });
        builder.Property(x => x.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.HasOne<global::FoodDiary.Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table => {
            table.HasCheckConstraint("CK_AiQuotaPeriods_ConsumedInputTokens", "\"ConsumedInputTokens\" >= 0");
            table.HasCheckConstraint("CK_AiQuotaPeriods_ConsumedOutputTokens", "\"ConsumedOutputTokens\" >= 0");
            table.HasCheckConstraint("CK_AiQuotaPeriods_ReservedInputTokens", "\"ReservedInputTokens\" >= 0");
            table.HasCheckConstraint("CK_AiQuotaPeriods_ReservedOutputTokens", "\"ReservedOutputTokens\" >= 0");
        });
    }
}
