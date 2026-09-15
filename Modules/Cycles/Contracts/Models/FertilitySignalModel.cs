using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Contracts.Models;

public sealed record FertilitySignalModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly Date,
    double? BasalBodyTemperatureCelsius,
    OvulationTestResult? OvulationTestResult,
    string? CervicalFluid,
    bool? HadSex,
    string? Notes);
