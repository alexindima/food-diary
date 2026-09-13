using FoodDiary.Domain.Entities.OpenFoodFacts;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure.Persistence;

public sealed class OpenFoodFactsDbContext(DbContextOptions<OpenFoodFactsDbContext> options) : DbContext(options) {
    public DbSet<OpenFoodFactsProduct> OpenFoodFactsProducts => Set<OpenFoodFactsProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyOpenFoodFactsPersistenceModel();
    }
}
