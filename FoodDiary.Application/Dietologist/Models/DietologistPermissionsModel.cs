namespace FoodDiary.Application.Dietologist.Models;

public sealed record DietologistPermissionsModel(
    bool ShareMeals,
    bool ShareStatistics,
    bool ShareWeight,
    bool ShareWaist,
    bool ShareGoals,
    bool ShareHydration);
