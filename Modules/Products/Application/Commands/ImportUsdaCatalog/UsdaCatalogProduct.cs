namespace FoodDiary.Modules.Products.Application.Commands.ImportUsdaCatalog;

public sealed record UsdaCatalogProduct(
    int FdcId,
    string Name,
    string SourceName,
    string ProductType,
    string Category,
    double CaloriesPer100G,
    double ProteinsPer100G,
    double FatsPer100G,
    double CarbsPer100G,
    double FiberPer100G,
    double AlcoholPer100G);
