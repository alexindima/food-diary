using System;

namespace FoodDiary.Contracts.Cycles;

public record UpsertCycleDayRequest(
    DateTime Date,
    bool IsPeriod,
    DailySymptomsDto Symptoms,
    string? Notes);
