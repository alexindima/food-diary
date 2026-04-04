namespace FoodDiary.Application.Dietologist.Models;

public sealed record ClientSummaryModel(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    DietologistPermissionsModel Permissions,
    DateTime AcceptedAtUtc);
