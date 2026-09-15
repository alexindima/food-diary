namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record InvitationModel(
    Guid InvitationId,
    string? ClientEmail,
    string? ClientFirstName,
    string? ClientLastName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
