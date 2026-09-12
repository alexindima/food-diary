using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Products.Models;

public sealed record ProductSnapshotReadModel(
    ProductId Id, string Name, string? ImageUrl, MeasurementUnit BaseUnit, double BaseAmount,
    double CaloriesPerBase, double ProteinsPerBase, double FatsPerBase, double CarbsPerBase,
    double FiberPerBase, double AlcoholPerBase, ProductType ProductType, Visibility Visibility, string? Category);
