using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Products.Contracts.Models;

public sealed record ProductSnapshotReadModel(
    ProductId Id, string Name, string? ImageUrl, MeasurementUnit BaseUnit, double BaseAmount,
    double CaloriesPerBase, double ProteinsPerBase, double FatsPerBase, double CarbsPerBase,
    double FiberPerBase, double AlcoholPerBase, ProductType ProductType, Visibility Visibility, string? Category);
