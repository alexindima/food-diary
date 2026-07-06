using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record FertilitySignalReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    double? BasalBodyTemperatureCelsius,
    OvulationTestResult? OvulationTestResult,
    string? CervicalFluid,
    bool? HadSex,
    string? Notes);
