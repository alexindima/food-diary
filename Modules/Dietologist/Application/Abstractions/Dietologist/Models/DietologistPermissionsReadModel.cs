namespace FoodDiary.Application.Abstractions.Dietologist.Models;

public sealed record DietologistPermissionsReadModel(
    bool ShareMeals,
    bool ShareStatistics,
    bool ShareWeight,
    bool ShareWaist,
    bool ShareGoals,
    bool ShareHydration,
    bool ShareProfile,
    bool ShareFasting);
