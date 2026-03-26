using FoodDiary.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate> {
    public void Configure(EntityTypeBuilder<EmailTemplate> entity) {
        entity.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.Locale)
            .IsRequired()
            .HasMaxLength(8);

        entity.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(e => e.HtmlBody)
            .IsRequired()
            .HasColumnType("text");

        entity.Property(e => e.TextBody)
            .IsRequired()
            .HasColumnType("text");

        entity.Property(e => e.IsActive)
            .HasDefaultValue(true);

        entity.HasIndex(e => new { e.Key, e.Locale })
            .IsUnique();
    }
}
