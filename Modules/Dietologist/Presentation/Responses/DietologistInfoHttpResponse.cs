using FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;
namespace FoodDiary.Modules.Dietologist.Presentation.Responses;

public sealed record DietologistInfoHttpResponse(
    Guid InvitationId,
    Guid DietologistUserId,
    string? Email,
    string? FirstName,
    string? LastName,
    DietologistPermissionsHttpResponse Permissions,
    DateTime AcceptedAtUtc);
