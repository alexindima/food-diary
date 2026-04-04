namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record DeclineInvitationHttpRequest(Guid InvitationId, string Token);
