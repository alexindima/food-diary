using System;

namespace FoodDiary.Contracts.WeightEntries;

public record WeightEntryResponse(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double Weight);
