namespace FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;

public sealed record DietologistPermissionsHttpResponse(
    bool ShareMeals,
    bool ShareStatistics,
    bool ShareWeight,
    bool ShareWaist,
    bool ShareGoals,
    bool ShareHydration,
    bool ShareProfile,
    bool ShareFasting);
