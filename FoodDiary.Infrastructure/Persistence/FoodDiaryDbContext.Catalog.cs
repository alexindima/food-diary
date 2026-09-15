using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.OpenFoodFacts.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.RecentItems.Domain.Entities.Recents;
using FoodDiary.Modules.Usda.Domain.Entities;
using FoodDiary.Modules.Images.PersistenceModel.Images;
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
