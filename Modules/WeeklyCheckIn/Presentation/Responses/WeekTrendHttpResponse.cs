namespace FoodDiary.Modules.WeeklyCheckIn.Presentation.Responses;

public sealed record WeekTrendHttpResponse(
    double CalorieChange,
    double ProteinChange,
    double FatChange,
    double CarbChange,
    double? WeightChange,
    double? WaistChange,
    int HydrationChange,
    int MealsLoggedChange);
