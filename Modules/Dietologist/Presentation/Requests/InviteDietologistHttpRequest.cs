namespace FoodDiary.Modules.Dietologist.Presentation.Requests;

public sealed record InviteDietologistHttpRequest(
    string DietologistEmail,
    DietologistPermissionsHttpRequest Permissions);
