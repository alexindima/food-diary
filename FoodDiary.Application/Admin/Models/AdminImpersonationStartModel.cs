namespace FoodDiary.Application.Admin.Models;

public sealed record AdminImpersonationStartModel(
    string AccessToken,
    Guid TargetUserId,
    string TargetEmail,
    Guid ActorUserId,
    string Reason);
