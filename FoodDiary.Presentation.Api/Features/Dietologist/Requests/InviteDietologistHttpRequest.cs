namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record InviteDietologistHttpRequest(
    string DietologistEmail,
    DietologistPermissionsHttpRequest Permissions);

public sealed record DietologistPermissionsHttpRequest(
    bool ShareMeals = true,
    bool ShareStatistics = true,
    bool ShareWeight = true,
    bool ShareWaist = true,
    bool ShareGoals = true,
    bool ShareHydration = true);
