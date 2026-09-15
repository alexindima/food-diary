namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record DietologistPermissionsInput(
    bool ShareMeals = true,
    bool ShareStatistics = true,
    bool ShareWeight = true,
    bool ShareWaist = true,
    bool ShareGoals = true,
    bool ShareHydration = true,
    bool ShareProfile = true,
    bool ShareFasting = true);
