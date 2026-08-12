namespace FoodDiary.Application.Dietologist.Models;

public sealed record DietologistInvitationForCurrentUserModel(
    Guid InvitationId,
    Guid ClientUserId,
    string ClientEmail,
    string? ClientFirstName,
    string? ClientLastName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
