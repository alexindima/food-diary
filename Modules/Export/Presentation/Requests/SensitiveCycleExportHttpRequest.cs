using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Export.Application.Abstractions.Common;

namespace FoodDiary.Modules.Export.Presentation.Requests;

public sealed record SensitiveCycleExportHttpRequest(
    DateTime DateFrom,
    DateTime DateTo,
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string CurrentPassword,
    [property: Range(ExportInputLimits.MinimumTimeZoneOffsetMinutes, ExportInputLimits.MaximumTimeZoneOffsetMinutes)] int? TimeZoneOffsetMinutes = null);
