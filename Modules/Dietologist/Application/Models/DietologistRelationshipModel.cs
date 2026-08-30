namespace FoodDiary.Application.Dietologist.Models;

public sealed record DietologistRelationshipModel(
    Guid InvitationId,
    string Status,
    string Email,
    string? FirstName,
    string? LastName,
    Guid? DietologistUserId,
    DietologistPermissionsModel Permissions,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc);
