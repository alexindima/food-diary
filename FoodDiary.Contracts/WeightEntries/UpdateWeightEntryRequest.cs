using System;

namespace FoodDiary.Contracts.WeightEntries;

public record UpdateWeightEntryRequest(
    DateTime Date,
    double Weight);
