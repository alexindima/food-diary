using System;
using System.Collections.Generic;

namespace FoodDiary.Contracts.Cycles;

public record CycleResponse(
    Guid Id,
    Guid UserId,
    DateTime StartDate,
    int AverageLength,
    int LutealLength,
    string? Notes,
    IReadOnlyCollection<CycleDayResponse> Days,
    CyclePredictionsResponse? Predictions);

public record CycleDayResponse(
    Guid Id,
    Guid CycleId,
    DateTime Date,
    bool IsPeriod,
    DailySymptomsDto Symptoms,
    string? Notes);

public record DailySymptomsDto(
    int Pain,
    int Mood,
    int Edema,
    int Headache,
    int Energy,
    int SleepQuality,
    int Libido);

public record CyclePredictionsResponse(
    DateTime? NextPeriodStart,
    DateTime? OvulationDate,
    DateTime? PmsStart);
