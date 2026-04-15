namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record DietologistInvitationForCurrentUserHttpResponse(
    Guid InvitationId,
    Guid ClientUserId,
    string ClientEmail,
    string? ClientFirstName,
    string? ClientLastName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
