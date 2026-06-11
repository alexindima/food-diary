namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public sealed record FertilitySignalCommandModel(
    double? BasalBodyTemperatureCelsius,
    int? OvulationTestResult,
    string? CervicalFluid,
    bool? HadSex,
    string? Notes,
    bool ClearNotes);
