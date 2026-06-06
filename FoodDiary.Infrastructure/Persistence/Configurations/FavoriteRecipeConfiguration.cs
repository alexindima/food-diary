using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class FavoriteRecipeConfiguration : IEntityTypeConfiguration<FavoriteRecipe> {
    public void Configure(EntityTypeBuilder<FavoriteRecipe> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new FavoriteRecipeId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.RecipeId).HasConversion(
            id => id.Value,
            value => new RecipeId(value));

        builder.Property(e => e.Name)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Recipe)
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.RecipeId })
            .IsUnique();

        builder.HasIndex(e => e.UserId);
    }
}
