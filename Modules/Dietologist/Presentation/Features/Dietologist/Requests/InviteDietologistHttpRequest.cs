namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record InviteDietologistHttpRequest(
    string DietologistEmail,
    DietologistPermissionsHttpRequest Permissions);
