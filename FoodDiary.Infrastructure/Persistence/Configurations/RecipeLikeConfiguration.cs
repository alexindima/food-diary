using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class RecipeLikeConfiguration : IEntityTypeConfiguration<RecipeLike> {
    public void Configure(EntityTypeBuilder<RecipeLike> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RecipeLikeId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        builder.HasIndex(e => new { e.UserId, e.RecipeId }).IsUnique();
        builder.HasIndex(e => e.RecipeId);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
