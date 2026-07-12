namespace FoodDiary.Application.Users.Models;

public sealed record ProfileDietologistPermissionsModel(
    bool ShareMeals,
    bool ShareStatistics,
    bool ShareWeight,
    bool ShareWaist,
    bool ShareGoals,
    bool ShareHydration,
    bool ShareProfile,
    bool ShareFasting);
