namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record ClientSummaryHttpResponse(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    DietologistPermissionsHttpResponse Permissions,
    DateTime AcceptedAtUtc);
