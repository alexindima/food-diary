using System.ComponentModel.DataAnnotations;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Export.Application.Abstractions.Common;

namespace FoodDiary.Modules.Export.Presentation.Requests;

public sealed class SensitiveCycleExportHttpRequest(
    DateTime dateFrom,
    DateTime dateTo,
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string currentPassword,
    int? timeZoneOffsetMinutes = null) {
    public DateTime DateFrom { get; init; } = dateFrom;
    public DateTime DateTo { get; init; } = dateTo;

    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)]
    public string CurrentPassword { get; init; } = currentPassword;

    [Range(ExportInputLimits.MinimumTimeZoneOffsetMinutes, ExportInputLimits.MaximumTimeZoneOffsetMinutes)]
    public int? TimeZoneOffsetMinutes { get; init; } = timeZoneOffsetMinutes;
}
