using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Wearables.Models;

public sealed record WearableDataPoint(
    WearableDataType DataType,
    double Value);
