namespace FoodDiary.Modules.Cycles.Presentation.Requests;

public sealed record FertilitySignalHttpModel(
    double? BasalBodyTemperatureCelsius,
    int? OvulationTestResult,
    string? CervicalFluid,
    bool? HadSex,
    string? Notes,
    bool ClearNotes);
