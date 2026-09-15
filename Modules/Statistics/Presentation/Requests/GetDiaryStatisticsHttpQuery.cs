using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Statistics.Presentation.Requests;

public sealed record GetDiaryStatisticsHttpQuery([OpenApiNumericRange(1, 7)] int Days = 1);
