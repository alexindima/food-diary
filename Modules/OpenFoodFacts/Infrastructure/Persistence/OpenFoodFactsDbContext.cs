using FoodDiary.Modules.OpenFoodFacts.PersistenceModel;
using FoodDiary.Modules.OpenFoodFacts.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure.Persistence;

public sealed class OpenFoodFactsDbContext(DbContextOptions<OpenFoodFactsDbContext> options) : DbContext(options) {
    public DbSet<OpenFoodFactsProduct> OpenFoodFactsProducts => Set<OpenFoodFactsProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyOpenFoodFactsPersistenceModel();
    }
}
