using FoodDiary.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate> {
    public void Configure(EntityTypeBuilder<EmailTemplate> builder) {
        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.Locale)
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.HtmlBody)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.TextBody)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(value: true);

        builder.HasIndex(e => new { e.Key, e.Locale })
            .IsUnique();
    }
}
