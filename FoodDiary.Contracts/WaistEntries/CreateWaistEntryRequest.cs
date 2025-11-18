using System;

namespace FoodDiary.Contracts.WaistEntries;

public record CreateWaistEntryRequest(
    DateTime Date,
    double Circumference);
