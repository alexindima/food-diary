using FoodDiary.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Email;

internal sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate> {
    public void Configure(EntityTypeBuilder<EmailTemplate> builder) {
        builder.OwnsMany(e => e.Revisions, revisions => {
            revisions.ToTable("EmailTemplateRevisions");
            revisions.WithOwner().HasForeignKey("TemplateId");
            revisions.HasKey(e => e.Id);
            revisions.Property(e => e.Id).ValueGeneratedNever();
            revisions.Property(e => e.Subject).HasMaxLength(256).IsRequired();
            revisions.Property(e => e.HtmlBody).IsRequired();
            revisions.Property(e => e.TextBody).IsRequired();
            revisions.HasIndex("TemplateId", nameof(EmailTemplateRevision.ArchivedOnUtc));
        });
        builder.Navigation(e => e.Revisions).AutoInclude(autoInclude: false);
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
