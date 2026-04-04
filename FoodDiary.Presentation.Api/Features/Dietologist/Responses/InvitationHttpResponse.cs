namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record InvitationHttpResponse(
    Guid InvitationId,
    string ClientEmail,
    string? ClientFirstName,
    string? ClientLastName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
