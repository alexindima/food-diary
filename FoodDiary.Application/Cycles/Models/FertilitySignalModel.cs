using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record FertilitySignalModel(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    double? BasalBodyTemperatureCelsius,
    OvulationTestResult? OvulationTestResult,
    string? CervicalFluid,
    bool? HadSex,
    string? Notes);
