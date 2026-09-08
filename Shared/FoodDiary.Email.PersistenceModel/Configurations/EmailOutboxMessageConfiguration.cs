using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Email.Configurations;

internal sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage> {
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder) {
        builder.ToTable("EmailOutbox");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Purpose).HasMaxLength(64).HasDefaultValue("other");
        builder.Property(message => message.ReplyTo).HasMaxLength(320);
        builder.Property(message => message.InReplyTo).HasMaxLength(998);
        builder.Property(message => message.CorrelationId).HasMaxLength(128);

        builder.Property(message => message.FromAddress)
            .HasMaxLength(320);

        builder.Property(message => message.FromName)
            .HasMaxLength(200);

        builder.Property(message => message.Subject)
            .HasMaxLength(500);

        builder.Property(message => message.LastError)
            .HasMaxLength(2048);

        builder.Property(message => message.LockedBy)
            .IsConcurrencyToken()
            .HasMaxLength(128);

        builder.HasIndex(message => new { message.ProcessedOnUtc, message.DeadLetteredOnUtc, message.NextAttemptOnUtc, message.LockedUntilUtc })
            .HasDatabaseName("IX_EmailOutbox_DueLease");
    }
}
