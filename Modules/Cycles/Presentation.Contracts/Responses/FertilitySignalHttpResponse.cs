namespace FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;

public sealed record FertilitySignalHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    double? BasalBodyTemperatureCelsius,
    int? OvulationTestResult,
    string? CervicalFluid,
    bool? HadSex,
    string? Notes);
