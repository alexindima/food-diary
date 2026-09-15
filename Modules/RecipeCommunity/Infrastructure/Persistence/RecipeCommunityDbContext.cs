using FoodDiary.Modules.RecipeCommunity.PersistenceModel;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.RecipeCommunity.Infrastructure.Persistence;

public sealed class RecipeCommunityDbContext(DbContextOptions<RecipeCommunityDbContext> options) : DbContext(options) {
    public DbSet<RecipeComment> RecipeComments => Set<RecipeComment>();
    public DbSet<RecipeLike> RecipeLikes => Set<RecipeLike>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyRecipeCommunityPersistenceModel();
    }
}
