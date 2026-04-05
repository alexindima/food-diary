using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class AiPromptTemplateConfiguration : IEntityTypeConfiguration<AiPromptTemplate> {
    public void Configure(EntityTypeBuilder<AiPromptTemplate> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new AiPromptTemplateId(value));

        entity.Property(e => e.Key).HasMaxLength(64);
        entity.Property(e => e.Locale).HasMaxLength(8);
        entity.Property(e => e.PromptText).HasMaxLength(4096);

        entity.HasIndex(e => new { e.Key, e.Locale }).IsUnique();
        entity.HasIndex(e => new { e.Key, e.IsActive });
    }
}
