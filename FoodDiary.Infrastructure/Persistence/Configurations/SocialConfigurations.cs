using FoodDiary.Domain.Entities.Recipes;
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
