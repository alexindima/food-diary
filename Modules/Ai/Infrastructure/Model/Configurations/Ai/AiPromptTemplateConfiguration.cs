using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Ai;

internal sealed class AiPromptTemplateConfiguration : IEntityTypeConfiguration<AiPromptTemplate> {
    public void Configure(EntityTypeBuilder<AiPromptTemplate> builder) {
        builder.OwnsMany(e => e.Revisions, revisions => {
            revisions.ToTable("AiPromptRevisions");
            revisions.WithOwner().HasForeignKey("TemplateId");
            revisions.HasKey(e => e.Id);
            revisions.Property(e => e.Id).ValueGeneratedNever();
            revisions.Property(e => e.PromptText).HasMaxLength(4096).IsRequired();
            revisions.HasIndex("TemplateId", nameof(AiPromptRevision.ArchivedOnUtc));
        });
        builder.Navigation(e => e.Revisions).AutoInclude(autoInclude: false);
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new AiPromptTemplateId(value));

        builder.Property(e => e.Key).HasMaxLength(64);
        builder.Property(e => e.Locale).HasMaxLength(8);
        builder.Property(e => e.PromptText).HasMaxLength(4096);

        builder.HasIndex(e => new { e.Key, e.Locale }).IsUnique();
        builder.HasIndex(e => new { e.Key, e.IsActive });
    }
}
