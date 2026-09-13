using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Modules.Usda.Infrastructure.Model;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Usda.Infrastructure.Persistence;

public sealed class UsdaDbContext(DbContextOptions<UsdaDbContext> options) : DbContext(options) {
    public DbSet<UsdaFood> UsdaFoods => Set<UsdaFood>();
    public DbSet<UsdaNutrient> UsdaNutrients => Set<UsdaNutrient>();
    public DbSet<UsdaFoodNutrient> UsdaFoodNutrients => Set<UsdaFoodNutrient>();
    public DbSet<UsdaFoodPortion> UsdaFoodPortions => Set<UsdaFoodPortion>();
    public DbSet<DailyReferenceValue> DailyReferenceValues => Set<DailyReferenceValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyUsdaPersistenceModel();
    }
}
