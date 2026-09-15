using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using System.Runtime.InteropServices;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct ProductMeasurementNutritionUpdate(
    MeasurementUnit? BaseUnit = null,
    double? BaseAmount = null,
    double? DefaultPortionAmount = null,
    double? CaloriesPerBase = null,
    double? ProteinsPerBase = null,
    double? FatsPerBase = null,
    double? CarbsPerBase = null,
    double? FiberPerBase = null,
    double? AlcoholPerBase = null);
