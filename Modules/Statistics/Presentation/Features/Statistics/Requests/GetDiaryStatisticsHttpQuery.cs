using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Statistics.Requests;

public sealed record GetDiaryStatisticsHttpQuery([OpenApiNumericRange(1, 7)] int Days = 1);
