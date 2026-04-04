namespace FoodDiary.Application.Dietologist.Models;

public sealed record DietologistInfoModel(
    Guid InvitationId,
    Guid DietologistUserId,
    string Email,
    string? FirstName,
    string? LastName,
    DietologistPermissionsModel Permissions,
    DateTime AcceptedAtUtc);
