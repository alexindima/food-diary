using System;

namespace FoodDiary.Contracts.Cycles;

public record CreateCycleRequest(
    DateTime StartDate,
    int? AverageLength,
    int? LutealLength,
    string? Notes);
