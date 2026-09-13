namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminImpersonationStartHttpResponse(
    string Code,
    Guid TargetUserId,
    string? TargetEmail,
    Guid ActorUserId,
    string Reason);
