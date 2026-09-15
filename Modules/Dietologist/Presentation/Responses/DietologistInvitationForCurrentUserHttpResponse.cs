namespace FoodDiary.Modules.Dietologist.Presentation.Responses;

public sealed record DietologistInvitationForCurrentUserHttpResponse(
    Guid InvitationId,
    Guid ClientUserId,
    string? ClientEmail,
    string? ClientFirstName,
    string? ClientLastName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
