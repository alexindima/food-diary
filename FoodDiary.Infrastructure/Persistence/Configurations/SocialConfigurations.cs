using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class RecipeCommentConfiguration : IEntityTypeConfiguration<RecipeComment> {
    public void Configure(EntityTypeBuilder<RecipeComment> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeCommentId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        entity.Property(e => e.Text).HasMaxLength(2000).IsRequired();

        entity.HasIndex(e => new { e.RecipeId, e.CreatedOnUtc });

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Recipe)
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RecipeLikeConfiguration : IEntityTypeConfiguration<RecipeLike> {
    public void Configure(EntityTypeBuilder<RecipeLike> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeLikeId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        entity.HasIndex(e => new { e.UserId, e.RecipeId }).IsUnique();
        entity.HasIndex(e => e.RecipeId);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport> {
    public void Configure(EntityTypeBuilder<ContentReport> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new ContentReportId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
        entity.Property(e => e.AdminNote).HasMaxLength(2000);

        entity.Property(e => e.TargetType)
            .HasConversion<string>()
            .HasMaxLength(20);

        entity.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        entity.HasIndex(e => new { e.Status, e.CreatedOnUtc });
        entity.HasIndex(e => new { e.UserId, e.TargetType, e.TargetId });

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
