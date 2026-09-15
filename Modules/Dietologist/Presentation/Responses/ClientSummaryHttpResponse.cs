using FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;
namespace FoodDiary.Modules.Dietologist.Presentation.Responses;

public sealed record ClientSummaryHttpResponse(
    Guid UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? ProfileImage,
    DateTime? BirthDate,
    string? Gender,
    double? HeightCm,
    string? ActivityLevel,
    DietologistPermissionsHttpResponse Permissions,
    DateTime AcceptedAtUtc);
