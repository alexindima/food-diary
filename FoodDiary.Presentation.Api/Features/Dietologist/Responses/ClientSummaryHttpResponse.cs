namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record ClientSummaryHttpResponse(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? ProfileImage,
    DateTime? BirthDate,
    string? Gender,
    double? Height,
    string? ActivityLevel,
    DietologistPermissionsHttpResponse Permissions,
    DateTime AcceptedAtUtc);
