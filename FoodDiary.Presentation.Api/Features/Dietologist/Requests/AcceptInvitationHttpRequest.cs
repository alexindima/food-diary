namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record AcceptInvitationHttpRequest(Guid InvitationId, string Token);
