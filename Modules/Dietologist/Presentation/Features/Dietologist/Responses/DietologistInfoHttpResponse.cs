namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record DietologistInfoHttpResponse(
    Guid InvitationId,
    Guid DietologistUserId,
    string Email,
    string? FirstName,
    string? LastName,
    DietologistPermissionsHttpResponse Permissions,
    DateTime AcceptedAtUtc);
