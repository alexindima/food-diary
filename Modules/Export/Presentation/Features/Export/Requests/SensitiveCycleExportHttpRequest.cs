using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Export.Common;

namespace FoodDiary.Presentation.Api.Features.Export.Requests;

public sealed record SensitiveCycleExportHttpRequest(
    DateTime DateFrom,
    DateTime DateTo,
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string CurrentPassword,
    [property: Range(ExportInputLimits.MinimumTimeZoneOffsetMinutes, ExportInputLimits.MaximumTimeZoneOffsetMinutes)] int? TimeZoneOffsetMinutes = null);
