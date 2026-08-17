namespace FoodDiary.Presentation.Api.Features.Export.Requests;

public sealed record SensitiveCycleExportHttpRequest(
    DateTime DateFrom,
    DateTime DateTo,
    string CurrentPassword,
    int? TimeZoneOffsetMinutes = null);
