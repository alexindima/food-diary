using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Models;

public sealed record FertilitySignalReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly Date,
    double? BasalBodyTemperatureCelsius,
    OvulationTestResult? OvulationTestResult,
    string? CervicalFluid,
    bool? HadSex,
    string? Notes);
