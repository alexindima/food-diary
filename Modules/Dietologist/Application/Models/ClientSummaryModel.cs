namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record ClientSummaryModel(
    Guid UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? ProfileImage,
    DateTime? BirthDate,
    string? Gender,
    double? HeightCm,
    string? ActivityLevel,
    DietologistPermissionsModel Permissions,
    DateTime AcceptedAtUtc);
