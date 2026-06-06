using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class RecipeCommentConfiguration : IEntityTypeConfiguration<RecipeComment> {
    public void Configure(EntityTypeBuilder<RecipeComment> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeCommentId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        builder.Property(e => e.Text).HasMaxLength(2000).IsRequired();

        builder.HasIndex(e => new { e.RecipeId, e.CreatedOnUtc });

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Recipe)
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
