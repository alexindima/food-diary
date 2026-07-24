using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Dietologist;

internal sealed class RecommendationCommentConfiguration : IEntityTypeConfiguration<RecommendationComment> {
    public void Configure(EntityTypeBuilder<RecommendationComment> builder) {
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new RecommendationCommentId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.RecommendationId)
            .HasConversion(id => id.Value, value => new RecommendationId(value));

        builder.Property(e => e.AuthorUserId)
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(e => e.Text)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(e => new { e.RecommendationId, e.CreatedOnUtc });

        builder.HasOne(e => e.Recommendation)
            .WithMany()
            .HasForeignKey(e => e.RecommendationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.AuthorUser)
            .WithMany()
            .HasForeignKey(e => e.AuthorUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
