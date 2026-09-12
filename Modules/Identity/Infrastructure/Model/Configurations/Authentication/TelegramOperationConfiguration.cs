using FoodDiary.Infrastructure.Persistence.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Authentication;

internal sealed class TelegramOperationConfiguration : IEntityTypeConfiguration<TelegramOperation> {
    public void Configure(EntityTypeBuilder<TelegramOperation> builder) {
        builder.ToTable("TelegramOperations");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.BotId, item.UpdateId }).IsUnique();
        builder.HasIndex(item => new { item.BotId, item.Completed, item.NextAttemptAtUtc });
        builder.HasIndex(item => item.UserId);
        builder.Property(item => item.PayloadHash).HasMaxLength(64);
        builder.Property(item => item.ProtectedPayload).HasMaxLength(65536);
        builder.Property(item => item.ProtectedCheckpoint).HasMaxLength(65536);
    }
}
