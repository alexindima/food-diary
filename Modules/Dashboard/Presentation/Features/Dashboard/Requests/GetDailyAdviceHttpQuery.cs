using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Requests;

public sealed record GetDailyAdviceHttpQuery(
    DateTime Date,
    [Required, MaxLength(PresentationQueryLimits.MaximumLocaleLength)] string Locale = "en");
