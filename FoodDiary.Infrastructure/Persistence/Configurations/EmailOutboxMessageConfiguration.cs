using FoodDiary.Infrastructure.Persistence.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage> {
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder) {
        builder.ToTable("EmailOutbox");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.FromAddress)
            .HasMaxLength(320);

        builder.Property(message => message.FromName)
            .HasMaxLength(200);

        builder.Property(message => message.Subject)
            .HasMaxLength(500);

        builder.Property(message => message.LastError)
            .HasMaxLength(2048);

        builder.Property(message => message.LockedBy)
            .HasMaxLength(128);

        builder.HasIndex(message => new { message.ProcessedOnUtc, message.NextAttemptOnUtc, message.LockedUntilUtc })
            .HasDatabaseName("IX_EmailOutbox_DueLease");
    }
}
