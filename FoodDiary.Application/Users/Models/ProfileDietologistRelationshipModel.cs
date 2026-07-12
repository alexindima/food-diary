namespace FoodDiary.Application.Users.Models;

public sealed record ProfileDietologistRelationshipModel(
    Guid InvitationId,
    string Status,
    string Email,
    string? FirstName,
    string? LastName,
    Guid? DietologistUserId,
    ProfileDietologistPermissionsModel Permissions,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc);
