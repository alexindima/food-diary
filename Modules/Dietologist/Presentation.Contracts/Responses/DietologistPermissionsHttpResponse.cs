namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record DietologistPermissionsHttpResponse(
    bool ShareMeals,
    bool ShareStatistics,
    bool ShareWeight,
    bool ShareWaist,
    bool ShareGoals,
    bool ShareHydration,
    bool ShareProfile,
    bool ShareFasting);
