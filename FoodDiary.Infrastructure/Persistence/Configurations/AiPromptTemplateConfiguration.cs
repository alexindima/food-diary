using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class AiPromptTemplateConfiguration : IEntityTypeConfiguration<AiPromptTemplate> {
    public void Configure(EntityTypeBuilder<AiPromptTemplate> builder) {
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
