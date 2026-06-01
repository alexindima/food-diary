using System.Runtime.InteropServices;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct ProductMeasurementState(
    MeasurementUnit BaseUnit,
    double BaseAmount,
    double DefaultPortionAmount);
