using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Statistics.Presentation.Requests;

public sealed record GetStatisticsSummaryHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumQuantizationDays)] int QuantizationDays = 1,
    DateOnly? BodyDateFrom = null,
    DateOnly? BodyDateTo = null,
    [MaxLength(100)] string? TimeZoneId = null);
