using System;

namespace FoodDiary.Contracts.WaistEntries;

public record WaistEntryResponse(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double Circumference);
