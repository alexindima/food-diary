using System;

namespace FoodDiary.Contracts.WeightEntries;

public record CreateWeightEntryRequest(
    DateTime Date,
    double Weight);
