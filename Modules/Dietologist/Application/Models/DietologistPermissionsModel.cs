namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record DietologistPermissionsModel(
    bool ShareMeals,
    bool ShareStatistics,
    bool ShareWeight,
    bool ShareWaist,
    bool ShareGoals,
    bool ShareHydration,
    bool ShareProfile,
    bool ShareFasting);
