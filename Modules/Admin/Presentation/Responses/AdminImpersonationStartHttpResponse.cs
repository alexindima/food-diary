namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminImpersonationStartHttpResponse(
    string Code,
    Guid TargetUserId,
    string? TargetEmail,
    Guid ActorUserId,
    string Reason);
