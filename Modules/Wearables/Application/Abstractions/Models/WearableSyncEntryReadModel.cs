using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Wearables.Models;

public sealed record WearableSyncEntryReadModel(WearableDataType DataType, double Value);
