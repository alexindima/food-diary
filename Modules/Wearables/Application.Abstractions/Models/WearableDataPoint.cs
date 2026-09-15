using FoodDiary.Modules.Wearables.Domain.Enums;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Models;

public sealed record WearableDataPoint(
    WearableDataType DataType,
    double Value);
