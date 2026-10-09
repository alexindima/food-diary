namespace FoodDiary.Modules.Products.Domain.ValueObjects;

public sealed record ProductMeasurementNutritionChanges(
    ProductMeasurementBasis? Basis = null,
    ProductDefaultPortion? DefaultPortion = null,
    double? CaloriesPerBase = null,
    double? ProteinsPerBase = null,
    double? FatsPerBase = null,
    double? CarbsPerBase = null,
    double? FiberPerBase = null,
    double? AlcoholPerBase = null);
