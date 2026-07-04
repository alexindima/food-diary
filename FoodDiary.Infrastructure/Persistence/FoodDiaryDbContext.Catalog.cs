using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.OpenFoodFacts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Infrastructure.Persistence.Images;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
    public DbSet<ImageObjectDeletionOutboxMessage> ImageObjectDeletionOutbox => Set<ImageObjectDeletionOutboxMessage>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RecentItem> RecentItems => Set<RecentItem>();
    public DbSet<OpenFoodFactsProduct> OpenFoodFactsProducts => Set<OpenFoodFactsProduct>();
    public DbSet<UsdaFood> UsdaFoods => Set<UsdaFood>();
    public DbSet<UsdaNutrient> UsdaNutrients => Set<UsdaNutrient>();
    public DbSet<UsdaFoodNutrient> UsdaFoodNutrients => Set<UsdaFoodNutrient>();
    public DbSet<UsdaFoodPortion> UsdaFoodPortions => Set<UsdaFoodPortion>();
    public DbSet<DailyReferenceValue> DailyReferenceValues => Set<DailyReferenceValue>();
}
