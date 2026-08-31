namespace FoodDiary.Application.Admin.Models;

public sealed record AdminImpersonationStartModel(
    string Code,
    Guid TargetUserId,
    string TargetEmail,
    Guid ActorUserId,
    string Reason);
