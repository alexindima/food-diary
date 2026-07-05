using FoodDiary.Infrastructure.Persistence.Images;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class ImageObjectDeletionOutboxMessageConfiguration : IEntityTypeConfiguration<ImageObjectDeletionOutboxMessage> {
    public void Configure(EntityTypeBuilder<ImageObjectDeletionOutboxMessage> builder) {
        builder.ToTable("ImageObjectDeletionOutbox");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.ObjectKey)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(message => message.LastError)
            .HasMaxLength(2048);

        builder.Property(message => message.LockedBy)
            .HasMaxLength(128);

        builder.HasIndex(message => new { message.ProcessedOnUtc, message.NextAttemptOnUtc, message.LockedUntilUtc })
            .HasDatabaseName("IX_ImageObjectDeletionOutbox_DueLease");
    }
}
