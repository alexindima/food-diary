using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using System.Runtime.InteropServices;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct ProductMeasurementState(
    MeasurementUnit BaseUnit,
    double BaseAmount,
    double DefaultPortionAmount);
