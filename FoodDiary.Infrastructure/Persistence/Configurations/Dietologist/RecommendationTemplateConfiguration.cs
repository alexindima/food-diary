using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Dietologist;

internal sealed class RecommendationTemplateConfiguration : IEntityTypeConfiguration<RecommendationTemplate> {
    public void Configure(EntityTypeBuilder<RecommendationTemplate> builder) {
        builder.Property(template => template.Id)
            .HasConversion(id => id.Value, value => new RecommendationTemplateId(value))
            .ValueGeneratedNever();
        builder.Property(template => template.DietologistUserId)
            .HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(template => template.Name).IsRequired().HasMaxLength(120);
        builder.Property(template => template.Text).IsRequired().HasMaxLength(2000);
        builder.HasIndex(template => new { template.DietologistUserId, template.IsArchived, template.Name });
        builder.HasOne<FoodDiary.Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(template => template.DietologistUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
