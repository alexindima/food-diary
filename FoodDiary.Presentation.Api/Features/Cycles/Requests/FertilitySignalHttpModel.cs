namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record FertilitySignalHttpModel(
    double? BasalBodyTemperatureCelsius,
    int? OvulationTestResult,
    string? CervicalFluid,
    bool? HadSex,
    string? Notes,
    bool ClearNotes);
