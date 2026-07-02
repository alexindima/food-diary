namespace FoodDiary.Application.Gamification.Common;

public interface IGamificationUserProfile {
    double? GetCalorieTargetForDate(DateTime date);
}
