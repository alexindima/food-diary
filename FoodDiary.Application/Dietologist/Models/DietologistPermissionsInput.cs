namespace FoodDiary.Application.Dietologist.Models;

public sealed record DietologistPermissionsInput(
    bool ShareMeals = true,
    bool ShareStatistics = true,
    bool ShareWeight = true,
    bool ShareWaist = true,
    bool ShareGoals = true,
    bool ShareHydration = true);
