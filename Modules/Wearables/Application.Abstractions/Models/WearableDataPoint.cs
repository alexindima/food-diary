using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Wearables.Domain.ValueObjects;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Models;

public sealed record WearableDataPoint(WearableDataType DataType, double Value) {
    public WearableReading ToReading() => WearableReading.FromFields(DataType, Value);
}
