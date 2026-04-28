namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminImpersonationStartHttpResponse(
    string AccessToken,
    Guid TargetUserId,
    string TargetEmail,
    Guid ActorUserId,
    string Reason);
