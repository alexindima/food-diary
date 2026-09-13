namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminImpersonationStartModel(
    string Code,
    Guid TargetUserId,
    string? TargetEmail,
    Guid ActorUserId,
    string Reason);
