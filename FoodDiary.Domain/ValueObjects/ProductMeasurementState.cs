using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ProductMeasurementState(
    MeasurementUnit BaseUnit,
    double BaseAmount,
    double DefaultPortionAmount);
