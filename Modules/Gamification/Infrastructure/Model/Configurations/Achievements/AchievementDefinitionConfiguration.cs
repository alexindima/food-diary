using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Achievements;

internal sealed class AchievementDefinitionConfiguration : IEntityTypeConfiguration<AchievementDefinition> {
    public void Configure(EntityTypeBuilder<AchievementDefinition> builder) {
        builder.ToTable("AchievementDefinitions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasConversion(id => id.Value, value => new AchievementDefinitionId(value)).ValueGeneratedNever();
        builder.Property(item => item.Key).IsRequired().HasMaxLength(AchievementDefinition.KeyMaxLength);
        builder.Property(item => item.Category).IsRequired().HasMaxLength(AchievementDefinition.CategoryMaxLength);
        builder.Property(item => item.Metric).HasConversion<string>().HasMaxLength(30);
        builder.Property(item => item.TitleRu).IsRequired().HasMaxLength(AchievementDefinition.TitleMaxLength);
        builder.Property(item => item.TitleEn).IsRequired().HasMaxLength(AchievementDefinition.TitleMaxLength);
        builder.Property(item => item.DescriptionRu).IsRequired().HasMaxLength(AchievementDefinition.DescriptionMaxLength);
        builder.Property(item => item.DescriptionEn).IsRequired().HasMaxLength(AchievementDefinition.DescriptionMaxLength);
        builder.Property(item => item.Icon).IsRequired().HasMaxLength(AchievementDefinition.IconMaxLength);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => item.Key).IsUnique();
        builder.HasIndex(item => new { item.IsActive, item.SortOrder });
    }
}
