using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Export.Requests;

public sealed record SensitiveCycleExportHttpRequest(
    DateTime DateFrom,
    DateTime DateTo,
    [MaxLength(AuthenticationInputLimits.MaximumPasswordLength)] string CurrentPassword,
    int? TimeZoneOffsetMinutes = null);
