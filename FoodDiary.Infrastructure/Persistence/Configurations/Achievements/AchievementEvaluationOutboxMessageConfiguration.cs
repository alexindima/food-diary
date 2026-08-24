using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Achievements;

internal sealed class AchievementEvaluationOutboxMessageConfiguration : IEntityTypeConfiguration<AchievementEvaluationOutboxMessage> {
    public void Configure(EntityTypeBuilder<AchievementEvaluationOutboxMessage> builder) {
        builder.ToTable("AchievementEvaluationOutbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(message => message.LastError).HasMaxLength(2048);
        builder.Property(message => message.LockedBy).HasMaxLength(128);
        builder.Property(message => message.Revision);
        builder.HasIndex(message => new { message.ProcessedOnUtc, message.DeadLetteredOnUtc, message.NextAttemptOnUtc, message.LockedUntilUtc })
            .HasDatabaseName("IX_AchievementEvaluationOutbox_DueLease");
        builder.HasIndex(message => message.UserId).IsUnique();
    }
}
