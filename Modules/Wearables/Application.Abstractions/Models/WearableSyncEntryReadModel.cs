using FoodDiary.Modules.Wearables.Domain.Enums;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Models;

public sealed record WearableSyncEntryReadModel(WearableDataType DataType, double Value);
